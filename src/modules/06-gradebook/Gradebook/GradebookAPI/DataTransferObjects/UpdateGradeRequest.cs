using System.ComponentModel.DataAnnotations;

namespace GradebookAPI.DataTransferObjects;

public class UpdateGradeRequest
{
    [Range(typeof(decimal), "0", "100000")]
    public decimal Score { get; set; }
}