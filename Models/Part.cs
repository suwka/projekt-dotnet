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

        public ICollection<UsedPart> UsedParts { get; set; }
    }
}

