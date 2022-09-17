namespace WebServer.Models
{
    // 放在Session的記錄使用者資訊欄位
    public class UserProfileModel
    {
        public string? ID { get; set; }
        public string? Account { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? Avatar { get; set; } = @"/images/avatar.png";
    }
}
