using Microsoft.AspNetCore.Http;

namespace Jobick.Services.Interfaces;

/// <summary>
/// Provides a single place to manage task attachment storage and download resolution.
/// </summary>
public interface IAttachmentService
{
    /// <summary>
    /// Saves an uploaded form file to disk under the Attachments folder and returns the relative path (e.g. "Attachments/<guid>_OriginalName.ext").
    /// </summary>
    System.Threading.Tasks.Task<string> SaveAsync(IFormFile file, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to resolve a stored relative path to a downloadable file info.
    /// If the stored name contains an original name ("<GUID>_<Original>"), that name will be used; otherwise a fallback name based on the id is used.
    /// </summary>
    bool TryGetDownloadInfo(string? relativePath, int? idForFallback, out AttachmentDownloadInfo? info);
}

/// <summary>
/// Represents resolved information for downloading an attachment file.
/// </summary>
public sealed class AttachmentDownloadInfo
{
    public required string AbsolutePath { get; init; }
    public required string ContentType { get; init; }
    public required string DownloadName { get; init; }
}
