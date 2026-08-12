using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;
namespace PersonalUltra.TrainerApi.Endpoints;
public static class NutritionProgressEndpointExtensions
{
 public static void MapNutritionProgressApi(this WebApplication app)
 {
  var api=app.MapGroup("/api/v1/students").RequireAuthorization();
  api.MapGet("/{studentId:guid}/nutrition", async(Guid studentId,PersonalUltraDbContext db,ClaimsPrincipal user,HttpContext c,CancellationToken ct)=>{var tid=Id(user);if(!await Own(db,tid,studentId,ct))return c.ApiError("STUDENT_NOT_FOUND","Aluno não encontrado.",404);var p=await db.NutritionPlans.AsNoTracking().Include(x=>x.Meals).ThenInclude(x=>x.Foods).SingleOrDefaultAsync(x=>x.StudentId==studentId,ct);return p is null?Results.Ok<NutritionPlanResponse?>(null):Results.Ok(To(p));});
  api.MapPut("/{studentId:guid}/nutrition", async(Guid studentId,NutritionPlanRequest request,PersonalUltraDbContext db,ClaimsPrincipal user,TimeProvider clock,HttpContext c,CancellationToken ct)=>{var tid=Id(user);if(!await Own(db,tid,studentId,ct))return c.ApiError("STUDENT_NOT_FOUND","Aluno não encontrado.",404);if(string.IsNullOrWhiteSpace(request.Name)||request.Meals.Count<1)return c.ApiError("VALIDATION_ERROR","Informe o nome e ao menos uma refeição.",400);var p=await db.NutritionPlans.Include(x=>x.Meals).ThenInclude(x=>x.Foods).SingleOrDefaultAsync(x=>x.StudentId==studentId,ct);if(p is null){p=new NutritionPlan{Id=Guid.NewGuid(),TrainerId=tid,StudentId=studentId};db.NutritionPlans.Add(p);}p.Name=request.Name.Trim();p.Notes=request.Notes?.Trim()??"";p.UpdatedAt=clock.GetUtcNow();db.MealFoods.RemoveRange(p.Meals.SelectMany(x=>x.Foods));db.Meals.RemoveRange(p.Meals);p.Meals.Clear();foreach(var (m,i) in request.Meals.OrderBy(x=>x.Sequence).Select((x,i)=>(x,i))){var meal=new Meal{Id=Guid.NewGuid(),NutritionPlanId=p.Id,Name=m.Name.Trim(),Sequence=i+1,Notes=m.Notes?.Trim()??""};meal.Foods.AddRange(m.Foods.Select(f=>new MealFood{Id=Guid.NewGuid(),MealId=meal.Id,FoodName=f.FoodName.Trim(),QuantityGrams=f.QuantityGrams}));p.Meals.Add(meal);}await db.SaveChangesAsync(ct);return Results.Ok(To(p));});
  api.MapGet("/{studentId:guid}/progress/weight", async(Guid studentId,PersonalUltraDbContext db,ClaimsPrincipal user,HttpContext c,CancellationToken ct)=>{if(!await Own(db,Id(user),studentId,ct))return c.ApiError("STUDENT_NOT_FOUND","Aluno não encontrado.",404);return Results.Ok(await db.WeightEntries.AsNoTracking().Where(x=>x.StudentId==studentId).OrderBy(x=>x.RecordedAt).Select(x=>new WeightResponse(x.Id,x.WeightKg,x.RecordedAt)).ToListAsync(ct));});
 }
 static Guid Id(ClaimsPrincipal u)=>Guid.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);static Task<bool> Own(PersonalUltraDbContext db,Guid t,Guid s,CancellationToken ct)=>db.TrainerStudents.AnyAsync(x=>x.TrainerId==t&&x.StudentId==s&&x.EndedAt==null,ct);static NutritionPlanResponse To(NutritionPlan p)=>new(p.Id,p.Name,p.Notes,p.Meals.OrderBy(x=>x.Sequence).Select(m=>new MealResponse(m.Id,m.Name,m.Sequence,m.Notes,m.Foods.Select(f=>new MealFoodInput(f.FoodName,f.QuantityGrams)).ToArray())).ToArray());
}
