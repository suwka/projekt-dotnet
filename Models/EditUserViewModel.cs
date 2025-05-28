using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models
{
    public class EditUserViewModel
    {
        [Required(ErrorMessage = "Adres email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Rola jest wymagana.")]
        public string Role { get; set; }
    }
}

