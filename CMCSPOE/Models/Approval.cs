namespace CMCSPOE.Models
{
    public class Approval
    {
        public int ApprovalId { get; set; }
        public int ClaimId { get; set; }
        public int ApprovedBy { get; set; } // UserId of PC/AM
        public string Decision { get; set; } // Approved or Rejected
        public string Comments { get; set; }
        public DateTime DecisionDate { get; set; }

    }
}
