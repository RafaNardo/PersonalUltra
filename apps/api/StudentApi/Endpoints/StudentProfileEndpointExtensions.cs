using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Infrastructure;
using PersonalUltra.StudentApi.Contracts;

namespace PersonalUltra.StudentApi.Endpoints;

public static class StudentProfileEndpointExtensions
{
    public static void MapStudentProfileApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/profile").RequireAuthorization();
        api.MapGet("", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!TryGetStudent(user, out var id)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão válida.", 403);
            var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            return student is null ? Results.NotFound() : Results.Ok(ToResponse(student));
        });
        api.MapPut("", async (UpdateStudentProfileRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!TryGetStudent(user, out var id)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão válida.", 403);
            var preferredName = request.PreferredName?.Trim();
            if (preferredName?.Length > 100) return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", "O nome de preferência deve ter até 100 caracteres.", 400);
            var student = await db.Students.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (student is null) return Results.NotFound();
            student.PreferredName = string.IsNullOrWhiteSpace(preferredName) ? null : preferredName;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(student));
        });
    }

    private static StudentProfileResponse ToResponse(PersonalUltra.Domain.Student student) =>
        new(student.FirstName, student.LastName, student.Email, student.Phone, student.PreferredName);

    private static bool TryGetStudent(ClaimsPrincipal user, out Guid id) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out id) && user.FindFirstValue("subject") == "student";
}
