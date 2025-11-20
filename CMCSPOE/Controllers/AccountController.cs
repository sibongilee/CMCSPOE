using System.Data.SqlClient;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace CMCSPOE.Controllers
{
    public class AccountController : Controller
    {
        private readonly DatabaseConnection db = new DatabaseConnection();

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string PasswordHash)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Please fill all fields.";
                return View();
            }

            try
            {
                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();
                    string query = "SELECT UserId, FullName, Role FROM Users WHERE Email = @Email AND PasswordHash = @PasswordHash";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@PasswordHash", PasswordHash);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : 0;
                                string fullName = reader["FullName"]?.ToString() ?? "";
                                string role = reader["Role"]?.ToString() ?? "";

                                // Persist into session so DashboardController can read them
                                HttpContext.Session.SetInt32("UserId", userId);
                                HttpContext.Session.SetString("UserName", fullName);
                                HttpContext.Session.SetString("Role", role);

                                // Optionally keep TempData for one-time messages
                                TempData["UserName"] = fullName;
                                TempData["UserRole"] = role;

                                return RedirectToAction("Index", "Dashboard");
                            }
                            else
                            {
                                ViewBag.Error = "Invalid email or password.";
                                return View();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Login failed: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registration(User model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Please complete all fields.";
                return View(model);
            }

            try
            {
                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();

                    SqlCommand cmd = new SqlCommand("sp_RegisterUser", con);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", model.PasswordHash);
                    cmd.Parameters.AddWithValue("@Role", model.Role);

                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Registration successful. Please log in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Registration failed: " + ex.Message;
                return View(model);
            }
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

