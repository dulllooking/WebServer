using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using WebServer.Services;

namespace WebServer.Filters
{
    public class AuthorizeFilter : IActionFilter
    {
        private readonly SiteService _siteService;
        public AuthorizeFilter(SiteService siteService)
        {
            _siteService = siteService;
        }

        // Action 執行後
        void IActionFilter.OnActionExecuted(ActionExecutedContext context)
        {
            return;
        }

        // Action 執行前 (可用於過濾使用者用了什麼功能，ex: 從哪裡來的、有沒有登入...)
        void IActionFilter.OnActionExecuting(ActionExecutingContext context)
        {
            // 可以從 Request 取得想知道的資訊來使用過濾
            string returnUrl = context.HttpContext.Request.Path.ToString();
            string fromSring = !string.IsNullOrEmpty(context.HttpContext.Request.Query["From"].ToString()) ? "?From=" + context.HttpContext.Request.Query["From"].ToString() : "";
            returnUrl += fromSring;

            var userInfo = _siteService.GetUserProfile().Result;
            if (userInfo == null) {
                //導頁至登入頁
                context.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        action = "Signin",
                        controller = "Account",
                        area = "",
                        returnUrl = returnUrl
                    }));
            }
        }
    }
}
