using Microsoft.AspNetCore.Mvc;
using WebServer.Services;

namespace WebServer.Components
{
    [ViewComponent(Name = "UserProfile")]
    public class UserProfileComponent : ViewComponent
    {
        private readonly SiteService _siteService;

        public UserProfileComponent(SiteService siteService)
        {
            _siteService = siteService;
        }

        // Async 結尾 core 可忽略
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 建立預設元件，Default 是找不到時 core 會自動找的名稱
            return View("Default", await _siteService.GetUserProfile());
        }
    }
}
