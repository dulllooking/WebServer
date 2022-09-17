using Microsoft.AspNetCore.Mvc;
using WebServer.Services;

namespace WebServer.Components
{
    // 元件名稱Razor叫用會變小寫及第二個大寫後前面皆會補上"-"，且Shared裡對應的資料夾要同名 // <vc:set-language>
    [ViewComponent(Name = "SetLanguage")]
    public class LanguageComponent : ViewComponent
    {
        private readonly SiteService _siteService;

        public LanguageComponent(SiteService siteService)
        {
            _siteService = siteService;
        }

        // Component 預設執行 Invoke
        public async Task<IViewComponentResult> InvokeAsync()
        {
            await Task.Yield();
            return View("Default", _siteService.GetCurrentCulture());
        }
    }
}