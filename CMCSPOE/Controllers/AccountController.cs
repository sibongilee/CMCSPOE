using System.Data.SqlClient;
using CMCSPOE.Data;
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

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

