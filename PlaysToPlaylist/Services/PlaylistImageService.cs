namespace PlaystoPlaylist.Services;

/// <summary>
/// 登録済みユーザーのアバターを、プレイリストに埋め込むData URIに変換します。
/// </summary>
public sealed class PlaylistImageService(HttpClient httpClient)
{
    public const int MaxImageBytes = 5 * 1024 * 1024;

    public async Task<string?> GetImageAsync(string? avatarUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("https" or "http"))
            return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxImageBytes)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var image = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer, timeout.Token)) > 0)
            {
                if (image.Length + read > MaxImageBytes) return null;
                image.Write(buffer, 0, read);
            }

            var bytes = image.ToArray();
            // Detect the actual format; CDN URLs and Content-Type may not contain it.
            var mediaType = GetMediaType(bytes);
            return mediaType is null ? null : $"data:{mediaType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
    }

    private static string? GetMediaType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (bytes.StartsWith(new byte[] { 255, 216, 255 })) return "image/jpeg";
        return null;
    }
}
