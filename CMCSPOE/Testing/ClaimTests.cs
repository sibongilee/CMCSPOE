using System;
using CMCSPOE.Models;

namespace CMSCPOE.Tests 
{
    public class ClaimTests
    { 
        public void TestCalution()
        {
             Claim claim = new Claim { HoursWorked = 10, HourlyRate = 200 };
            decimal result = claim.CalculateAmount();
            Console.WriteLine(result == 2000 ? "PASS: Calculation correct" : "FAIL: Incorrect calculation");
        }

        public void TestStatusUpdate()
        {
            Claim claim = new Claim { Status = "Pending" };
            claim.Status = "Approved";
            Console.WriteLine(claim.Status == "Approved" ? "PASS: Status updated" : "FAIL: Status error");
        }

        }
    }

