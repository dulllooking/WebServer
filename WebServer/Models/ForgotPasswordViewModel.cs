using System.ComponentModel.DataAnnotations;

namespace WebServer.Models
{
    public class ForgotPasswordViewModel
    {
        [Display(Name = "Email")]
        [Required(ErrorMessage = "EmailRequired")]
        [EmailAddress(ErrorMessage = "InvalidEmail")]
        public string? Email { get; set; }
        public string? ErrorMessage { get; set; }
    }
}