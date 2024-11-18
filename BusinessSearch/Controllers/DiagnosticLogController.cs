using Microsoft.AspNetCore.Mvc;

namespace BusinessSearch.Controllers
{
    public class DiagnosticLogController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<DiagnosticLogController> _logger;

        public DiagnosticLogController(IWebHostEnvironment hostingEnvironment, ILogger<DiagnosticLogController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        public IActionResult DownloadLatestLog()
        {
            try
            {
                var logsDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Logs");
                if (!Directory.Exists(logsDirectory))
                {
                    return Content("No logs directory found.");
                }

                var logFile = Directory.GetFiles(logsDirectory)
                    .OrderByDescending(f => f)
                    .FirstOrDefault();

                if (logFile == null)
                {
                    return Content("No log files found.");
                }

                var fileBytes = System.IO.File.ReadAllBytes(logFile);
                var fileName = Path.GetFileName(logFile);

                return File(fileBytes, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading log: {ex.Message}");
                return Content($"Error: {ex.Message}");
            }
        }
    }
}
