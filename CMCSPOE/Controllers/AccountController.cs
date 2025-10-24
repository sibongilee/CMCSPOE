using Microsoft.AspNetCore.Mvc;

namespace CMCSPOE.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
