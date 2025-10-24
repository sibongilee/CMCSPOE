using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;


namespace CMCSPOE.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            string role = HttpContext.Session.GetString("Role");
            string fullName = HttpContext.Session.GetString("FullName");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            ViewBag.FullName = fullName;
            ViewBag.Role = role;

            // Role-based dashboard logic
            if (role == "Lecturer")
            {
                ViewBag.Message = "Welcome Lecturer! You can submit and view your claims.";
            }
            else if (role == "Programme Coordinator")
            {
                ViewBag.Message = "Welcome PC! You can verify and approve lecturer claims.";
            }
            else if (role == "Academic Manager")
            {
                ViewBag.Message = "Welcome AM! You can review and finalize all claims.";
            }

            return View();

        }
    }
}

