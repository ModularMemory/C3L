using System.ComponentModel.DataAnnotations;

namespace GradebookAPI.Models
{
    public class CategoryWeight
    {
        public Guid Id { get; set; }

        [Required]
        public string CourseId { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        // Weight percentage should be between 0 and 100. Total course weighting will be validated separately
        [Range(0, 100)]
        public decimal WeightPercentage { get; set; }
    }
}