using Microsoft.AspNetCore.Mvc;

namespace CMCSPOE.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
