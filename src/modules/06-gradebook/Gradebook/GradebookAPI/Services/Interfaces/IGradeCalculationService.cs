using GradebookAPI.Models;

namespace GradebookAPI.Services.Interfaces
{
    public interface IGradeCalculationService
    {
        decimal CalculateAssessmentPercentage(
            Grade grade,
            Assessment assessment);

        decimal? CalculateCategoryPercentage(
            IEnumerable<Assessment> assessments,
            IEnumerable<Grade> grades);

        decimal? CalculateCurrentCoursePercentage(
            IDictionary<AssessmentCategory, decimal?> categoryPercentages,
            IEnumerable<CategoryWeight> categoryWeights);
    }
}
