using GreenSyndic.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenSyndic.Api.Controllers
{
    /// <summary>
    /// OCR endpoints for document scanning.
    /// Supports Google Cloud Vision (primary) and Tesseract.js text parsing (fallback).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OcrController : ControllerBase
    {
        private readonly GoogleVisionService _visionService;
        private readonly ILogger<OcrController> _logger;

        public OcrController(GoogleVisionService visionService, ILogger<OcrController> logger)
        {
            _visionService = visionService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/ocr/document — Scans a document (image or PDF) and extracts raw text.
        /// Returns the OCR text for the frontend to display/use.
        /// </summary>
        [HttpPost("document")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> ScanDocument(IFormFile file)
        {
            _logger.LogInformation("[OCR] Scan document : {File} ({Size} Ko)", file?.FileName, file?.Length / 1024);

            if (file == null || file.Length == 0)
                return Ok(new DocumentOcrResult { Success = false, Error = "Fichier manquant ou vide" });

            var vision = await _visionService.ExtractTextAsync(file);
            if (!vision.Success)
                return Ok(new DocumentOcrResult { Success = false, Error = vision.Error });

            return Ok(new DocumentOcrResult
            {
                Success = true,
                RawText = vision.RawText,
                OcrEngine = vision.Engine,
                FileName = file.FileName,
                FileSize = file.Length,
                CharCount = vision.RawText.Length
            });
        }

        /// <summary>
        /// POST /api/ocr/parse/document — Parses raw text from Tesseract.js (fallback).
        /// Simply returns the text as-is (no structured parsing needed for general documents).
        /// </summary>
        [HttpPost("parse/document")]
        public IActionResult ParseDocument([FromBody] OcrParseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RawText))
                return Ok(new DocumentOcrResult { Success = false, Error = "Texte vide" });

            return Ok(new DocumentOcrResult
            {
                Success = true,
                RawText = request.RawText,
                OcrEngine = "Tesseract (local)",
                CharCount = request.RawText.Length
            });
        }

        /// <summary>
        /// GET /api/ocr/status — Checks if OCR is configured (Google Vision API key present).
        /// </summary>
        [HttpGet("status")]
        [AllowAnonymous]
        public IActionResult GetStatus()
        {
            var apiKey = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["GoogleCloudVision:ApiKey"];
            return Ok(new
            {
                GoogleVisionConfigured = !string.IsNullOrWhiteSpace(apiKey),
                TesseractAvailable = true // Always available (client-side)
            });
        }
    }

    public class OcrParseRequest
    {
        public string RawText { get; set; } = "";
    }

    public class DocumentOcrResult
    {
        public bool Success { get; set; }
        public string RawText { get; set; } = "";
        public string? Error { get; set; }
        public string OcrEngine { get; set; } = "";
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public int CharCount { get; set; }
    }
}
