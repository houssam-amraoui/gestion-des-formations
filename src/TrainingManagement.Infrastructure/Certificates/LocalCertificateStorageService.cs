using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TrainingManagement.Application.Certificates;

namespace TrainingManagement.Infrastructure.Certificates;

public sealed class LocalCertificateStorageService(
    IHostEnvironment environment, IOptions<CertificateStorageOptions> options) : ICertificateStorageService
{
    private readonly string root = ResolveRoot(environment.ContentRootPath, options.Value.BasePath);

    public async Task<string> SaveAsync(string serverFileName, byte[] content, CancellationToken token = default)
    {
        var safeName = Path.GetFileName(serverFileName);
        if (!string.Equals(safeName, serverFileName, StringComparison.Ordinal) ||
            !safeName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Nom de fichier de certificat invalide.");
        Directory.CreateDirectory(root);
        var path = SafePath(safeName);
        await File.WriteAllBytesAsync(path, content, token);
        return safeName;
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        var safeName = Path.GetFileName(relativePath);
        if (!string.Equals(safeName, relativePath, StringComparison.Ordinal))
            throw new InvalidOperationException("Chemin de certificat invalide.");
        var path = SafePath(safeName);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, token) : null;
    }

    private string SafePath(string name)
    {
        var path = Path.GetFullPath(Path.Combine(root, name));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chemin de certificat invalide.");
        return path;
    }

    private static string ResolveRoot(string contentRoot, string configured)
    {
        var value = Path.IsPathRooted(configured) ? configured : Path.Combine(contentRoot, configured);
        return Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar);
    }
}
