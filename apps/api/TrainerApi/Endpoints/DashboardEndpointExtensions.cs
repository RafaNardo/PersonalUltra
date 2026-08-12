using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.TrainerApi.Contracts;
using PersonalUltra.Infrastructure;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class DashboardEndpointExtensions
{
    public static void MapDashboardApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireAuthorization();

        api.MapGet("/dashboard", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var trainerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var trainer = await db.Trainers.AsNoTracking().SingleAsync(x => x.Id == trainerId, cancellationToken);
            var students = await db.TrainerStudents.AsNoTracking()
                .Where(link => link.TrainerId == trainerId && link.EndedAt == null)
                .OrderByDescending(link => link.StartedAt)
                .Select(link => new DashboardStudentSummary(
                    link.StudentId,
                    link.Student.FirstName,
                    link.Student.LastName,
                    link.Student.Email,
                    link.Student.Anamnesis == null
                        ? "NotStarted"
                        : link.Student.Anamnesis.CompletedAt == null ? "InProgress" : "Completed",
                    link.StartedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(new DashboardResponse(
                trainer.Name,
                students.Count,
                students.Count(student => student.AnamnesisStatus is not "Completed"),
                students.Count(student => student.AnamnesisStatus is "Completed"),
                students.Take(5).ToArray()));
        });
    }
}
