using System.ComponentModel.DataAnnotations;
using GradebookAPI.Models;

namespace GradebookAPI.DataTransferObjects;

public class CreateAssessmentRequest
{
    [Required]
    public string CourseId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public AssessmentCategory Category { get; set; }

    [Range(typeof(decimal), "0.50", "100000")]
    public decimal MaxScore { get; set; } = 100;

    [Range(typeof(decimal), "0", "100")]
    public decimal WeightPercentage { get; set; }

    public DateTimeOffset? DueDate { get; set; }
}