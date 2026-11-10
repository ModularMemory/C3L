using System;
using System.ComponentModel.DataAnnotations;

namespace GradebookAPI.Models
{
    public class Grade
    {
        public Guid Id { get; set; }

        // Initialize with an empty string to avoid null reference issues
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public Guid AssessmentId { get; set; }

        // Raw points earned by the student for the assessment, should be non-negative and can be arbitrarily large for practical purposes
        [Range(0, 100000)]
        public decimal Score { get; set; }

        // Maybe useful for sorting or filtering grades by date
        public DateTimeOffset? GradedAt { get; set; }
    }
}