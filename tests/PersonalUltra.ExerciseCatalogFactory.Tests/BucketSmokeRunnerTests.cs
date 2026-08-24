using System.Net;
using System.Security.Cryptography;
using PersonalUltra.ExerciseCatalogFactory.Configuration;
using PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class BucketSmokeRunnerTests
{
    [Fact]
    public async Task Smoke_verifies_every_stage_and_deletes_exactly_its_object()
    {
        await using var store = new RecordingObjectStore();
        using var http = new HttpClient(new PresignedHandler(store));
        var runner = new BucketSmokeRunner(store, http, Options());

        var report = await runner.RunAsync("20260815010101-abcdef12", CancellationToken.None);

        Assert.Equal(
            ["PUT", "HEAD", "GET", "PRESIGNED GET", "DELETE", "CONFIRM NOT FOUND"],
            report.Steps.Select(step => step.Operation));
        Assert.Null(store.Content);
        Assert.Single(store.DeletedKeys);
        Assert.StartsWith("smoke/20260815010101-abcdef12/", store.DeletedKeys[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Smoke_runs_exact_cleanup_when_a_validation_fails()
    {
        await using var store = new RecordingObjectStore { CorruptAuthenticatedGet = true };
        using var http = new HttpClient(new PresignedHandler(store));
        var runner = new BucketSmokeRunner(store, http, Options());

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            runner.RunAsync("20260815010101-abcdef12", CancellationToken.None));

        Assert.Null(store.Content);
        Assert.Single(store.DeletedKeys);
        Assert.Equal(store.PutKey, store.DeletedKeys[0]);
    }

    [Fact]
    public async Task Smoke_preserves_primary_and_delete_failure_as_possible_orphan()
    {
        await using var store = new RecordingObjectStore
        {
            CorruptAuthenticatedGet = true,
            FailDelete = true
        };
        using var http = new HttpClient(new PresignedHandler(store));
        var runner = new BucketSmokeRunner(store, http, Options());

        var exception = await Assert.ThrowsAsync<BucketSmokeException>(() =>
            runner.RunAsync("20260815010101-abcdef12", CancellationToken.None));

        Assert.IsType<InvalidDataException>(exception.PrimaryFailure);
        Assert.Equal("DELETE", exception.CleanupFailure.Stage);
        Assert.True(exception.CleanupFailure.PossibleOrphan);
        Assert.IsType<BucketOperationException>(exception.CleanupFailure.InnerException);
        Assert.Single(store.DeletedKeys);
        Assert.Equal(store.PutKey, store.DeletedKeys[0]);

        var description = Cli.BucketCommands.DescribeSmokeFailure(exception);
        Assert.Contains("primary=(operation=validation", description);
        Assert.Contains("cleanupStage=DELETE", description);
        Assert.Contains("status=403", description);
        Assert.Contains("requestId=reques…[REDACTED]", description);
        Assert.Contains("possibleOrphan=true", description);
        Assert.DoesNotContain(store.PutKey!, description, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Smoke_preserves_primary_and_confirmation_timeout_as_possible_orphan()
    {
        await using var store = new RecordingObjectStore
        {
            CorruptAuthenticatedGet = true,
            CancelConfirmation = true
        };
        using var http = new HttpClient(new PresignedHandler(store));
        var runner = new BucketSmokeRunner(store, http, Options());

        var exception = await Assert.ThrowsAsync<BucketSmokeException>(() =>
            runner.RunAsync("20260815010101-abcdef12", CancellationToken.None));

        Assert.IsType<InvalidDataException>(exception.PrimaryFailure);
        Assert.Equal("CONFIRM NOT FOUND", exception.CleanupFailure.Stage);
        Assert.True(exception.CleanupFailure.PossibleOrphan);
        Assert.IsType<OperationCanceledException>(exception.CleanupFailure.InnerException);
        Assert.Single(store.DeletedKeys);
        Assert.Equal(store.PutKey, store.DeletedKeys[0]);

        var description = Cli.BucketCommands.DescribeSmokeFailure(exception);
        Assert.Contains("primary=(operation=validation", description);
        Assert.Contains("cleanupStage=CONFIRM NOT FOUND", description);
        Assert.Contains("operation=cancelled-or-timeout", description);
        Assert.Contains("possibleOrphan=true", description);
        Assert.DoesNotContain(store.PutKey!, description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Smoke_never_deletes_when_conditional_put_did_not_create_the_object()
    {
        await using var store = new RecordingObjectStore { FailPutCollision = true };
        using var http = new HttpClient(new PresignedHandler(store));
        var runner = new BucketSmokeRunner(store, http, Options());

        await Assert.ThrowsAsync<BucketObjectCollisionException>(() =>
            runner.RunAsync("20260815010101-abcdef12", CancellationToken.None));

        Assert.Empty(store.DeletedKeys);
        Assert.Null(store.Content);
    }

    [Fact]
    public async Task Cli_smoke_is_dry_run_without_execute_and_never_creates_a_store()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var created = false;
        var settings = FactorySettingsTests.CreateSettings(
            bucketName: "bucket",
            bucketAccessKeyId: "access",
            bucketSecretAccessKey: "secret");
        var command = new Cli.BucketCommands(
            settings,
            output,
            error,
            (_, _) =>
            {
                created = true;
                return new RecordingObjectStore();
            });

        var exit = await command.RunAsync(["smoke"], CancellationToken.None);

        Assert.Equal(0, exit);
        Assert.False(created);
        Assert.Contains("DRY-RUN", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    private static BucketOptions Options() =>
        new("https://example.invalid", "auto", false, TimeSpan.FromMinutes(5));

    private sealed class RecordingObjectStore : IObjectStore
    {
        internal byte[]? Content { get; private set; }
        internal string? ContentType { get; private set; }
        internal string? Sha256 { get; private set; }
        internal string? PutKey { get; private set; }
        internal List<string> DeletedKeys { get; } = [];
        internal bool CorruptAuthenticatedGet { get; init; }
        internal bool FailDelete { get; init; }
        internal bool CancelConfirmation { get; init; }
        internal bool FailPutCollision { get; init; }

        public Task<ObjectStoreResult> ProbeAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new ObjectStoreResult("probe-request"));

        public Task<ObjectStoreResult> PutAsync(
            ObjectKey key,
            ReadOnlyMemory<byte> content,
            string contentType,
            string sha256,
            CancellationToken cancellationToken)
        {
            if (FailPutCollision)
                throw new BucketObjectCollisionException(new InvalidOperationException("precondition failed"));
            PutKey = key.Value;
            Content = content.ToArray();
            ContentType = contentType;
            Sha256 = sha256;
            return Task.FromResult(new ObjectStoreResult("put-request"));
        }

        public Task<ObjectMetadata?> HeadAsync(ObjectKey key, CancellationToken cancellationToken)
        {
            if (CancelConfirmation && DeletedKeys.Count > 0)
            {
                throw new OperationCanceledException("confirmation timed out", cancellationToken);
            }

            ObjectMetadata? result = Content is null
                ? null
                : new ObjectMetadata(Content.Length, ContentType, Sha256, "head-request");
            return Task.FromResult(result);
        }

        public Task<ObjectContent> GetAsync(ObjectKey key, CancellationToken cancellationToken)
        {
            var bytes = CorruptAuthenticatedGet ? [0x01] : Content!.ToArray();
            return Task.FromResult(new ObjectContent(bytes, ContentType, "get-request"));
        }

        public Uri CreatePresignedGetUri(ObjectKey key, DateTimeOffset expiresAt) =>
            new("https://example.invalid/signed-object?signature=must-not-be-logged");

        public Task<ObjectStoreResult> DeleteAsync(ObjectKey key, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(key.Value);
            if (FailDelete)
            {
                throw new BucketOperationException(
                    "DELETE object",
                    HttpStatusCode.Forbidden,
                    "request-id-never-fully-shown",
                    "AccessDenied",
                    "Acesso negado pelo provider.",
                    new InvalidOperationException("secret URL and object key omitted"));
            }

            Content = null;
            return Task.FromResult(new ObjectStoreResult("delete-request"));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class PresignedHandler(RecordingObjectStore store) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(store.Content!.ToArray())
            };
            response.Content.Headers.ContentType =
                System.Net.Http.Headers.MediaTypeHeaderValue.Parse(store.ContentType!);
            return Task.FromResult(response);
        }
    }
}
