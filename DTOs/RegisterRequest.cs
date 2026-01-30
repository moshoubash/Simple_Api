using System.ComponentModel.DataAnnotations;

namespace ecommerce_back.DTOs
{
    public class RegisterRequest
    {
        [Required, StringLength(100)]
        public string name { get; set; } = "";

        [Required, EmailAddress]
        public string email { get; set; } = "";

        [Required, MinLength(8)]
        public string password { get; set; } = "";

        [Required]
        [Compare("password", ErrorMessage = "Passwords do not match")]
        public string password_confirmation { get; set; } = "";
    }
}