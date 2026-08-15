using System.Net;

namespace PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

internal sealed class BucketOperationException : Exception
{
    internal BucketOperationException(
        string operation,
        HttpStatusCode? statusCode,
        string? requestId,
        string? errorCode,
        string safeProviderMessage,
        Exception innerException)
        : base($"Operação S3 '{operation}' falhou" +
               (statusCode is null ? "." : $" com HTTP {(int)statusCode.Value}."), innerException)
    {
        Operation = operation;
        StatusCode = statusCode;
        RequestId = requestId;
        ErrorCode = errorCode;
        SafeProviderMessage = safeProviderMessage;
    }

    internal string Operation { get; }
    internal HttpStatusCode? StatusCode { get; }
    internal string? RequestId { get; }
    internal string? ErrorCode { get; }
    internal string SafeProviderMessage { get; }
}

internal sealed class BucketCleanupException : Exception
{
    internal BucketCleanupException(string stage, Exception innerException)
        : base("O cleanup delimitado do smoke falhou; a existência do objeto não pôde ser descartada.", innerException)
    {
        Stage = stage;
    }

    internal string Stage { get; }
    internal bool PossibleOrphan => true;
}

internal sealed class BucketSmokeException : Exception
{
    internal BucketSmokeException(Exception primaryFailure, BucketCleanupException cleanupFailure)
        : base("A operação primária e o cleanup delimitado do smoke falharam.")
    {
        PrimaryFailure = primaryFailure;
        CleanupFailure = cleanupFailure;
    }

    internal Exception PrimaryFailure { get; }
    internal BucketCleanupException CleanupFailure { get; }
}
