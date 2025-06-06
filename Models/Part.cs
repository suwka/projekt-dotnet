using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models
{
    public class Part
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Cena jednostkowa musi być nieujemna.")]
        public decimal UnitPrice { get; set; }

        [StringLength(100)]
        public string Manufacturer { get; set; }

        [StringLength(50)]
        public string CatalogNumber { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Ilość musi być nieujemna.")]
        public int Quantity { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public ICollection<UsedPart> UsedParts { get; set; } = new List<UsedPart>();
    }
}

