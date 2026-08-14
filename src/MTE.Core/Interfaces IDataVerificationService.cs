using MTE.Core.Models;

namespace MTE.Core.Interfaces;

public interface IDataVerificationService
{
    Task<VerificationResult> VerifyFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default);

    Task<string> CalculateSHA256Async(
        string filePath,
        CancellationToken cancellationToken = default);
}