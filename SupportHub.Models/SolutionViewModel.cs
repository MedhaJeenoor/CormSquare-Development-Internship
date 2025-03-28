using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportHub.Models
{
    public class SolutionViewModel
    {
        public string Title { get; set; }
        public int CategoryId { get; set; }
        public int ProductId { get; set; }
        public int? SubCategoryId { get; set; }
        public string Contributors { get; set; }
        public string HtmlContent { get; set; }
        public string IssueDescription { get; set; }
        public List<Category> Categories { get; set; }
        public List<Product> Products { get; set; }
        public List<SubCategory> SubCategories { get; set; }
        public List<SolutionAttachment> Attachments { get; set; }
        public List<SolutionReference> References { get; set; }
    }
}
