namespace CMCSPOE.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }
        public int LecturerId { get; set; }
        public string Month { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public string Notes { get; set; }
        public string FileName { get; set; }
        public string SupportingDocuments { get; set; }
        public string Status { get; set; } // Pending, Approved, Rejected
        public string LecturerName { get; set; } // Linked to the Lecturers table

        public int TotalAmount
        {
            get { return (int)(HoursWorked * HourlyRate); }
        }

    }
}
