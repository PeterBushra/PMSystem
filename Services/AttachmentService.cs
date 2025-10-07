using Jobick.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Jobick.Services;

/// <summary>
/// Attachment storage service using a single configurable root folder for the whole app.
/// Reads the root from configuration key "Attachments:Root".
/// If not configured or relative, defaults to a folder named "Attachments" next to the executable directory.
/// Ensures the directory exists.
/// </summary>
public class AttachmentService : IAttachmentService
{
    private readonly string _attachmentsRoot;
    private readonly string _legacyContentRootAttachments;

    public AttachmentService(IWebHostEnvironment env, IConfiguration configuration)
    {
        // Read configured root path
        var configuredRoot = configuration["Attachments:Root"]; // may be null/empty

        // Choose base directory as the executable location to satisfy "behind the .exe"
        var baseDir = AppContext.BaseDirectory?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                      ?? env.ContentRootPath; // fallback just in case

        string resolvedRoot;
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            // No config provided: use <exe>/Attachments
            resolvedRoot = Path.Combine(baseDir, "Attachments");
        }
        else if (Path.IsPathRooted(configuredRoot))
        {
            // Absolute path: use as-is
            resolvedRoot = configuredRoot;
        }
        else
        {
            // Relative path: place it under the executable directory
            resolvedRoot = Path.Combine(baseDir, configuredRoot);
        }

        // Ensure the directory exists
        if (!Directory.Exists(resolvedRoot))
        {
            Directory.CreateDirectory(resolvedRoot);
        }

        _attachmentsRoot = resolvedRoot;
        _legacyContentRootAttachments = Path.Combine(env.ContentRootPath, "Attachments");
    }

    private static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file";

        var name = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }
        var sanitized = sb.ToString().Trim();
        if (sanitized.Length == 0)
            sanitized = "file";

        // keep last 180 chars to preserve extension
        return sanitized.Length > 180 ? sanitized[^180..] : sanitized;
    }

    public async System.Threading.Tasks.Task<string> SaveAsync(IFormFile file, System.Threading.CancellationToken cancellationToken = default)
    {
        var originalName = SanitizeFileName(file.FileName);
        var unique = $"{Guid.NewGuid():N}_{originalName}";
        var absPath = Path.Combine(_attachmentsRoot, unique);
        await using (var stream = System.IO.File.Create(absPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }
        // Store a relative-friendly identifier; keep backward compatibility by prefixing with "Attachments/" as before
        // Consumers resolve using only the file name portion.
        var stored = Path.Combine("Attachments", unique);
        return stored.Replace('\\', '/');
    }

    public bool TryGetDownloadInfo(string? relativePath, int? idForFallback, out AttachmentDownloadInfo? info)
    {
        info = null;
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        // Support both legacy stored values like "Attachments/xxxx.ext" and plain file names
        var fileName = Path.GetFileName(relativePath);
        var absPath = Path.Combine(_attachmentsRoot, fileName);
        if (!System.IO.File.Exists(absPath))
        {
            // Legacy fallback: look in content-root Attachments folder (old behavior)
            var legacyAttempt = Path.Combine(_legacyContentRootAttachments, fileName);
            if (System.IO.File.Exists(legacyAttempt))
            {
                absPath = legacyAttempt;
            }
            else
            {
                // As another legacy attempt, if the stored path was a full relative path under content root
                var legacyRelative = Path.Combine(_legacyContentRootAttachments, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(legacyRelative))
                {
                    absPath = legacyRelative;
                }
                else
                {
                    return false;
                }
            }
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(absPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var fileNameOnDisk = Path.GetFileName(absPath);
        string downloadName;
        var underscoreIndex = fileNameOnDisk.IndexOf('_');
        if (underscoreIndex > 0 && underscoreIndex < fileNameOnDisk.Length - 1)
        {
            downloadName = fileNameOnDisk[(underscoreIndex + 1)..];
        }
        else
        {
            var ext = Path.GetExtension(absPath) ?? string.Empty;
            var idPart = idForFallback.HasValue ? idForFallback.Value.ToString() : Guid.NewGuid().ToString("N");
            downloadName = $"Attachment_{idPart}{ext}";
        }

        info = new AttachmentDownloadInfo
        {
            AbsolutePath = absPath,
            ContentType = contentType,
            DownloadName = downloadName
        };
        return true;
    }
}
