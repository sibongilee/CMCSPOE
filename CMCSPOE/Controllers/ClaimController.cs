using Microsoft.AspNetCore.Mvc;

namespace CMCSPOE.Controllers
{
    public class ClaimController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
