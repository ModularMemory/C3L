using GradebookAPI.Controllers;
using GradebookAPI.Data;
using GradebookAPI.DataTransferObjects;
using GradebookAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GradebookAPI.Tests;

public class AssessmentsControllerTests
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

    private static CreateAssessmentRequest ValidRequest()
    {
        return new CreateAssessmentRequest
        {
            CourseId = "1",
            Title = "Assignment 1",
            Category = AssessmentCategory.Assignment,
            MaxScore = 100,
            WeightPercentage = 20,
            DueDate = new DateTimeOffset(2026, 12, 1, 23, 59, 0,
                TimeSpan.Zero)
        };
    }
    //
    // Test cases for Create and GetById methods
    //
    [Fact]
    public async Task Create_ValidAssessment_ReturnsCreatedAndSaves()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var controller = new AssessmentsController(context);

            var result = await controller.Create(ValidRequest());

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var assessment = Assert.IsType<Assessment>(created.Value);

            Assert.Equal(201, created.StatusCode);
            Assert.Equal("Assignment 1", assessment.Title);
            Assert.NotEqual(Guid.Empty, assessment.Id);

            var saved = await context.Assessments.FindAsync(assessment.Id);
            Assert.NotNull(saved);
            Assert.Equal("1", saved.CourseId);
        }
    }

    [Fact]
    public async Task Create_EmptyCourseId_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var controller = new AssessmentsController(context);
            var request = ValidRequest();
            request.CourseId = " ";

            var result = await controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(await context.Assessments.ToListAsync());
        }
    }

    [Fact]
    public async Task Create_InvalidCategory_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var controller = new AssessmentsController(context);
            var request = ValidRequest();
            request.Category = (AssessmentCategory)999;

            var result = await controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(await context.Assessments.ToListAsync());
        }
    }

    [Fact]
    public async Task GetById_ExistingAssessment_ReturnsOk()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                CourseId = "1",
                Title = "Midterm",
                Category = AssessmentCategory.Test,
                MaxScore = 100
            };

            context.Assessments.Add(assessment);
            await context.SaveChangesAsync();

            var controller = new AssessmentsController(context);

            var result = await controller.GetById(assessment.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<Assessment>(ok.Value);

            Assert.Equal(assessment.Id, returned.Id);
            Assert.Equal("Midterm", returned.Title);
        }
    }

    [Fact]
    public async Task GetById_NonexistentAssessment_ReturnsNotFound()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var controller = new AssessmentsController(context);

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
            var controller = new AssessmentsController(context);

            var result = await controller.GetById(Guid.Empty);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
    //
    // Test cases for GetByCourseId method
    //
    [Fact]
    public async Task GetByCourseId_ExistingAssessments_ReturnsOnlyMatchingCourse()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            context.Assessments.AddRange(
                new Assessment
                {
                    Id = Guid.NewGuid(),
                    CourseId = "1",
                    Title = "Assignment 1",
                    Category = AssessmentCategory.Assignment,
                    MaxScore = 100
                },
                new Assessment
                {
                    Id = Guid.NewGuid(),
                    CourseId = "1",
                    Title = "Midterm",
                    Category = AssessmentCategory.Test,
                    MaxScore = 100
                },
                new Assessment
                {
                    Id = Guid.NewGuid(),
                    CourseId = "2",
                    Title = "Quiz 1",
                    Category = AssessmentCategory.Quiz,
                    MaxScore = 20
                }
            );

            await context.SaveChangesAsync();

            var controller = new AssessmentsController(context);

            var result = await controller.GetByCourseId("1");

            var ok = Assert.IsType<OkObjectResult>(result);
            var assessments = Assert.IsType<List<Assessment>>(ok.Value);

            Assert.Equal(2, assessments.Count);
            Assert.All(assessments, a => Assert.Equal("1", a.CourseId));
            Assert.Equal("Assignment 1", assessments[0].Title);
            Assert.Equal("Midterm", assessments[1].Title);
        }
    }

    [Fact]
    public async Task GetByCourseId_NoAssessments_ReturnsEmptyList()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var controller = new AssessmentsController(context);

            var result = await controller.GetByCourseId("1");

            var ok = Assert.IsType<OkObjectResult>(result);
            var assessments = Assert.IsType<List<Assessment>>(ok.Value);

            Assert.Empty(assessments);
        }
    }

    [Fact]
    public async Task GetByCourseId_EmptyCourseId_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var controller = new AssessmentsController(context);

            var result = await controller.GetByCourseId(" ");

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
    //
    // Test cases for Update method
    //
    [Fact]
    public async Task Update_ExistingAssessment_UpdatesAndSaves()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                CourseId = "1",
                Title = "Original Assignment",
                Category = AssessmentCategory.Assignment,
                MaxScore = 100,
                WeightPercentage = 10
            };

            context.Assessments.Add(assessment);
            await context.SaveChangesAsync();

            var request = new UpdateAssessmentRequest
            {
                Title = "Updated Assignment",
                Category = AssessmentCategory.Project,
                MaxScore = 200,
                WeightPercentage = 25,
                DueDate = new DateTimeOffset(
                    2026, 12, 15, 23, 59, 0, TimeSpan.Zero)
            };

            var controller = new AssessmentsController(context);

            var result = await controller.Update(assessment.Id, request);

            var ok = Assert.IsType<OkObjectResult>(result);
            var updated = Assert.IsType<Assessment>(ok.Value);

            Assert.Equal("Updated Assignment", updated.Title);
            Assert.Equal(AssessmentCategory.Project, updated.Category);
            Assert.Equal(200m, updated.MaxScore);
            Assert.Equal(25m, updated.WeightPercentage);
            Assert.Equal("1", updated.CourseId);

            // Reload from the database to verify persistence.
            context.ChangeTracker.Clear();

            var saved = await context.Assessments.FindAsync(assessment.Id);

            Assert.NotNull(saved);
            Assert.Equal("Updated Assignment", saved.Title);
            Assert.Equal(200m, saved.MaxScore);
            Assert.Equal(25m, saved.WeightPercentage);
        }
    }

    [Fact]
    public async Task Update_NonexistentAssessment_ReturnsNotFound()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var controller = new AssessmentsController(context);

            var result = await controller.Update(
                Guid.NewGuid(),
                new UpdateAssessmentRequest
                {
                    Title = "Updated Assignment",
                    Category = AssessmentCategory.Assignment
                });

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
            var controller = new AssessmentsController(context);

            var result = await controller.Update(
                Guid.Empty,
                new UpdateAssessmentRequest
                {
                    Title = "Updated Assignment",
                    Category = AssessmentCategory.Assignment
                });

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Fact]
    public async Task Update_InvalidCategory_ReturnsBadRequest()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var controller = new AssessmentsController(context);

            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                CourseId = "1",
                Title = "Original Assignment",
                Category = AssessmentCategory.Assignment,
                MaxScore = 100
            };

            context.Assessments.Add(assessment);
            await context.SaveChangesAsync();

            var result = await controller.Update(
                assessment.Id,
                new UpdateAssessmentRequest
                {
                    Title = "Updated Assignment",
                    Category = (AssessmentCategory)999
                });

            Assert.IsType<BadRequestObjectResult>(result);

            context.ChangeTracker.Clear();

            var saved = await context.Assessments.FindAsync(assessment.Id);

            Assert.NotNull(saved);
            Assert.Equal("Original Assignment", saved.Title);
        }
    }
    //
    // Test cases for Delete method
    //
    [Fact]
    public async Task Delete_ExistingAssessment_RemovesFromDatabase()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                CourseId = "1",
                Title = "Assignment 1",
                Category = AssessmentCategory.Assignment,
                MaxScore = 100
            };

            context.Assessments.Add(assessment);
            await context.SaveChangesAsync();

            var controller = new AssessmentsController(context);

            var result = await controller.Delete(assessment.Id);

            Assert.IsType<NoContentResult>(result);

            context.ChangeTracker.Clear();

            var saved = await context.Assessments.FindAsync(assessment.Id);
            Assert.Null(saved);
        }
    }

    [Fact]
    public async Task Delete_NonexistentAssessment_ReturnsNotFound()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var controller = new AssessmentsController(context);

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
            var controller = new AssessmentsController(context);

            var result = await controller.Delete(Guid.Empty);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Fact]
    public async Task Delete_AssessmentWithGrades_ReturnsConflict()
    {
        var (connection, context) = CreateTestDatabase();
        using (connection)
        using (context)
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                CourseId = "1",
                Title = "Assignment 1",
                Category = AssessmentCategory.Assignment,
                MaxScore = 100
            };

            context.Assessments.Add(assessment);
            await context.SaveChangesAsync();

            var grade = new Grade
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessment.Id,
                StudentId = "student-1",
                Score = 85
            };

            context.Grades.Add(grade);
            await context.SaveChangesAsync();

            var controller = new AssessmentsController(context);

            var result = await controller.Delete(assessment.Id);

            Assert.IsType<ConflictObjectResult>(result);

            context.ChangeTracker.Clear();

            var savedAssessment =
                await context.Assessments.FindAsync(assessment.Id);

            var savedGrade =
                await context.Grades.FindAsync(grade.Id);

            Assert.NotNull(savedAssessment);
            Assert.NotNull(savedGrade);
        }
    }
}