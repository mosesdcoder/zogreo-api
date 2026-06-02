using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Infrastructure.Files;

public class LocalFileStorage(IConfiguration config, IHostEnvironment env) : IFileStorage
{
    public async Task<string> SaveAsync(IFileProxy file, string folder, CancellationToken ct = default)
    {
        var uploadsRoot = config["FileStorage:UploadsPath"] ?? "uploads";
        var dir = Path.Combine(env.ContentRootPath, uploadsRoot, folder);
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);

        var baseUrl = config["FileStorage:BaseUrl"] ?? "";
        return $"{baseUrl.TrimEnd('/')}/{folder}/{fileName}";
    }
}
