using System;
using System.Data.SqlClient;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace CMCSPOE.Controllers
{
    [AllowAnonymous]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            if (TempData["User_Id"] == null)
                return RedirectToAction("Login", "Account");
            return View();
        }
    }
}