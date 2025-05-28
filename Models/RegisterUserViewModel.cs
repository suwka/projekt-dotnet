using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models
{
    public class RegisterUserViewModel
    {
        [Required]
        [Display(Name = "Nazwa użytkownika")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Rola")]
        public string Role { get; set; } = string.Empty;
    }
}

