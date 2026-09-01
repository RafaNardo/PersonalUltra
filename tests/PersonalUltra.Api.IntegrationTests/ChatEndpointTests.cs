using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class ChatEndpointTests
{
    [Fact]
    public async Task Student_and_trainer_roundtrip_persists_messages_in_order()
    {
        await using var environment = new NutritionTestEnvironment();
        var student = environment.CreateStudentClient();
        var trainer = environment.CreateTrainerClient();
        trainer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);
        await LoginStudent(student);

        var studentSend = await student.PostAsJsonAsync("/api/v1/chat", new { content = "  Olá, personal!  " });
        Assert.Equal(HttpStatusCode.Created, studentSend.StatusCode);
        var studentMessage = await studentSend.Content.ReadFromJsonAsync<StudentChatMessageResponse>();
        Assert.NotNull(studentMessage);
        Assert.Equal("Student", studentMessage!.Sender);
        Assert.Equal("Olá, personal!", studentMessage.Content);

        var trainerSend = await trainer.PostAsJsonAsync($"/api/v1/students/{DemoIds.StudentId}/chat", new { content = "Olá! Vou acompanhar sua evolução." });
        Assert.Equal(HttpStatusCode.Created, trainerSend.StatusCode);
        var trainerMessage = await trainerSend.Content.ReadFromJsonAsync<TrainerChatMessageResponse>();
        Assert.NotNull(trainerMessage);
        Assert.Equal("Trainer", trainerMessage!.Sender);

        var studentHistory = await student.GetFromJsonAsync<StudentChatResponse>("/api/v1/chat");
        Assert.NotNull(studentHistory);
        Assert.Equal(["Student", "Trainer"], studentHistory!.Messages.Select(x => x.Sender));
        Assert.Equal(["Olá, personal!", "Olá! Vou acompanhar sua evolução."], studentHistory.Messages.Select(x => x.Content));

        var trainerHistory = await trainer.GetFromJsonAsync<List<TrainerChatMessageResponse>>($"/api/v1/students/{DemoIds.StudentId}/chat");
        Assert.NotNull(trainerHistory);
        Assert.Equal([studentMessage.Id, trainerMessage.Id], trainerHistory!.Select(x => x.Id));

        await using var scope = environment.TrainerServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var persisted = await db.ChatMessages.Where(x => x.StudentId == DemoIds.StudentId).OrderBy(x => x.CreatedAt).ToListAsync();
        Assert.Equal(2, persisted.Count);
        Assert.Equal([ChatMessageSender.Student, ChatMessageSender.Trainer], persisted.Select(x => x.Sender));
    }

    [Fact]
    public async Task Trainer_cannot_read_chat_for_a_student_outside_their_active_roster()
    {
        await using var environment = new NutritionTestEnvironment();
        var trainer = environment.CreateTrainerClient();
        trainer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);
        var otherTrainerId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();

        await using (var scope = environment.TrainerServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.Trainers.Add(new Trainer { Id = otherTrainerId, Name = "Outro personal", CreatedAt = DateTimeOffset.UtcNow });
            db.Students.Add(new Student { Id = otherStudentId, FirstName = "Outro", LastName = "Aluno", CreatedAt = DateTimeOffset.UtcNow });
            db.TrainerStudents.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = otherTrainerId, StudentId = otherStudentId, StartedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var response = await trainer.GetAsync($"/api/v1/students/{otherStudentId}/chat");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("STUDENT_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task Chat_rejects_blank_and_overlong_messages_for_both_actors()
    {
        await using var environment = new NutritionTestEnvironment();
        var student = environment.CreateStudentClient();
        var trainer = environment.CreateTrainerClient();
        trainer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);
        await LoginStudent(student);
        var overlong = new string('x', 1001);

        foreach (var content in new[] { " ", overlong })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await student.PostAsJsonAsync("/api/v1/chat", new { content })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await trainer.PostAsJsonAsync($"/api/v1/students/{DemoIds.StudentId}/chat", new { content })).StatusCode);
        }
    }

    [Fact]
    public async Task Student_chat_returns_the_optional_trainer_phone()
    {
        await using var environment = new NutritionTestEnvironment();
        var student = environment.CreateStudentClient();
        _ = environment.CreateTrainerClient();
        const string phone = "+5511999990000";

        await using (var scope = environment.TrainerServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var trainer = await db.Trainers.SingleAsync(x => x.Id == DemoIds.TrainerId);
            trainer.Phone = phone;
            await db.SaveChangesAsync();
        }

        await LoginStudent(student);
        var response = await student.GetFromJsonAsync<StudentChatResponse>("/api/v1/chat");

        Assert.NotNull(response);
        Assert.Equal(phone, response!.TrainerPhone);
    }

    private static async Task LoginStudent(HttpClient student)
    {
        var login = await student.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<LoginResponse>();
        student.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
    }

    private sealed record LoginResponse(string AccessToken);
    private sealed record ErrorResponse(string Code, string Message, object Details, string TraceId);
    private sealed record StudentChatResponse(string? TrainerPhone, IReadOnlyList<StudentChatMessageResponse> Messages);
    private sealed record StudentChatMessageResponse(Guid Id, string Sender, string Content, DateTimeOffset CreatedAt);
    private sealed record TrainerChatMessageResponse(Guid Id, Guid StudentId, string Sender, string Content, DateTimeOffset CreatedAt);
}
