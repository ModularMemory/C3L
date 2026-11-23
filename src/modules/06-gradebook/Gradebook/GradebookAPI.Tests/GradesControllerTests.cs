using GradebookAPI.Controllers;
using GradebookAPI.Data;
using GradebookAPI.DataTransferObjects;
using GradebookAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GradebookAPI.Tests;

public class GradesControllerTests
{
    private static (SqliteConnection Connection, GradebookDbContext Context)
        CreateTestDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<GradebookDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new GradebookDbContext(options);
        context.Database.EnsureCreated();

        return (connection, context);
    }

    private static GradesController CreateController(
        GradebookDbContext context)
    {
        return new GradesController(
            NullLogger<GradesController>.Instance,
            context);
    }

    private static async Task<Assessment> CreateAssessment(
        GradebookDbContext context)
    {
        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            CourseId = "course-1",
            Title = "Assignment 1",
            Category = AssessmentCategory.Assignment,
            MaxScore = 100
        };

        context.Assessments.Add(assessment);
        await context.SaveChangesAsync();

        return assessment;
    }
    //
    // Test cases for the Create method
    //
    [Fact]
    public async Task Create_ValidGrade_SavesToDatabase()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);
            var controller = CreateController(context);

            var request = new CreateGradeRequest
            {
                StudentId = "student-1",
                AssessmentId = assessment.Id,
                Score = 85
            };

            var result = await controller.Create(request);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var grade = Assert.IsType<Grade>(created.Value);

            Assert.Equal("student-1", grade.StudentId);
            Assert.Equal(assessment.Id, grade.AssessmentId);
            Assert.Equal(85m, grade.Score);
            Assert.NotNull(grade.GradedAt);

            context.ChangeTracker.Clear();

            var savedGrade = await context.Grades.FindAsync(grade.Id);

            Assert.NotNull(savedGrade);
            Assert.Equal(85m, savedGrade.Score);
        }
    }

    [Fact]
    public async Task Create_MissingStudentId_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var request = new CreateGradeRequest
            {
                StudentId = " ",
                AssessmentId = Guid.NewGuid(),
                Score = 85
            };

            var result = await controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Fact]
    public async Task Create_EmptyAssessmentId_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var request = new CreateGradeRequest
            {
                StudentId = "student-1",
                AssessmentId = Guid.Empty,
                Score = 85
            };

            var result = await controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Fact]
    public async Task Create_NonexistentAssessment_ReturnsNotFound()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var request = new CreateGradeRequest
            {
                StudentId = "student-1",
                AssessmentId = Guid.NewGuid(),
                Score = 85
            };

            var result = await controller.Create(request);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }

    [Fact]
    public async Task Create_ScoreExceedsMaximum_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);
            var controller = CreateController(context);

            var request = new CreateGradeRequest
            {
                StudentId = "student-1",
                AssessmentId = assessment.Id,
                Score = 110
            };

            var result = await controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Fact]
    public async Task Create_NegativeScore_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);
            var controller = CreateController(context);

            var request = new CreateGradeRequest
            {
                StudentId = "student-1",
                AssessmentId = assessment.Id,
                Score = -5
            };

            var result = await controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Fact]
    public async Task Create_DuplicateGrade_ReturnsConflict()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);

            context.Grades.Add(new Grade
            {
                Id = Guid.NewGuid(),
                StudentId = "student-1",
                AssessmentId = assessment.Id,
                Score = 80
            });

            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var request = new CreateGradeRequest
            {
                StudentId = "student-1",
                AssessmentId = assessment.Id,
                Score = 85
            };

            var result = await controller.Create(request);

            Assert.IsType<ConflictObjectResult>(result);

            Assert.Equal(1, await context.Grades.CountAsync());
        }
    }
    //
    // Test cases for the GetById method
    //
    [Fact]
    public async Task GetById_ExistingGrade_ReturnsGrade()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);

            var grade = new Grade
            {
                Id = Guid.NewGuid(),
                StudentId = "student-1",
                AssessmentId = assessment.Id,
                Score = 85,
                GradedAt = DateTimeOffset.UtcNow
            };

            context.Grades.Add(grade);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetById(grade.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedGrade = Assert.IsType<Grade>(okResult.Value);

            Assert.Equal(grade.Id, returnedGrade.Id);
            Assert.Equal("student-1", returnedGrade.StudentId);
            Assert.Equal(85m, returnedGrade.Score);
        }
    }

    [Fact]
    public async Task GetById_NonexistentGrade_ReturnsNotFound()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var result = await controller.GetById(Guid.NewGuid());

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }

    [Fact]
    public async Task GetById_EmptyGuid_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var result = await controller.GetById(Guid.Empty);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
    //
    // Test cases for the GetByAssessmentId method
    //
    [Fact]
    public async Task GetByAssessmentId_ExistingGrades_ReturnsMatchingGrades()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);
            var controller = CreateController(context);

            context.Grades.AddRange(
                new Grade
                {
                    Id = Guid.NewGuid(),
                    StudentId = "student-2",
                    AssessmentId = assessment.Id,
                    Score = 90
                },
                new Grade
                {
                    Id = Guid.NewGuid(),
                    StudentId = "student-1",
                    AssessmentId = assessment.Id,
                    Score = 85
                });

            await context.SaveChangesAsync();

            var result = await controller.GetByAssessmentId(assessment.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var grades = Assert.IsType<List<Grade>>(okResult.Value);

            Assert.Equal(2, grades.Count);
            Assert.Equal("student-1", grades[0].StudentId);
            Assert.Equal("student-2", grades[1].StudentId);
        }
    }

    [Fact]
    public async Task GetByAssessmentId_NoGrades_ReturnsEmptyList()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);
            var controller = CreateController(context);

            var result = await controller.GetByAssessmentId(assessment.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var grades = Assert.IsType<List<Grade>>(okResult.Value);

            Assert.Empty(grades);
        }
    }

    [Fact]
    public async Task GetByAssessmentId_NonexistentAssessment_ReturnsNotFound()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var result = await controller.GetByAssessmentId(Guid.NewGuid());

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }

    [Fact]
    public async Task GetByAssessmentId_EmptyGuid_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var result = await controller.GetByAssessmentId(Guid.Empty);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
    //
    // Test cases for the GetByStudentId method
    //
    [Fact]
    public async Task GetByStudentId_ExistingGrades_ReturnsOnlyStudentGrades()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);

            context.Grades.AddRange(
                new Grade
                {
                    Id = Guid.NewGuid(),
                    StudentId = "student-1",
                    AssessmentId = assessment.Id,
                    Score = 85
                },
                new Grade
                {
                    Id = Guid.NewGuid(),
                    StudentId = "student-2",
                    AssessmentId = assessment.Id,
                    Score = 90
                });

            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetByStudentId("student-1");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var grades = Assert.IsType<List<Grade>>(okResult.Value);

            Assert.Single(grades);
            Assert.Equal("student-1", grades[0].StudentId);
            Assert.Equal(85m, grades[0].Score);
        }
    }

    [Fact]
    public async Task GetByStudentId_NoGrades_ReturnsEmptyList()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var result = await controller.GetByStudentId("student-999");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var grades = Assert.IsType<List<Grade>>(okResult.Value);

            Assert.Empty(grades);
        }
    }

    [Fact]
    public async Task GetByStudentId_WhitespaceId_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var result = await controller.GetByStudentId("   ");

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
    //
    // Test cases for the GetByCourseId method
    //
    [Fact]
    public async Task GetByCourseId_ExistingGrades_ReturnsOnlyCourseGrades()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            // Create assessments for two different courses.
            var courseOneAssessment = await CreateAssessment(context);

            var courseTwoAssessment = new Assessment
            {
                Id = Guid.NewGuid(),
                CourseId = "course-2",
                Title = "Assignment 2",
                Category = AssessmentCategory.Assignment,
                MaxScore = 100
            };

            context.Assessments.Add(courseTwoAssessment);
            await context.SaveChangesAsync();

            // Add grades for both courses.
            context.Grades.AddRange(
                new Grade
                {
                    Id = Guid.NewGuid(),
                    StudentId = "student-1",
                    AssessmentId = courseOneAssessment.Id,
                    Score = 85
                },
                new Grade
                {
                    Id = Guid.NewGuid(),
                    StudentId = "student-2",
                    AssessmentId = courseOneAssessment.Id,
                    Score = 90
                },
                new Grade
                {
                    Id = Guid.NewGuid(),
                    StudentId = "student-1",
                    AssessmentId = courseTwoAssessment.Id,
                    Score = 75
                });

            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetByCourseId("course-1");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var grades = Assert.IsType<List<Grade>>(okResult.Value);

            Assert.Equal(2, grades.Count);
            Assert.All(grades, grade =>
                Assert.Equal(courseOneAssessment.Id, grade.AssessmentId));
        }
    }

    [Fact]
    public async Task GetByCourseId_NoGrades_ReturnsEmptyList()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var result = await controller.GetByCourseId("course-999");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var grades = Assert.IsType<List<Grade>>(okResult.Value);

            Assert.Empty(grades);
        }
    }

    [Fact]
    public async Task GetByCourseId_WhitespaceId_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var result = await controller.GetByCourseId("   ");

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
    //
    // Test cases for the Update method
    //
    [Fact]
    public async Task Update_ValidScore_UpdatesGrade()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);

            var grade = new Grade
            {
                Id = Guid.NewGuid(),
                StudentId = "student-1",
                AssessmentId = assessment.Id,
                Score = 75
            };

            context.Grades.Add(grade);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var request = new UpdateGradeRequest
            {
                Score = 90
            };

            var result = await controller.Update(grade.Id, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedGrade = Assert.IsType<Grade>(okResult.Value);

            Assert.Equal(90m, updatedGrade.Score);
            Assert.NotNull(updatedGrade.GradedAt);

            // Verify that the change was saved to SQLite.
            context.ChangeTracker.Clear();

            var savedGrade = await context.Grades.FindAsync(grade.Id);

            Assert.NotNull(savedGrade);
            Assert.Equal(90m, savedGrade.Score);

            // Updating the score must not change ownership.
            Assert.Equal("student-1", savedGrade.StudentId);
            Assert.Equal(assessment.Id, savedGrade.AssessmentId);
        }
    }

    [Fact]
    public async Task Update_NonexistentGrade_ReturnsNotFound()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var request = new UpdateGradeRequest
            {
                Score = 85
            };

            var result = await controller.Update(Guid.NewGuid(), request);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }

    [Fact]
    public async Task Update_EmptyGuid_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var request = new UpdateGradeRequest
            {
                Score = 85
            };

            var result = await controller.Update(Guid.Empty, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Fact]
    public async Task Update_NegativeScore_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);

            var grade = new Grade
            {
                Id = Guid.NewGuid(),
                StudentId = "student-1",
                AssessmentId = assessment.Id,
                Score = 75
            };

            context.Grades.Add(grade);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var request = new UpdateGradeRequest
            {
                Score = -5
            };

            var result = await controller.Update(grade.Id, request);

            Assert.IsType<BadRequestObjectResult>(result);

            // Confirm that the original score was not changed.
            context.ChangeTracker.Clear();

            var savedGrade = await context.Grades.FindAsync(grade.Id);

            Assert.NotNull(savedGrade);
            Assert.Equal(75m, savedGrade.Score);
        }
    }

    [Fact]
    public async Task Update_ScoreExceedsMaximum_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);

            var grade = new Grade
            {
                Id = Guid.NewGuid(),
                StudentId = "student-1",
                AssessmentId = assessment.Id,
                Score = 75
            };

            context.Grades.Add(grade);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var request = new UpdateGradeRequest
            {
                Score = 110
            };

            var result = await controller.Update(grade.Id, request);

            Assert.IsType<BadRequestObjectResult>(result);

            context.ChangeTracker.Clear();

            var savedGrade = await context.Grades.FindAsync(grade.Id);

            Assert.NotNull(savedGrade);
            Assert.Equal(75m, savedGrade.Score);
        }
    }
    //
    // Test cases for the Delete method
    //
    [Fact]
    public async Task Delete_ExistingGrade_RemovesGrade()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var assessment = await CreateAssessment(context);

            var grade = new Grade
            {
                Id = Guid.NewGuid(),
                StudentId = "student-1",
                AssessmentId = assessment.Id,
                Score = 85
            };

            context.Grades.Add(grade);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.Delete(grade.Id);

            Assert.IsType<NoContentResult>(result);

            // Confirm the grade was removed from SQLite.
            context.ChangeTracker.Clear();

            var deletedGrade = await context.Grades.FindAsync(grade.Id);

            Assert.Null(deletedGrade);

            // Confirm the associated assessment still exists.
            var savedAssessment = await context.Assessments.FindAsync(assessment.Id);

            Assert.NotNull(savedAssessment);
        }
    }

    [Fact]
    public async Task Delete_NonexistentGrade_ReturnsNotFound()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var result = await controller.Delete(Guid.NewGuid());

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }

    [Fact]
    public async Task Delete_EmptyGuid_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();

        using (connection)
        using (context)
        {
            var controller = CreateController(context);

            var result = await controller.Delete(Guid.Empty);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

}