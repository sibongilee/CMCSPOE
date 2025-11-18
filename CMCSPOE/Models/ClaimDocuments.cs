using System;

namespace CMCSPOE.Models
{
    public class ClaimDocuments
    {
        public int DocumentID { get; set; }
        public int ClaimID { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }
}
