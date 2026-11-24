using GradebookAPI.Models;
using GradebookAPI.Services.Interfaces;

namespace GradebookAPI.Services;

public class GradeCalculationService : IGradeCalculationService
{
    // Calculate a student's percentage on one assessment
    public decimal CalculateAssessmentPercentage(Grade grade, Assessment assessment)
    {

        ArgumentNullException.ThrowIfNull(grade);
        
        ArgumentNullException.ThrowIfNull(assessment);

        if (grade.AssessmentId != assessment.Id)
            throw new ArgumentException("The grade does not belong to this assessment.");

        if (assessment.MaxScore <= 0)
            throw new ArgumentException("The assessment's maximum score must be positive.");

        if (grade.Score < 0 || grade.Score > assessment.MaxScore)
            throw new ArgumentException("The score must be between zero and the maximum score.");

        return grade.Score / assessment.MaxScore * 100m;
    }

    // Calculate a category percentage using total points
    // Assessments without grades are excluded from student's course grade calculation
    public decimal? CalculateCategoryPercentage(
        IEnumerable<Assessment> assessments,
        IEnumerable<Grade> grades)
    {
        ArgumentNullException.ThrowIfNull(assessments);
        ArgumentNullException.ThrowIfNull(grades);

        var assessmentList = assessments.ToList();
        var gradeList = grades.ToList();

        if (assessmentList.Any(a => a.MaxScore <= 0))
            throw new ArgumentException("Assessment maximum scores must be positive.");

        if (assessmentList.Select(a => a.Id).Distinct().Count() != assessmentList.Count)
            throw new ArgumentException("Duplicate assessment IDs were provided.");

        if (gradeList.GroupBy(g => g.AssessmentId).Any(group => group.Count() > 1))
            throw new ArgumentException("Only one grade per assessment is allowed.");

        decimal earnedPoints = 0m;
        decimal possiblePoints = 0m;

        foreach (var assessment in assessmentList)
        {
            var grade = gradeList.FirstOrDefault(g => g.AssessmentId == assessment.Id);

            if (grade == null)
                continue;

            CalculateAssessmentPercentage(grade, assessment);

            earnedPoints += grade.Score;
            possiblePoints += assessment.MaxScore;
        }

        if (possiblePoints == 0m)
            return null;

        return earnedPoints / possiblePoints * 100m;
    }

    // Calculate the current course grade from category percentages
    // and category weights... Categories without grades are excluded
    public decimal? CalculateCurrentCoursePercentage(
        IDictionary<AssessmentCategory, decimal?> categoryPercentages,
        IEnumerable<CategoryWeight> categoryWeights)
    {
        ArgumentNullException.ThrowIfNull(categoryPercentages);
        ArgumentNullException.ThrowIfNull(categoryWeights);

        decimal weightedTotal = 0m;
        decimal activeWeight = 0m;

        foreach (var categoryWeight in categoryWeights)
        {
            if (!Enum.TryParse<AssessmentCategory>(
                categoryWeight.Category, true, out var category) ||
                category == AssessmentCategory.Undefined)
            {
                throw new ArgumentException(
                    $"Unknown assessment category: {categoryWeight.Category}");
            }

            if (categoryWeight.WeightPercentage < 0m ||
                categoryWeight.WeightPercentage > 100m)
            {
                throw new ArgumentException("Category weights must be between 0 and 100.");
            }

            if (!categoryPercentages.TryGetValue(category, out var percentage) ||
                percentage == null)
            {
                continue;
            }

            if (percentage is < 0m or > 100m)
                throw new ArgumentException("Category percentages must be between 0 and 100.");

            weightedTotal += percentage.Value * categoryWeight.WeightPercentage;
            activeWeight += categoryWeight.WeightPercentage;
        }

        if (activeWeight == 0m)
            return null;

        // Normalize by the weight of categories that have grades, only scored categories count towards current average
        return weightedTotal / activeWeight;
    }
}
