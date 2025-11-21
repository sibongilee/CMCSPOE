using System;

namespace CMCSPOE.Models
{
    public class HrReport
    {
        public int ClaimId { get; set; }
        public string? LecturerName { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? ApprovedBy { get; set; }
        public string? Comments { get; set; }
    }
}
