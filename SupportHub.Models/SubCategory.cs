﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportHub.Models
{
    public class SubCategory : AuditableEntity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProductId { get; set; }  // Foreign Key
        public string Code { get; set; }
        public Product Product { get; set; }
    }
}
