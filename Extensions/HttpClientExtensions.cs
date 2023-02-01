using System.Text.RegularExpressions;

namespace Duthie.Extensions;

public static class HttpClientExtensions
{
    public static async Task<string> GetStringAsync(this HttpClient httpClient, Uri? requestUri, bool removeComments)
    {
        var html = await httpClient.GetStringAsync(requestUri);
        return removeComments ? RemoveComments(html) : html;
    }

    public static async Task<string> GetStringAsync(this HttpClient httpClient, string? requestUri, bool removeComments)
    {
        var html = await httpClient.GetStringAsync(requestUri);
        return removeComments ? RemoveComments(html) : html;
    }

    public static async Task<string> GetStringAsync(this HttpClient httpClient, string? requestUri, CancellationToken cancellationToken, bool removeComments)
    {
        var html = await httpClient.GetStringAsync(requestUri, cancellationToken);
        return removeComments ? RemoveComments(html) : html;
    }

    public static async Task<string> GetStringAsync(this HttpClient httpClient, Uri? requestUri, CancellationToken cancellationToken, bool removeComments)
    {
        var html = await httpClient.GetStringAsync(requestUri, cancellationToken);
        return removeComments ? RemoveComments(html) : html;
    }
    private static string RemoveComments(string html)
    {
        while (Regex.Match(html, @"<!--(?!.*?<!--).*?-->").Success)
            html = Regex.Replace(html, @"<!--(?!.*?<!--).*?-->", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return html;
    }
}