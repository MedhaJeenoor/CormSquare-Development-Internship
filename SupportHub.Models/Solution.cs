﻿using System.Collections.Generic;

namespace SupportHub.Models
{
    public class Solution : AuditableEntity
    {
        public string Title { get; set; }
        public string? DocId { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int? SubCategoryId { get; set; }
        public SubCategory SubCategory { get; set; }
        public string AuthorId { get; set; }
        public ExternalUser Author { get; set; }
        public string? Contributors { get; set; }
        public string HtmlContent { get; set; }
        public string PlainTextContent { get; set; }
        public string? IssueDescription { get; set; }
        public string Status { get; set; } = "Draft"; // "Draft", "Submitted", "Under Review", "Approved and Published", "Rejected", "Needs Revision"
        public string? Feedback { get; set; }
        public List<SolutionAttachment> Attachments { get; set; } = new List<SolutionAttachment>();
        public List<SolutionReference> References { get; set; } = new List<SolutionReference>();
    }
}