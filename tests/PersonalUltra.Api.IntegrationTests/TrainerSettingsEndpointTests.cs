extern alias trainerapi;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class TrainerSettingsEndpointTests : IClassFixture<TrainerApiFactory>
{
    private readonly HttpClient client;
    private readonly TrainerApiFactory factory;

    public TrainerSettingsEndpointTests(TrainerApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);
    }

    [Fact]
    public async Task Prescription_settings_return_defaults_then_persist_for_authenticated_trainer()
    {
        await RemoveDemoSettingsAsync();

        var initial = await client.GetFromJsonAsync<PrescriptionSettingsResponse>("/api/v1/settings/prescription");
        Assert.Equal(new PrescriptionSettingsResponse(3, 8, 12, 60, false), initial);

        var response = await client.PutAsJsonAsync("/api/v1/settings/prescription", new
        {
            sets = 4,
            repetitionsMin = 6,
            repetitionsMax = 10,
            restSeconds = 90,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            new PrescriptionSettingsResponse(4, 6, 10, 90, true),
            await response.Content.ReadFromJsonAsync<PrescriptionSettingsResponse>());

        var persisted = await client.GetFromJsonAsync<PrescriptionSettingsResponse>("/api/v1/settings/prescription");
        Assert.Equal(new PrescriptionSettingsResponse(4, 6, 10, 90, true), persisted);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var entity = await db.TrainerPrescriptionSettings.SingleAsync(x => x.TrainerId == DemoIds.TrainerId);
        Assert.Equal(4, entity.Sets);
        Assert.Equal(6, entity.RepetitionsMin);
        Assert.Equal(10, entity.RepetitionsMax);
        Assert.Equal(90, entity.RestSeconds);
    }

    [Fact]
    public async Task Updating_prescription_settings_does_not_change_another_trainers_row()
    {
        await RemoveDemoSettingsAsync();
        var otherTrainerId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.Trainers.Add(new Trainer { Id = otherTrainerId, Name = "Outro Trainer", CreatedAt = DateTimeOffset.UtcNow });
            db.TrainerPrescriptionSettings.Add(new TrainerPrescriptionSettings
            {
                Id = Guid.NewGuid(), TrainerId = otherTrainerId, Sets = 5,
                RepetitionsMin = 5, RepetitionsMax = 5, RestSeconds = 120,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        try
        {
            var response = await client.PutAsJsonAsync("/api/v1/settings/prescription", new
            {
                sets = 2,
                repetitionsMin = 12,
                repetitionsMax = 15,
                restSeconds = 45,
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var other = await db.TrainerPrescriptionSettings.SingleAsync(x => x.TrainerId == otherTrainerId);
            Assert.Equal((5, 5, 5, 120), (other.Sets, other.RepetitionsMin, other.RepetitionsMax, other.RestSeconds));
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.TrainerPrescriptionSettings.RemoveRange(db.TrainerPrescriptionSettings.Where(x => x.TrainerId == otherTrainerId));
            db.Trainers.RemoveRange(db.Trainers.Where(x => x.Id == otherTrainerId));
            await db.SaveChangesAsync();
        }
    }

    [Theory]
    [InlineData(0, 8, 12, 60)]
    [InlineData(3, 13, 12, 60)]
    [InlineData(3, 8, 101, 60)]
    [InlineData(3, 8, 12, 901)]
    public async Task Prescription_settings_reject_invalid_values(
        int sets, int repetitionsMin, int repetitionsMax, int restSeconds)
    {
        var response = await client.PutAsJsonAsync("/api/v1/settings/prescription", new
        {
            sets,
            repetitionsMin,
            repetitionsMax,
            restSeconds,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Prescription_settings_require_trainer_authentication()
    {
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync("/api/v1/settings/prescription");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task RemoveDemoSettingsAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        db.TrainerPrescriptionSettings.RemoveRange(
            db.TrainerPrescriptionSettings.Where(x => x.TrainerId == DemoIds.TrainerId));
        await db.SaveChangesAsync();
    }

    private sealed record PrescriptionSettingsResponse(
        int Sets,
        int RepetitionsMin,
        int RepetitionsMax,
        int RestSeconds,
        bool IsCustomized);
}
