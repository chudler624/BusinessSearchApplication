using Microsoft.AspNetCore.Mvc;

namespace BusinessSearch.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}