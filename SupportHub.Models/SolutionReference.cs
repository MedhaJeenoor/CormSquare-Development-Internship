namespace SupportHub.Models
{
    public class SolutionReference : AuditableEntity
    {
        public string Url { get; set; }
        public string Description { get; set; }
        public bool IsInternal { get; set; }
        public string OpenOption { get; set; } // "_blank" or "_self"
        public int SolutionId { get; set; }
        public Solution Solution { get; set; }
    }
}