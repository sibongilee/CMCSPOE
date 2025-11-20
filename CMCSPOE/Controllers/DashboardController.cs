using System;
using System.Data.SqlClient;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace CMCSPOE.Controllers
{
    public class DashboardController : Controller
    {
        private readonly DatabaseConnection db = new DatabaseConnection();
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            User user = null;

            using (SqlConnection con = db.GetConnection())
            {
                con.Open();
                string query = "SELECT UserId, FullName, Email, Role FROM Users WHERE UserId = @UserId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId.Value);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                UserId = reader.GetInt32(0),
                                FullName = reader.GetString(1),
                                Email = reader.GetString(2),
                                Role = reader.GetString(3)
                            };
                        }
                    }
                }
            }

            // Provide values for view and layout - keep casing that layout expects
            if (user != null)
            {
                // keep in session for other pages if needed
                HttpContext.Session.SetString("UserName", user.FullName ?? "");
                HttpContext.Session.SetString("Role", user.Role ?? "");

                // Provide TempData keys used by your layout/view (match keys used in _Layout.cshtml)
                TempData["user_Name"] = user.FullName;
                TempData["user_Role"] = user.Role;

                // Also provide ViewData fallback
                ViewData["UserName"] = user.FullName;
                ViewData["UserRole"] = user.Role;
            }

            return View(user);
        }
    }
}