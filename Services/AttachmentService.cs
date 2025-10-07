using Jobick.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;

namespace Jobick.Services;

/// <summary>
/// Concrete implementation persisting files under the app content root in an "Attachments" folder.
/// </summary>
public class AttachmentService(IWebHostEnvironment env) : IAttachmentService
{
    private string GetAttachmentsRoot()
    {
        var root = Path.Combine(env.ContentRootPath, "Attachments");
        if (!Directory.Exists(root))
        {
            Directory.CreateDirectory(root);
        }
        return root;
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
        var folder = GetAttachmentsRoot();
        var originalName = SanitizeFileName(file.FileName);
        var unique = $"{Guid.NewGuid():N}_{originalName}";
        var absPath = Path.Combine(folder, unique);
        await using (var stream = System.IO.File.Create(absPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }
        var relPath = Path.Combine("Attachments", unique);
        return relPath.Replace('\\', '/');
    }

    public bool TryGetDownloadInfo(string? relativePath, int? idForFallback, out AttachmentDownloadInfo? info)
    {
        info = null;
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var absPath = Path.Combine(env.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(absPath))
            return false;

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
