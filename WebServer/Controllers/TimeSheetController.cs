using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Extensions;
using WebServer.Models;
using WebServer.Models.WebServerDB;
using WebServer.Services;

namespace WebServer.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(WebServer.Filters.AuthorizeFilter))]
    public class TimeSheetController : Controller
    {
        private readonly ILogger<TimeSheetController> _logger;
        private readonly WebServerDBContext _WebServerDBContext;
        private readonly SiteService _SiteService;

        public TimeSheetController(ILogger<TimeSheetController> logger,
            WebServerDBContext WebServerDBContext,
            SiteService SiteService)
        {
            _logger = logger;
            _WebServerDBContext = WebServerDBContext;
            _SiteService = SiteService;
        }

        #region Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await Task.Yield();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetColumns()
        {
            try {
                var _columnList = new List<string>
                {
                    nameof(TimeSheetIndexViewModel.CardNo),
                    nameof(TimeSheetIndexViewModel.UserName),
                    nameof(TimeSheetIndexViewModel.PunchInDateTime),
                };
                var columns = await _SiteService.GetDatatableColumns<TimeSheetIndexViewModel>(_columnList);

                return new SystemTextJsonResult(new
                {
                    status = "success",
                    data = columns,
                });
            }
            catch (Exception e) {
                return new SystemTextJsonResult(new
                {
                    status = "fail",
                    message = e.Message,
                });
            }
        }

        /// <summary>
        /// For Data Table
        /// </summary>
        /// <param name="draw">DataTable用,不用管他</param>
        /// <param name="start">起始筆數</param>
        /// <param name="length">顯示筆數</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetData(int draw, int start, int length)
        {
            await Task.Yield();
            try {
                //總筆數
                int nTotalCount = await _WebServerDBContext.CardHistory.CountAsync();

                var info = from n1 in _WebServerDBContext.CardHistory
                           join n2 in _WebServerDBContext.Card on n1.CardID equals n2.ID
                           join n3 in _WebServerDBContext.User on n2.UserID equals n3.ID into tempN3
                           from n3 in tempN3.DefaultIfEmpty()
                           select new TimeSheetIndexViewModel
                           {
                               ID = n1.ID,
                               CardNo = n2.CardNo,
                               UserName = n3 == null ? string.Empty : n3.Name,
                               PunchInDateTime = n1.PunchInDateTime,
                           };

                #region 關鍵字搜尋
                if (!string.IsNullOrEmpty((string)Request.Form["search[value]"])) {
                    string sQuery = Request.Form["search[value]"].ToString().ToUpper();
                    bool IsNumber = decimal.TryParse(sQuery, out decimal nQuery);
                    info = info.Where(t =>
                                 (!string.IsNullOrEmpty(t.CardNo) && t.CardNo.ToUpper().Contains(sQuery))
                                || (!string.IsNullOrEmpty(t.UserName) && t.UserName.ToUpper().Contains(sQuery))
                    );
                }
                #endregion 關鍵字搜尋

                #region 排序
                int sortColumnIndex = (string)Request.Form["order[0][column]"] == null ? -1 : int.Parse(Request.Form["order[0][column]"]);
                string sortDirection = (string)Request.Form["order[0][dir]"] == null ? "" : Request.Form["order[0][dir]"].ToString().ToUpper();
                string sortColumn = Request.Form["columns[" + sortColumnIndex + "][data]"].ToString() ?? "";

                bool bDescending = sortDirection.Equals("DESC");
                switch (sortColumn) {
                    case nameof(TimeSheetIndexViewModel.CardNo):
                        info = bDescending ? info.OrderByDescending(o => o.CardNo) : info.OrderBy(o => o.CardNo);
                        break;

                    case nameof(TimeSheetIndexViewModel.UserName):
                        info = bDescending ? info.OrderByDescending(o => o.UserName) : info.OrderBy(o => o.UserName);
                        break;
                    case nameof(TimeSheetIndexViewModel.PunchInDateTime):
                        info = bDescending ? info.OrderByDescending(o => o.PunchInDateTime) : info.OrderBy(o => o.PunchInDateTime);
                        break;

                    default:
                        info = info.OrderBy(o => o.CardNo);
                        break;
                }

                #endregion 排序

                //結果
                var list = nTotalCount == 0 ? new List<TimeSheetIndexViewModel>() : info.OrderByDescending(x => x.PunchInDateTime).Skip(start).Take(Math.Min(length, nTotalCount - start)).ToList();

                return new SystemTextJsonResult(new DataTableData
                {
                    Draw = draw,
                    Data = list,
                    RecordsTotal = nTotalCount,
                    RecordsFiltered = info.Count()
                });
            }
            catch (Exception e) {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}
