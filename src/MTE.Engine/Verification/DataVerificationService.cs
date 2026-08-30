using System.Security.Cryptography;
using MTE.Core.Interfaces;
using MTE.Core.Models;

namespace MTE.Engine.Verification;

public sealed class DataVerificationService : IDataVerificationService
{
    public async Task<VerificationResult> VerifyFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
        {
            return new VerificationResult
            {
                SourcePath = sourcePath,
                DestinationPath = destinationPath,
                Exists = false,
                HashMatches = false,
                Timestamp = DateTime.Now
            };
        }

        if (!File.Exists(destinationPath))
        {
            return new VerificationResult
            {
                SourcePath = sourcePath,
                DestinationPath = destinationPath,
                FileSize = new FileInfo(sourcePath).Length,
                Exists = false,
                HashMatches = false,
                Timestamp = DateTime.Now
            };
        }

        var sourceInfo = new FileInfo(sourcePath);
        var destinationInfo = new FileInfo(destinationPath);

        if (sourceInfo.Length != destinationInfo.Length)
        {
            return new VerificationResult
            {
                SourcePath = sourcePath,
                DestinationPath = destinationPath,
                FileSize = sourceInfo.Length,
                Exists = true,
                HashMatches = false,
                SourceHash = string.Empty,
                DestinationHash = string.Empty,
                Timestamp = DateTime.Now
            };
        }

        var sourceHash = await CalculateSHA256Async(
            sourcePath,
            cancellationToken);

        var destinationHash = await CalculateSHA256Async(
            destinationPath,
            cancellationToken);

        return new VerificationResult
        {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            FileSize = sourceInfo.Length,
            SourceHash = sourceHash,
            DestinationHash = destinationHash,
            Exists = true,
            HashMatches = string.Equals(
                sourceHash,
                destinationHash,
                StringComparison.OrdinalIgnoreCase),
            Timestamp = DateTime.Now
        };
    }

    public async Task<string> CalculateSHA256Async(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 64,
            useAsync: true);

        using var sha256 = SHA256.Create();

        var hash = await sha256.ComputeHashAsync(
            stream,
            cancellationToken);

        return Convert.ToHexString(hash);
    }
}