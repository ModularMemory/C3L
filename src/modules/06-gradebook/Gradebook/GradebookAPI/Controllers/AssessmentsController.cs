using GradebookAPI.Data;
using GradebookAPI.DataTransferObjects;
using GradebookAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradebookAPI.Controllers;

public class AssessmentsController : V1ApiController
{
    private readonly GradebookDbContext _context;

    public AssessmentsController(GradebookDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Assessment))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssessmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CourseId))
        {
            return BadRequest("CourseId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        if (!Enum.IsDefined(request.Category) ||
            request.Category == AssessmentCategory.Undefined)
        {
            return BadRequest("A valid assessment category is required.");
        }

        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            CourseId = request.CourseId.Trim(),
            Title = request.Title.Trim(),
            Category = request.Category,
            MaxScore = request.MaxScore,
            WeightPercentage = request.WeightPercentage,
            DueDate = request.DueDate
        };

        _context.Assessments.Add(assessment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = assessment.Id },
            assessment
        );
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Assessment))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromQuery] Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("A valid assessment ID is required.");
        }

        var assessment = await _context.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assessment == null)
        {
            return NotFound("Assessment not found.");
        }

        return Ok(assessment);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Assessment>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByCourseId([FromQuery] string courseId)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest("CourseId is required.");

        var assessments = await _context.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId.Trim())
            .OrderBy(a => a.Title)
            .ToListAsync();

        return Ok(assessments);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Assessment))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromQuery] Guid id,
        [FromBody] UpdateAssessmentRequest request)
    {
        if (id == Guid.Empty)
            return BadRequest("A valid assessment ID is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");

        if (!Enum.IsDefined(request.Category) ||
            request.Category == AssessmentCategory.Undefined)
            return BadRequest("A valid assessment category is required.");

        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assessment == null)
            return NotFound("Assessment not found.");

        assessment.Title = request.Title.Trim();
        assessment.Category = request.Category;
        assessment.MaxScore = request.MaxScore;
        assessment.WeightPercentage = request.WeightPercentage;
        assessment.DueDate = request.DueDate;

        await _context.SaveChangesAsync();

        return Ok(assessment);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromQuery] Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest("A valid assessment ID is required.");

        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assessment == null)
            return NotFound("Assessment not found.");

        var hasGrades = await _context.Grades
            .AnyAsync(g => g.AssessmentId == id);

        if (hasGrades)
            return Conflict(
                "This assessment cannot be deleted because it has recorded grades.");

        _context.Assessments.Remove(assessment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}