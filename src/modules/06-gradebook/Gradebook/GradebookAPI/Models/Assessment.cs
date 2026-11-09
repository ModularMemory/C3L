using System;
using System.ComponentModel.DataAnnotations;

namespace GradebookAPI.Models
{
    public class Assessment
    {
        public int Id { get; set; }

        [Required]
        public string CourseId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Range(0.50, 100000)]   // Assessments should have a positive max score, assessment should be out of at least half a point (think like a bonus question/assignment),                        
        public decimal MaxScore { get; set; } = 100;    // and we can set an arbitrary upper limit for practical purposes
                                                        // Default max value of 100
        public DateTime? DueDate { get; set; }
    }
}