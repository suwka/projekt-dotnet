using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WorkshopManager.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AuthorId { get; set; }
        public IdentityUser Author { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }
        
        public DateTime? ModifiedAt { get; set; }

        [Required]
        public int ServiceOrderId { get; set; }
        public ServiceOrder ServiceOrder { get; set; }
    }
}

