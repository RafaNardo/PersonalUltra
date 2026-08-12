using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Infrastructure;
namespace PersonalUltra.TrainerApi.Endpoints;
public static class DemoResetEndpointExtensions
{
 public static void MapDemoResetApi(this WebApplication app){app.MapPost("/api/v1/demo/reset",async(PersonalUltraDbContext db,ClaimsPrincipal user,CancellationToken ct)=>{var trainerId=Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);var studentIds=await db.TrainerStudents.Where(x=>x.TrainerId==trainerId).Select(x=>x.StudentId).ToListAsync(ct);db.SetPerformances.RemoveRange(db.SetPerformances.Where(x=>studentIds.Contains(x.WorkoutSessionExercise.WorkoutSession.StudentId)));db.WorkoutSessionExercises.RemoveRange(db.WorkoutSessionExercises.Where(x=>studentIds.Contains(x.WorkoutSession.StudentId)));db.WorkoutSessions.RemoveRange(db.WorkoutSessions.Where(x=>studentIds.Contains(x.StudentId)));db.StudentWorkoutExercises.RemoveRange(db.StudentWorkoutExercises.Where(x=>studentIds.Contains(x.StudentWorkout.StudentId)));db.StudentWorkouts.RemoveRange(db.StudentWorkouts.Where(x=>studentIds.Contains(x.StudentId)));db.WeightEntries.RemoveRange(db.WeightEntries.Where(x=>studentIds.Contains(x.StudentId)));db.NutritionPlans.RemoveRange(db.NutritionPlans.Where(x=>studentIds.Contains(x.StudentId)));db.TrainerMessages.RemoveRange(db.TrainerMessages.Where(x=>x.TrainerId==trainerId));await db.SaveChangesAsync(ct);return Results.Ok(new{reset=true});}).RequireAuthorization();}
}
