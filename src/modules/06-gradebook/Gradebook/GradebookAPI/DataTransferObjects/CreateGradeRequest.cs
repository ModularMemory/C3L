using System;
using System.ComponentModel.DataAnnotations;

namespace GradebookAPI.DataTransferObjects;

public class CreateGradeRequest
{
    [Required]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    public Guid AssessmentId { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal Score { get; set; }
}