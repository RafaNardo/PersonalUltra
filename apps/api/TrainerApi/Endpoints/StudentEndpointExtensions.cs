using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class StudentEndpointExtensions
{
    public static void MapStudentApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/students").RequireAuthorization();

        api.MapGet("/", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var trainerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var students = await db.TrainerStudents.AsNoTracking()
                .Where(link => link.TrainerId == trainerId && link.EndedAt == null)
                .OrderBy(link => link.Student.FirstName).ThenBy(link => link.Student.LastName)
                .Select(link => new TrainerStudentSummary(
                    link.StudentId,
                    link.Student.FirstName,
                    link.Student.LastName,
                    link.Student.Email,
                    link.Student.Anamnesis == null
                        ? "NotStarted"
                        : link.Student.Anamnesis.CompletedAt == null ? "InProgress" : "Completed",
                    link.StartedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(new StudentListResponse(students));
        });

        api.MapGet("/{studentId:guid}", async (Guid studentId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken cancellationToken) =>
        {
            var trainerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var student = await db.TrainerStudents.AsNoTracking()
                .Where(link => link.TrainerId == trainerId && link.StudentId == studentId && link.EndedAt == null)
                .Select(link => new StudentDetailResponse(
                    link.StudentId,
                    link.Student.FirstName,
                    link.Student.LastName,
                    link.Student.Email,
                    link.Student.Anamnesis == null
                        ? "NotStarted"
                        : link.Student.Anamnesis.CompletedAt == null ? "InProgress" : "Completed",
                    link.StartedAt))
                .SingleOrDefaultAsync(cancellationToken);

            return student is null
                ? context.ApiError("STUDENT_NOT_FOUND", "O aluno não foi encontrado no seu acompanhamento.", StatusCodes.Status404NotFound)
                : Results.Ok(student);
        });
    }
}
