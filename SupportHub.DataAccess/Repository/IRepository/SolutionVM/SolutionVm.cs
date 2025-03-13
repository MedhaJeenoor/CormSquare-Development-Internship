using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SupportHub.Models.ViewModels
{
    public class SolutionVM
    {
        public Solution Solution { get; set; } = new Solution();
        public List<Category> Categories { get; set; } = new List<Category>();
    }
}