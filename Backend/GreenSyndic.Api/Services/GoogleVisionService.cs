using System.Text;
using System.Text.Json;

namespace GreenSyndic.Api.Services
{
    /// <summary>
    /// Calls Google Cloud Vision API to extract text from an image or PDF.
    /// Accepts an IFormFile, handles both images AND PDFs natively.
    /// Ported from ArgusFlotte.
    /// </summary>
    public class GoogleVisionService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GoogleVisionService> _logger;

        public GoogleVisionService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<GoogleVisionService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Extracts text from a file (image or PDF) via Google Cloud Vision API.
        /// </summary>
        public async Task<VisionResult> ExtractTextAsync(IFormFile file, string language = "fra")
        {
            var apiKey = _config["GoogleCloudVision:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return VisionResult.Fail("Cle API Google Cloud Vision non configuree");

            if (file == null || file.Length == 0)
                return VisionResult.Fail("Fichier manquant ou vide");

            if (file.Length > 20_000_000) // 20 Mo max
                return VisionResult.Fail("Fichier trop volumineux (max 20 Mo)");

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());

                var isPdf = file.ContentType == "application/pdf"
                    || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

                var client = _httpClientFactory.CreateClient();
                string fullText;

                if (isPdf)
                {
                    fullText = await ExtractTextFromPdfAsync(client, apiKey, base64, language, file.FileName, file.Length);
                }
                else
                {
                    fullText = await ExtractTextFromImageAsync(client, apiKey, base64, language, file.FileName, file.Length);
                }

                _logger.LogInformation("[Vision] Texte detecte ({Len} chars) : {Preview}",
                    fullText.Length, fullText.Replace("\n", " ").Substring(0, Math.Min(200, fullText.Length)));

                return new VisionResult
                {
                    Success = true,
                    RawText = fullText,
                    Engine = "Google Vision"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Vision] Exception lors de l'appel API");
                return VisionResult.Fail(ex.Message);
            }
        }

        private async Task<string> ExtractTextFromImageAsync(HttpClient client, string apiKey, string base64, string language, string fileName, long fileSize)
        {
            var visionUrl = $"https://vision.googleapis.com/v1/images:annotate?key={apiKey}";

            var requestBody = new Dictionary<string, object>
            {
                ["requests"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["image"] = new Dictionary<string, string> { ["content"] = base64 },
                        ["features"] = new[] { new Dictionary<string, object> { ["type"] = "TEXT_DETECTION", ["maxResults"] = 10 } },
                        ["imageContext"] = new Dictionary<string, object> { ["languageHints"] = new[] { language } }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("[Vision] Appel images:annotate pour {File} ({Size} Ko)...", fileName, fileSize / 1024);

            var response = await client.PostAsync(visionUrl, httpContent);
            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            var responseBody = Encoding.UTF8.GetString(responseBytes);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Vision] Erreur images:annotate: {Status} - {Body}", response.StatusCode, responseBody);
                throw new Exception($"Erreur Google Vision API: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var responses = doc.RootElement.GetProperty("responses");
            var firstResponse = responses[0];

            if (firstResponse.TryGetProperty("fullTextAnnotation", out var fullTextAnnotation))
                return fullTextAnnotation.GetProperty("text").GetString() ?? "";
            if (firstResponse.TryGetProperty("textAnnotations", out var textAnnotations) && textAnnotations.GetArrayLength() > 0)
                return textAnnotations[0].GetProperty("description").GetString() ?? "";

            return "";
        }

        private async Task<string> ExtractTextFromPdfAsync(HttpClient client, string apiKey, string base64, string language, string fileName, long fileSize)
        {
            var visionUrl = $"https://vision.googleapis.com/v1/files:annotate?key={apiKey}";

            var requestBody = new Dictionary<string, object>
            {
                ["requests"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["inputConfig"] = new Dictionary<string, object>
                        {
                            ["content"] = base64,
                            ["mimeType"] = "application/pdf"
                        },
                        ["features"] = new[] { new Dictionary<string, object> { ["type"] = "DOCUMENT_TEXT_DETECTION", ["maxResults"] = 10 } },
                        ["imageContext"] = new Dictionary<string, object> { ["languageHints"] = new[] { language } },
                        ["pages"] = new[] { 1, 2, 3, 4, 5 }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("[Vision] Appel files:annotate (PDF) pour {File} ({Size} Ko)...", fileName, fileSize / 1024);

            var response = await client.PostAsync(visionUrl, httpContent);
            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            var responseBody = Encoding.UTF8.GetString(responseBytes);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Vision] Erreur files:annotate: {Status} - {Body}", response.StatusCode, responseBody);
                throw new Exception($"Erreur Google Vision API (PDF): {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var outerResponses = doc.RootElement.GetProperty("responses");
            var firstOuter = outerResponses[0];

            var sb = new StringBuilder();

            if (firstOuter.TryGetProperty("responses", out var pageResponses))
            {
                for (int i = 0; i < pageResponses.GetArrayLength(); i++)
                {
                    var pageResp = pageResponses[i];
                    if (pageResp.TryGetProperty("fullTextAnnotation", out var fta))
                    {
                        var pageText = fta.GetProperty("text").GetString() ?? "";
                        if (sb.Length > 0 && pageText.Length > 0) sb.Append('\n');
                        sb.Append(pageText);
                    }
                }
            }

            return sb.ToString();
        }
    }

    public class VisionResult
    {
        public bool Success { get; set; }
        public string RawText { get; set; } = "";
        public string? Error { get; set; }
        public string Engine { get; set; } = "";

        public static VisionResult Fail(string error) => new() { Success = false, Error = error };
    }
}
