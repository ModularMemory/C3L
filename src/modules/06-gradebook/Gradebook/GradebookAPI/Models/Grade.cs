using System;
using System.ComponentModel.DataAnnotations;

namespace GradebookAPI.Models
{
    public class Grade
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;   // Initialize with an empty string to avoid null reference issues

        [Range(1, int.MaxValue)]
        public int AssessmentId { get; set; }

        [Range(0, 100000)]  // Raw points earned by the student for the assessment, should be non-negative and can be arbitrarily large for practical purposes
        public decimal Score { get; set; }

        public DateTime? GradedAt { get; set; } // Maybe useful for sorting or filtering grades by date
    }
}