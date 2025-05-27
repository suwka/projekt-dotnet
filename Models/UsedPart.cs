using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models
{
    public class UsedPart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PartId { get; set; }
        public Part Part { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Ilość musi być większa od zera.")]
        public int Quantity { get; set; }

        [Required]
        public int ServiceTaskId { get; set; }
        public ServiceTask ServiceTask { get; set; }
    }
}

