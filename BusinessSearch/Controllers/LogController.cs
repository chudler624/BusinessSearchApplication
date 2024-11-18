using Microsoft.AspNetCore.Mvc;
using System.IO;

public class LogController : Controller
{
    private readonly IWebHostEnvironment _hostingEnvironment;

    public LogController(IWebHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment;
    }

    public IActionResult Index()
    {
        var logsDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Logs");
        if (!Directory.Exists(logsDirectory))
        {
            return View(new List<string>());
        }

        var logFiles = Directory.GetFiles(logsDirectory)
            .OrderByDescending(f => f)
            .Take(10)
            .Select(f => new FileInfo(f))
            .Select(f => new
            {
                Name = f.Name,
                Content = System.IO.File.ReadLines(f.FullName).TakeLast(1000).ToList()
            })
            .ToList();

        return View(logFiles);
    }
}
