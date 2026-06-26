using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QRGenerator;

public sealed class OneDriveUploader : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(120) };
    private const string GraphBase = "https://graph.microsoft.com/v1.0";

    public async Task<string> AcquireTokenAsync(string tenantId, string clientId, string clientSecret)
    {
        var url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        using var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id",     clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("scope",         "https://graph.microsoft.com/.default"),
            new KeyValuePair<string, string>("grant_type",    "client_credentials"),
        });

        var resp = await _http.PostAsync(url, body);
        var raw  = await resp.Content.ReadAsStringAsync();
        using var doc  = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        if (!resp.IsSuccessStatusCode || !root.TryGetProperty("access_token", out var tok))
        {
            var desc = root.TryGetProperty("error_description", out var d) ? d.GetString() : raw;
            throw new InvalidOperationException($"Token acquisition failed: {desc}");
        }

        return tok.GetString()!;
    }

    // Uploads pdfBytes to OneDrive and returns the Graph item ID of the uploaded file.
    // Suitable for PDFs up to ~4 MB (Graph API single-request limit).
    // Set forceOverwrite=true on re-uploads: adds If-Match: * to bypass eTag checks that
    // arise when the file's metadata was modified (e.g. by createLink) between uploads.
    public async Task<string> UploadPdfAsync(string token, string userEmail, string targetFolder,
        string fileName, byte[] pdfBytes, bool forceOverwrite = false)
    {
        var folder = targetFolder.Trim('/');
        var path   = string.IsNullOrWhiteSpace(folder) ? fileName : $"{folder}/{fileName}";
        var url    = $"{GraphBase}/users/{userEmail}/drive/root:/{path}:/content";

        using var req = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new ByteArrayContent(pdfBytes)
        };
        req.Headers.Authorization       = new AuthenticationHeaderValue("Bearer", token);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        if (forceOverwrite)
            req.Headers.Add("If-Match", "*");

        var resp = await _http.SendAsync(req);
        var raw  = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Upload failed ({(int)resp.StatusCode} {resp.StatusCode}): {raw}");

        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    // Creates an anonymous view-only shareable link for the given OneDrive item.
    // Returns the public webUrl that can be embedded in the QR code.
    public async Task<string> CreateShareLinkAsync(string token, string userEmail, string itemId)
    {
        var url  = $"{GraphBase}/users/{userEmail}/drive/items/{itemId}/createLink";
        var json = """{"type":"view","scope":"anonymous"}""";

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _http.SendAsync(req);
        var raw  = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Share link creation failed ({(int)resp.StatusCode} {resp.StatusCode}): {raw}");

        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement
                   .GetProperty("link")
                   .GetProperty("webUrl")
                   .GetString()
               ?? throw new InvalidOperationException("Share link URL was empty in the response.");
    }

    public void Dispose() => _http.Dispose();
}
