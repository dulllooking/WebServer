using System.ComponentModel.DataAnnotations;

namespace WebServer.Models
{
    // 定義Index頁欄位屬性
    public class UserIndexViewModel
    {
        public string? ID { get; set; }
        [Display(Name = "Account")]
        [SortingType(SortingTypeEnum.Disabled)] // 帳號欄位不允許排序，隱藏排序鈕
        public string? Account { get; set; }
        [Display(Name = "Name")]
        public string? Name { get; set; }
        [Display(Name = "Birthday")]
        public string? Birthday { get; set; }
        [Display(Name = "Email")]
        public string? Email { get; set; }
    }
}
