using System;
using System.ComponentModel.DataAnnotations;

namespace GradebookAPI.Models
{
    public class Assessment
    {
        public Guid Id { get; set; }

        [Required]
        public string CourseId { get; set; } = string.Empty;

        /// <summary>The title of the assessment.</summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public AssessmentCategory Category { get; set; } = AssessmentCategory.Undefined;

        // Assessments should have a positive max score, assessment should be out of at least half a point (think like a bonus question/assignment),
        // and we can set an arbitrary upper limit for practical purposes
        // Default max value of 100
        [Range(0.50, 100000)]
        public decimal MaxScore { get; set; } = 100;

        // Weight percentage should be between 0 and 100. Total assessment category weighting will be validated separately
        [Range(0, 100)]
        public decimal WeightPercentage { get; set; }

        public DateTimeOffset? DueDate { get; set; }
    }
}