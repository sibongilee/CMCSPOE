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
        public IActionResult Login(string email, string password)
        {
            using (SqlConnection con = db.GetConnection())
            {
                con.Open();
                string query = "SELECT * FROM Users WHERE Email=@Email AND Password=@Password";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    HttpContext.Session.SetInt32("UserId", (int)reader["UserId"]);
                    HttpContext.Session.SetString("FullName", reader["FullName"].ToString());
                    HttpContext.Session.SetString("Role", reader["Role"].ToString());

                    // Save LecturerId only for lecturer accounts
                    if (reader["Role"].ToString() == "Lecturer" && reader["LecturerId"] != DBNull.Value)
                    {
                        HttpContext.Session.SetInt32("LecturerId", (int)reader["LecturerId"]);
                    }

                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    ViewBag.Message = "Invalid email or password.";
                    return View();
                }
            }
        }
        // GET: Register
        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        public IActionResult Registration(User user)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();
                    string query = "INSERT INTO Users (FullName, Email, Password, Role) VALUES (@FullName, @Email, @Password, @Role)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@FullName", user.FullName);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", user.Password);
                    cmd.Parameters.AddWithValue("@Role", user.Role);
                    cmd.ExecuteNonQuery();
                }
                return RedirectToAction("Login");
            }
            return View(user);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

