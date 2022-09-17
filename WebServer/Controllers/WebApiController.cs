using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.WebServerDB;

namespace WebServer.Controllers
{
    [Route("api")]
    //使用JwtBearer
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class WebApiController : Controller
    {
        private readonly WebServerDBContext _WebServerDBContext;
        private readonly IHttpContextAccessor _context;
        public WebApiController(WebServerDBContext WebServerDBContext,
            IHttpContextAccessor context)
        {
            _WebServerDBContext = WebServerDBContext;
            _context = context;
        }

        // api/Test
        [HttpPost("Test")]
        public async Task<IActionResult> Test()
        {
            var userID = User?.Identity?.Name;
            var user = await _WebServerDBContext.User.FindAsync(userID);
            return Json(new
            {
                email = user?.Email,
            });
        }

        // api/PunchIn/{cardNo}
        [HttpPost("PunchIn/{cardNo}")]
        public async Task<IActionResult> PunchIn(string cardNo)
        {
            //先檢查卡號是否存在
            var card = await _WebServerDBContext.Card.Where(s => s.CardNo == cardNo).FirstOrDefaultAsync();
            if (card == null) {
                //卡片不存在, 便新增卡片
                card = new Card
                {
                    ID = Guid.NewGuid().ToString(),
                    CardNo = cardNo,
                };
                await _WebServerDBContext.Card.AddAsync(card);
            }
            //新增打卡記錄
            var cardHistory = new CardHistory
            {
                ID = Guid.NewGuid().ToString(),
                CardID = card.ID,
                PunchInDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            };
            await _WebServerDBContext.CardHistory.AddAsync(cardHistory);
            //儲存變更
            await _WebServerDBContext.SaveChangesAsync();
            return Json(new
            {
                PunchInDateTime = cardHistory.PunchInDateTime,
            });
        }


    }
}
