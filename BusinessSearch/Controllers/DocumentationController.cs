using Microsoft.AspNetCore.Mvc;

namespace BusinessSearch.Controllers
{
    public class DocumentationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
