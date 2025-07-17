using Microsoft.AspNetCore.Mvc;

namespace SesliDilDeneme.API.Controllers
{
    public class SesliDil : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
