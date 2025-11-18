namespace CMCSPOE.Models
{
    public class Approval
    {
        public int ApprovalId { get; set; }
        public int ClaimId { get; set; }
        public int ApprovedBy { get; set; } // UserId of PC/AM
        public string Status { get; set; } // Approved, Rejected
        public string DateApproved { get; set; }

    }
}
