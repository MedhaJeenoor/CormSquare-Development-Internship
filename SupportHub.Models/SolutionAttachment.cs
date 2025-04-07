using Newtonsoft.Json;

namespace SupportHub.Models
{
    public class SolutionAttachment : AuditableEntity
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Caption { get; set; }
        public bool IsInternal { get; set; }
        public int SolutionId { get; set; }
        public Solution Solution { get; set; }
        [JsonProperty("source")]
        public string? Source { get; set; }
        [JsonProperty("categoryAttachmentId")]
        public int? CategoryAttachmentId { get; set; }
    }
}