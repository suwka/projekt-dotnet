using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models
{
    public class PartOrder
    {
        public int Id { get; set; }

        [Required]
        public string Brand { get; set; }

        [Required]
        public string Model { get; set; }

        public string? PartNumber { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Ilość musi być większa od zera.")]
        public int Quantity { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

