using GradebookAPI.Data;
using GradebookAPI.DataTransferObjects;
using GradebookAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradebookAPI.Controllers;

public class GradesController : V1ApiController
{
    private readonly ILogger<GradesController> _logger;
    private readonly GradebookDbContext _context;

    public GradesController(
        ILogger<GradesController> logger,
        GradebookDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Grade))]
    public async Task<IActionResult> Create(
        [FromBody] CreateGradeRequest request)
    {
        // Validate the student identifier.
        if (string.IsNullOrWhiteSpace(request.StudentId))
        {
            return BadRequest("Student ID is required.");
        }

        // Validate the assessment identifier.
        if (request.AssessmentId == Guid.Empty)
        {
            return BadRequest("A valid assessment ID is required.");
        }

        // Validate the score.
        if (request.Score is < 0 or > 100000)
        {
            return BadRequest(
                "Score must be between 0 and 100000.");
        }

        // Find the assessment being graded.
        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId);

        if (assessment == null)
        {
            return NotFound("Assessment not found.");
        }

        // The score cannot exceed the assessment's maximum.
        if (request.Score > assessment.MaxScore)
        {
            return BadRequest(
                "Score cannot exceed the assessment's maximum score.");
        }

        // A student can only have one grade per assessment.
        var gradeExists = await _context.Grades
            .AnyAsync(g =>
                g.StudentId == request.StudentId.Trim() &&
                g.AssessmentId == request.AssessmentId);

        if (gradeExists)
        {
            return Conflict(
                "A grade already exists for this student and assessment.");
        }

        // Create the grade record.
        var grade = new Grade
        {
            Id = Guid.NewGuid(),
            StudentId = request.StudentId.Trim(),
            AssessmentId = request.AssessmentId,
            Score = request.Score,
            GradedAt = DateTimeOffset.UtcNow
        };

        // Save the grade to SQLite.
        _context.Grades.Add(grade);
        await _context.SaveChangesAsync();

        // Log successful grade creation.
        _logger.LogInformation(
            "Created grade {GradeId} for assessment {AssessmentId}",
            grade.Id,
            grade.AssessmentId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = grade.Id },
            grade);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Grade))]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("A valid grade ID is required.");
        }

        var grade = await _context.Grades
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grade == null)
        {
            return NotFound("Grade not found.");
        }

        return Ok(grade);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Grade>))]
    public async Task<IActionResult> GetByAssessmentId(Guid assessmentId)
    {
        // Validate the assessment identifier.
        if (assessmentId == Guid.Empty)
        {
            return BadRequest("A valid assessment ID is required.");
        }

        // Check that the assessment exists.
        var assessmentExists = await _context.Assessments
            .AnyAsync(a => a.Id == assessmentId);

        if (!assessmentExists)
        {
            return NotFound("Assessment not found.");
        }

        // Retrieve all grades associated with the assessment.
        var grades = await _context.Grades
            .AsNoTracking()
            .Where(g => g.AssessmentId == assessmentId)
            .OrderBy(g => g.StudentId)
            .ToListAsync();

        return Ok(grades);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Grade>))]
    public async Task<IActionResult> GetByStudentId(string studentId)
    {
        // Validate the student identifier.
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return BadRequest("Student ID is required.");
        }

        // Remove unnecessary whitespace.
        studentId = studentId.Trim();

        // Retrieve all grades belonging to the student.
        var grades = await _context.Grades
            .AsNoTracking()
            .Where(g => g.StudentId == studentId)
            .OrderBy(g => g.AssessmentId)
            .ToListAsync();

        return Ok(grades);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Grade>))]
    public async Task<IActionResult> GetByCourseId(string courseId)
    {
        // Validate the course identifier.
        if (string.IsNullOrWhiteSpace(courseId))
        {
            return BadRequest("Course ID is required.");
        }

        // Remove unnecessary whitespace.
        courseId = courseId.Trim();

        // Find assessments belonging to the course.
        var assessmentIds = await _context.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .Select(a => a.Id)
            .ToListAsync();

        // Retrieve grades associated with those assessments.
        var grades = await _context.Grades
            .AsNoTracking()
            .Where(g => assessmentIds.Contains(g.AssessmentId))
            .OrderBy(g => g.AssessmentId)
            .ThenBy(g => g.StudentId)
            .ToListAsync();

        return Ok(grades);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Grade))]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateGradeRequest request)
    {
        // Validate the grade identifier.
        if (id == Guid.Empty)
        {
            return BadRequest("A valid grade ID is required.");
        }

        // Validate the score.
        if (request.Score is < 0 or > 100000)
        {
            return BadRequest("Score must be between 0 and 100000.");
        }

        // Find the grade being updated.
        var grade = await _context.Grades
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grade == null)
        {
            return NotFound("Grade not found.");
        }

        // Find the assessment associated with the grade.
        var assessment = await _context.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == grade.AssessmentId);

        if (assessment == null)
        {
            return NotFound("Assessment not found.");
        }

        // Ensure the new score does not exceed the maximum.
        if (request.Score > assessment.MaxScore)
        {
            return BadRequest(
                "Score cannot exceed the assessment's maximum score.");
        }

        // Update the score and grading timestamp.
        grade.Score = request.Score;
        grade.GradedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        // Log the successful update.
        _logger.LogInformation(
            "Updated grade {GradeId} for assessment {AssessmentId}",
            grade.Id,
            grade.AssessmentId);

        return Ok(grade);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Validate the grade identifier.
        if (id == Guid.Empty)
        {
            return BadRequest("A valid grade ID is required.");
        }

        // Find the grade being deleted.
        var grade = await _context.Grades
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grade == null)
        {
            return NotFound("Grade not found.");
        }

        // Remove the grade from SQLite.
        _context.Grades.Remove(grade);
        await _context.SaveChangesAsync();

        // Log the successful deletion.
        _logger.LogInformation(
            "Deleted grade {GradeId} for assessment {AssessmentId}",
            grade.Id,
            grade.AssessmentId);

        return NoContent();
    }
}