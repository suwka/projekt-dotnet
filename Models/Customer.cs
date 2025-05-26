using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WorkshopManager.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        // Relacja 1:N z Vehicle
        public ICollection<Vehicle> Vehicles { get; set; }

        // Powiązanie z kontem użytkownika
        [Required]
        public string IdentityUserId { get; set; }
        public IdentityUser IdentityUser { get; set; }
    }
}

