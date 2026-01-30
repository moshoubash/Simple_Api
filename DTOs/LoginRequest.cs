using System.ComponentModel.DataAnnotations;

namespace ecommerce_back.DTOs
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string email { get; set; } = "";

        [Required]
        public string password { get; set; } = "";
    }
}