using System.Data.SqlClient;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult Login(User model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Please fill all fields.";
                return View(model);
            }

            try
            {
                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();

                    SqlCommand cmd = new SqlCommand("sp_LoginUser", con);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", model.Password); // plain password now

                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        TempData["User_Id"] = dr["UserId"].ToString();
                        TempData["User_Name"] = dr["FullName"].ToString();
                        TempData["User_Role"] = dr["Role"].ToString();

                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        ViewBag.Error = "Invalid email or password.";
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Login failed: " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registeration(User model)
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
                    cmd.Parameters.AddWithValue("@PasswordHash", model.Password);
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

