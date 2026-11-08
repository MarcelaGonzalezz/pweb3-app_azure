using Microsoft.AspNetCore.Mvc;

namespace FileShareApp.Controllers {
    public class HomeController : Controller {
        public IActionResult Index() {
            return View();
        }
    }
}
