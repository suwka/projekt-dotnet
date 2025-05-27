using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopManager.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Brand { get; set; }

        [Required]
        [StringLength(50)]
        public string Model { get; set; }

        [Required]
        [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN musi mieć 17 znaków.")]
        public string Vin { get; set; }

        [Required]
        [StringLength(15)]
        public string RegistrationNumber { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [StringLength(255)]
        public string ImageUrl { get; set; } = "https://via.placeholder.com/150"; // domyślny obrazek

        // Relacja N:1 z Customer
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Relacja 1:N z ServiceOrder
        public ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
    }
}

