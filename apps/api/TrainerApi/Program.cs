using Microsoft.EntityFrameworkCore;
using PersonalUltra.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PersonalUltraDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("PersonalUltraDatabase")));
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { actor = "trainer" }));
app.Run();

public partial class Program;
