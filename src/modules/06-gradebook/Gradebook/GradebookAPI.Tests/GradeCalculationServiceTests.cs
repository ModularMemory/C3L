using GradebookAPI.Models;
using GradebookAPI.Services;
using Xunit;

namespace GradebookAPI.Tests
{
    public class GradeCalculationServiceTests
    {
        private readonly GradeCalculationService _service = new();

        // Test 1: Calculate a normal assessment percentage.
        [Fact]
        public void CalculateAssessmentPercentage_ValidScore_ReturnsPercentage()
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 20
            };

            var grade = new Grade
            {
                AssessmentId = assessment.Id,
                Score = 18
            };

            decimal result = _service.CalculateAssessmentPercentage(grade, assessment);

            Assert.Equal(90, result);
        }

        // Test 2: A score of zero should return zero percent.
        [Fact]
        public void CalculateAssessmentPercentage_ZeroScore_ReturnsZero()
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 20
            };

            var grade = new Grade
            {
                AssessmentId = assessment.Id,
                Score = 0
            };

            decimal result = _service.CalculateAssessmentPercentage(grade, assessment);

            Assert.Equal(0, result);
        }

        // Test 3: Full marks should return 100 percent.
        [Fact]
        public void CalculateAssessmentPercentage_FullScore_Returns100()
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 20
            };

            var grade = new Grade
            {
                AssessmentId = assessment.Id,
                Score = 20
            };

            decimal result = _service.CalculateAssessmentPercentage(grade, assessment);

            Assert.Equal(100, result);
        }

        // Test 4: A grade cannot exceed the maximum score.
        [Fact]
        public void CalculateAssessmentPercentage_ScoreExceedsMaximum_ThrowsException()
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 20
            };

            var grade = new Grade
            {
                AssessmentId = assessment.Id,
                Score = 25
            };

            Assert.Throws<ArgumentException>(() =>
                _service.CalculateAssessmentPercentage(grade, assessment));
        }

        // Test 5: An assessment cannot have a maximum score of zero.
        [Fact]
        public void CalculateAssessmentPercentage_ZeroMaximum_ThrowsException()
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 0
            };

            var grade = new Grade
            {
                AssessmentId = assessment.Id,
                Score = 0
            };

            Assert.Throws<ArgumentException>(() =>
                _service.CalculateAssessmentPercentage(grade, assessment));
        }

        // Test 6: The grade must belong to the specified assessment.
        [Fact]
        public void CalculateAssessmentPercentage_MismatchedAssessment_ThrowsException()
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 100
            };

            var grade = new Grade
            {
                AssessmentId = Guid.NewGuid(),
                Score = 80
            };

            Assert.Throws<ArgumentException>(() =>
                _service.CalculateAssessmentPercentage(grade, assessment));
        }

        // Test 7: Category percentages use total points earned
        // divided by total points possible.
        [Fact]
        public void CalculateCategoryPercentage_MultipleAssessments_ReturnsPercentage()
        {
            var assessment1 = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 20
            };

            var assessment2 = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 100
            };

            var assessments = new List<Assessment>
            {
                assessment1,
                assessment2
            };

            var grades = new List<Grade>
            {
                new Grade { AssessmentId = assessment1.Id, Score = 18 },
                new Grade { AssessmentId = assessment2.Id, Score = 70 }
            };

            decimal? result = _service.CalculateCategoryPercentage(assessments, grades);

            Assert.NotNull(result);
            Assert.Equal(73.33m, Math.Round(result.Value, 2));
        }

        // Test 8: Ungraded assessments are excluded.
        [Fact]
        public void CalculateCategoryPercentage_UngradedAssessment_IsExcluded()
        {
            var assessment1 = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 20
            };

            var assessment2 = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 100
            };

            var assessments = new List<Assessment>
            {
                assessment1,
                assessment2
            };

            var grades = new List<Grade>
            {
                new Grade { AssessmentId = assessment1.Id, Score = 18 }
            };

            decimal? result = _service.CalculateCategoryPercentage(assessments, grades);

            Assert.Equal(90m, result);
        }

        // Test 9: A category without grades returns null.
        [Fact]
        public void CalculateCategoryPercentage_NoGrades_ReturnsNull()
        {
            var assessments = new List<Assessment>
            {
                new Assessment
                {
                    Id = Guid.NewGuid(),
                    MaxScore = 100
                }
            };

            var grades = new List<Grade>();

            decimal? result = _service.CalculateCategoryPercentage(assessments, grades);

            Assert.Null(result);
        }

        // Test 10: Duplicate grades for the same assessment are rejected.
        [Fact]
        public void CalculateCategoryPercentage_DuplicateGrades_ThrowsException()
        {
            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                MaxScore = 100
            };

            var grades = new List<Grade>
            {
                new Grade { AssessmentId = assessment.Id, Score = 80 },
                new Grade { AssessmentId = assessment.Id, Score = 90 }
            };

            Assert.Throws<ArgumentException>(() =>
                _service.CalculateCategoryPercentage(
                    new List<Assessment> { assessment }, grades));
        }

        // Test 11: Calculate the current course grade using category weights.
        [Fact]
        public void CalculateCurrentCoursePercentage_ValidCategories_ReturnsWeightedGrade()
        {
            var percentages = new Dictionary<AssessmentCategory, decimal?>
            {
                { AssessmentCategory.Assignment, 80 },
                { AssessmentCategory.Exam, 90 }
            };

            var weights = new List<CategoryWeight>
            {
                new CategoryWeight { Category = "Assignment", WeightPercentage = 40 },
                new CategoryWeight { Category = "Exam", WeightPercentage = 60 }
            };

            decimal? result = _service.CalculateCurrentCoursePercentage(percentages, weights);

            Assert.Equal(86m, result);
        }

        // Test 12: Ungraded categories are excluded and weights normalized.
        [Fact]
        public void CalculateCurrentCoursePercentage_UngradedCategory_NormalizesWeights()
        {
            var percentages = new Dictionary<AssessmentCategory, decimal?>
            {
                { AssessmentCategory.Assignment, 80 },
                { AssessmentCategory.Quiz, 90 },
                { AssessmentCategory.Exam, null }
            };

            var weights = new List<CategoryWeight>
            {
                new CategoryWeight { Category = "Assignment", WeightPercentage = 40 },
                new CategoryWeight { Category = "Quiz", WeightPercentage = 20 },
                new CategoryWeight { Category = "Exam", WeightPercentage = 40 }
            };

            decimal? result = _service.CalculateCurrentCoursePercentage(percentages, weights);

            Assert.NotNull(result);
            Assert.Equal(83.33m, Math.Round(result.Value, 2));
        }

        // Test 13: No graded categories means no current course grade.
        [Fact]
        public void CalculateCurrentCoursePercentage_NoGrades_ReturnsNull()
        {
            var percentages = new Dictionary<AssessmentCategory, decimal?>();

            var weights = new List<CategoryWeight>
            {
                new CategoryWeight { Category = "Assignment", WeightPercentage = 40 },
                new CategoryWeight { Category = "Exam", WeightPercentage = 60 }
            };

            decimal? result = _service.CalculateCurrentCoursePercentage(percentages, weights);

            Assert.Null(result);
        }

        // Test 14: Category weights cannot exceed 100 percent.
        [Fact]
        public void CalculateCurrentCoursePercentage_InvalidWeight_ThrowsException()
        {
            var percentages = new Dictionary<AssessmentCategory, decimal?>
            {
                { AssessmentCategory.Assignment, 80 }
            };

            var weights = new List<CategoryWeight>
            {
                new CategoryWeight { Category = "Assignment", WeightPercentage = 120 }
            };

            Assert.Throws<ArgumentException>(() =>
                _service.CalculateCurrentCoursePercentage(percentages, weights));
        }

        // Test 15: Unknown category names are rejected.
        [Fact]
        public void CalculateCurrentCoursePercentage_UnknownCategory_ThrowsException()
        {
            var percentages = new Dictionary<AssessmentCategory, decimal?>
            {
                { AssessmentCategory.Assignment, 80 }
            };

            var weights = new List<CategoryWeight>
            {
                new CategoryWeight { Category = "UnknownCategory", WeightPercentage = 40 }
            };

            Assert.Throws<ArgumentException>(() =>
                _service.CalculateCurrentCoursePercentage(percentages, weights));
        }
    }
}
