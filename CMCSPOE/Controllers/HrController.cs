using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMCSPOE.Controllers
{
    public class HrController : Controller
    {
        public IActionResult Index()
        {
            var reports = new List<HrReport>();
            var db = new DatabaseConnection();

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT 
                        c.ClaimId,
                        l.FullName,
                        c.HoursWorked,
                        COALESCE(c.HourlyRate, c.RatePerHour) AS HourlyRate,
                        COALESCE(c.TotalAmount, (c.HoursWorked * COALESCE(c.HourlyRate, c.RatePerHour))) AS TotalAmount,
                        c.ApprovedDate,
                        COALESCE(c.ApprovedBy, '') AS ApprovedBy,
                        COALESCE(c.Comments, c.ViolationReasons, '') AS Comments
                    FROM Claims c
                    INNER JOIN Lecturers l ON c.LecturerId = l.LecturerId
                    WHERE COALESCE(c.Status, c.ClaimStatus) = 'Approved'
                    ORDER BY c.ApprovedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reports.Add(new HrReport
                        {
                            ClaimId = reader["ClaimId"] != DBNull.Value ? Convert.ToInt32(reader["ClaimId"]) : 0,
                            LecturerName = reader["FullName"] != DBNull.Value ? reader["FullName"].ToString() : null,
                            HoursWorked = reader["HoursWorked"] != DBNull.Value ? Convert.ToDecimal(reader["HoursWorked"]) : 0m,
                            HourlyRate = reader["HourlyRate"] != DBNull.Value ? Convert.ToDecimal(reader["HourlyRate"]) : 0m,
                            TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0m,
                            ApprovedDate = reader["ApprovedDate"] != DBNull.Value ? Convert.ToDateTime(reader["ApprovedDate"]) : (DateTime?)null,
                            ApprovedBy = reader["ApprovedBy"] != DBNull.Value ? reader["ApprovedBy"].ToString() : null,
                            Comments = reader["Comments"] != DBNull.Value ? reader["Comments"].ToString() : null
                        });
                    }
                }
            }

            return View(reports);
        }

        // Download CSV Report
        public IActionResult DownloadReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ClaimId,Lecturer,HoursWorked,HourlyRate,TotalAmount,ApprovedDate,ApprovedBy,Comments");

            var db = new DatabaseConnection();

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT 
                        c.ClaimId,
                        l.FullName,
                        c.HoursWorked,
                        COALESCE(c.HourlyRate, c.RatePerHour) AS HourlyRate,
                        COALESCE(c.TotalAmount, (c.HoursWorked * COALESCE(c.HourlyRate, c.RatePerHour))) AS TotalAmount,
                        c.ApprovedDate,
                        COALESCE(c.ApprovedBy, '') AS ApprovedBy,
                        COALESCE(c.Comments, c.ViolationReasons, '') AS Comments
                    FROM Claims c
                    INNER JOIN Lecturers l ON c.LecturerId = l.LecturerId
                    WHERE COALESCE(c.Status, c.ClaimStatus) = 'Approved'
                    ORDER BY c.ApprovedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string Escape(object? value)
                        {
                            if (value == null || value == DBNull.Value) return "";
                            var s = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
                            if (s.Contains('"')) s = s.Replace("\"", "\"\"");
                            if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                                return $"\"{s}\"";
                            return s;
                        }

                        string approvedDate = reader["ApprovedDate"] != DBNull.Value
                            ? ((DateTime)reader["ApprovedDate"]).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                            : "";

                        sb.AppendLine(
                            $"{Escape(reader["ClaimId"])},{Escape(reader["FullName"])},{Escape(reader["HoursWorked"])},{Escape(reader["HourlyRate"])},{Escape(reader["TotalAmount"])},{Escape(approvedDate)},{Escape(reader["ApprovedBy"])},{Escape(reader["Comments"])}"
                        );
                    }
                }
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv; charset=utf-8", "HR_ApprovedClaims_Report.csv");
        }
    }
}
