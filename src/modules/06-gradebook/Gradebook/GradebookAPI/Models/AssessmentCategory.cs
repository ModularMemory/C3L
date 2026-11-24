using System.Text.Json.Serialization;

namespace GradebookAPI.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssessmentCategory
{
    Undefined = -1,
    Assignment,
    Test,
    Exam,
    Essay,
    Quiz,
    Project
}