using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.WebServerDB
{
    [ModelMetadataType(typeof(UserMetadata))]
    public partial class User
    {
        [NotMapped] // 不要對應資料 (Table 無此欄位)
        [Display(Name = "ConfirmPassword")]
        [Required(ErrorMessage = "EnterPasswordAgainNotice")]
        [Compare("Password", ErrorMessage = "PasswordCompareNotice")]
        public string? ConfirmPassword { get; set; }
    }
    public partial class UserMetadata
    {
        [Display(Name = "ID")]
        public string? ID { get; set; }

        [Display(Name = "Account")]
        [Required(ErrorMessage = "AccountRequired")]
        //帳號字元限3~20碼，英文和數字(中間可包含一個【_】或【.】)。
        [RegularExpression(@"^(?=[^\._]+[\._]?[^\._]+$)[\w\.]{3,20}$", ErrorMessage = "AccountRule")]
        public string? Account { get; set; }
        [Display(Name = "Password")]
        [Required(ErrorMessage = "PasswordRequired")]
        //密碼限4~20個字
        [RegularExpression(@"^.{4,20}$", ErrorMessage = "PasswordRule")]
        public string? Password { get; set; }
        [Display(Name = "Email")]
        [Required(ErrorMessage = "EmailRequired")]
        [EmailAddress(ErrorMessage = "InvalidEmail")]
        [MaxLength(50)]
        public string? Email { get; set; }
        [Display(Name = "Name")]
        [Required(ErrorMessage = "NameRequired")]
        [MaxLength(20)]
        public string? Name { get; set; }
        [Display(Name = "Birthday")]
        [Required(ErrorMessage = "BirthdayRequired")]
        public string? Birthday { get; set; }
    }
}