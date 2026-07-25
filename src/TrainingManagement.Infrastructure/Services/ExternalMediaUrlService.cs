using System.Text.RegularExpressions;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Infrastructure.Services;

public sealed partial class ExternalMediaUrlService : IExternalMediaUrlService
{
    public bool IsValidExternalUrl(string? url, LessonContentType contentType)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (contentType == LessonContentType.ExternalLink)
            return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
        return uri.Scheme == Uri.UriSchemeHttps;
    }

    public string? GetSafeEmbedUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return null;
        var host = uri.Host.ToLowerInvariant();
        string? id = null;
        if (host is "youtube.com" or "www.youtube.com")
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            id = query["v"];
            if (id is null && uri.AbsolutePath.StartsWith("/embed/", StringComparison.Ordinal))
                id = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (IsVideoId(id)) return $"https://www.youtube-nocookie.com/embed/{id}";
        }
        if (host == "youtu.be")
        {
            id = uri.AbsolutePath.Trim('/');
            if (IsVideoId(id)) return $"https://www.youtube-nocookie.com/embed/{id}";
        }
        if (host is "vimeo.com" or "www.vimeo.com")
        {
            id = uri.AbsolutePath.Trim('/').Split('/').LastOrDefault();
            if (id is not null && VimeoId().IsMatch(id)) return $"https://player.vimeo.com/video/{id}";
        }
        return null;
    }

    private static bool IsVideoId(string? value) => value is not null && YouTubeId().IsMatch(value);
    [GeneratedRegex("^[A-Za-z0-9_-]{6,20}$")]
    private static partial Regex YouTubeId();
    [GeneratedRegex("^[0-9]{5,15}$")]
    private static partial Regex VimeoId();
}
