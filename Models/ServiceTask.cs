using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Column(TypeName = "decimal(18,2)")]
        public decimal LaborCost { get; set; }

        [Required]
        public int ServiceOrderId { get; set; }
        public ServiceOrder ServiceOrder { get; set; }

        public ICollection<UsedPart> UsedParts { get; set; }
    }
}

