using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models
{
    public class ServiceTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Koszt robocizny musi być nieujemny.")]
        public decimal LaborCost { get; set; }

        [Required]
        public int ServiceOrderId { get; set; }
        public ServiceOrder ServiceOrder { get; set; }

        public ICollection<UsedPart> UsedParts { get; set; }
    }
}

