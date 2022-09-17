using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace WebServer.Models
{
    public class SigninViewModel
    {
        [Display(Name = "Account")]
        [Required(ErrorMessage = "AccountRequired")]
        public string? Account { get; set; }
        [Display(Name = "Password")]
        [Required(ErrorMessage = "PasswordRequired")]
        public string? Password { get; set; }
        public string? ReturnUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
