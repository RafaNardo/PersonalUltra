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
    }
}
