using System.ComponentModel.DataAnnotations;

namespace GradebookAPI.Models
{
    public class GradeWeight
    {
        public int Id { get; set; }

        [Required]
        public string CourseId { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Range(0, 100)] // Weight percentage should be between 0 and 100. Total course weighting will be validated separately
        public decimal WeightPercentage { get; set; }
    }
}