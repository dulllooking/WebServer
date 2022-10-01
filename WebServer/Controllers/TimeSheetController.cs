using CsvHelper;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Globalization;
using System.Text;
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
        #endregion

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
                        //info = info.OrderBy(o => o.CardNo);
                        info = info.OrderByDescending(o => o.PunchInDateTime);
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

        #region 匯出CSV
        //TimeSheet/ExportCSV/{month}
        [Route("[controller]/[action]/{month}")]
        [HttpGet()]
        public async Task<IActionResult> ExportCSV(string month)
        {
            await Task.Yield();
            try {
                var tmpDate = DateTime.Parse(month + "-01");
                //列出要顯示的日期
                var dateList = Enumerable.Range(1, DateTime.DaysInMonth(tmpDate.Year, tmpDate.Month))
                        .Select(day => (new DateTime(tmpDate.Year, tmpDate.Month, day))
                        .ToString("yyyy-MM-dd"))
                        .ToList();
                //展開資料
                var records = (from n1 in dateList
                               from n2 in _WebServerDBContext.Card
                               join n3 in _WebServerDBContext.User on n2.UserID equals n3.ID
                               //上班打卡(當天第一次)
                               from punchIn in _WebServerDBContext.CardHistory.Where(s => s.CardID == n2.ID && s.PunchInDateTime.Substring(0, 10) == n1).OrderBy(s => s.PunchInDateTime).Take(1).DefaultIfEmpty()
                                   //下班打卡(當天最後一次)
                               from punchOut in _WebServerDBContext.CardHistory.Where(s => s.CardID == n2.ID && s.PunchInDateTime.Substring(0, 10) == n1).OrderByDescending(s => s.PunchInDateTime).Take(1).DefaultIfEmpty()
                               orderby n3.Name, n1
                               select new TimeSheetReportModel
                               {
                                   UserName = n3.Name,
                                   Date = n1,
                                   PunchInTime = punchIn == null ? "" : punchIn.PunchInDateTime.Substring(11, 8),
                                   //排除只打一次卡，當天只有一筆資料時填空字串
                                   PunchOutTime = punchOut == null ? "" : (punchIn.PunchInDateTime == punchOut.PunchInDateTime ? "" : punchOut.PunchInDateTime.Substring(11, 8)),
                               }).ToList();

                //最後要輸出的檔案
                byte[] fileStream = Array.Empty<byte>();
                //轉檔
                using (var memoryStream = new MemoryStream()) {
                    //950 => Big5
                    using (var streamWriter = new StreamWriter(memoryStream, Encoding.GetEncoding(950)))
                    using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture)) {
                        csvWriter.WriteRecords(records);
                    }
                    fileStream = memoryStream.ToArray();
                }
                //回傳檔案
                return new FileStreamResult(new MemoryStream(fileStream), "application/octet-stream")
                {
                    FileDownloadName = $"{month}.csv",
                };
            }
            catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
        #endregion 匯出CSV

        #region 匯出PDF
        //TimeSheet/ExportPDF/{month}
        [Route("[controller]/[action]/{month}")]
        [HttpGet()]
        public async Task<IActionResult> ExportPDF(string month)
        {
            await Task.Yield();
            try {
                var tmpDate = DateTime.Parse(month + "-01");
                //列出要顯示的日期
                var dateList = Enumerable.Range(1, DateTime.DaysInMonth(tmpDate.Year, tmpDate.Month))
                        .Select(day => (new DateTime(tmpDate.Year, tmpDate.Month, day))
                        .ToString("yyyy-MM-dd"))
                        .ToList();
                //展開資料
                var records = (from n1 in dateList
                               from n2 in _WebServerDBContext.Card
                               join n3 in _WebServerDBContext.User on n2.UserID equals n3.ID
                               //上班打卡
                               from punchIn in _WebServerDBContext.CardHistory.Where(s => s.CardID == n2.ID && s.PunchInDateTime.Substring(0, 10) == n1).OrderBy(s => s.PunchInDateTime).Take(1).DefaultIfEmpty()
                                   //下班打卡
                               from punchOut in _WebServerDBContext.CardHistory.Where(s => s.CardID == n2.ID && s.PunchInDateTime.Substring(0, 10) == n1).OrderByDescending(s => s.PunchInDateTime).Take(1).DefaultIfEmpty()
                               orderby n3.Name, n1
                               select new TimeSheetReportModel
                               {
                                   UserName = n3.Name,
                                   Date = n1,
                                   PunchInTime = punchIn == null ? "" : punchIn.PunchInDateTime.Substring(11, 8),
                                   PunchOutTime = punchOut == null ? "" : (punchIn.PunchInDateTime == punchOut.PunchInDateTime ? "" : punchOut.PunchInDateTime.Substring(11, 8)),
                               }).ToArray();
                #region 產生PDF
                //使用專案檔案內的字型
                BaseFont bfChinese = BaseFont.CreateFont(Path.Combine("wwwroot", "fonts", "msjh.ttf"), BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                //產生PDF實體檔用
                MemoryStream pdfFileStream = new MemoryStream();

                //紙張 A4 直印
                iTextSharp.text.Document doc1 = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4);
                //取得每單位的寬和高 (px換cm)
                float wcm = (float)Math.Round(doc1.PageSize.Width / (float)21.00 * 1, 3);
                float hcm = (float)Math.Round(doc1.PageSize.Height / (float)29.70 * 1, 3);
                float shiftX = 1 * wcm; //左側留白

                iTextSharp.text.pdf.PdfWriter pdfWriter = iTextSharp.text.pdf.PdfWriter.GetInstance(doc1, pdfFileStream);
                doc1.Open();
                iTextSharp.text.pdf.PdfContentByte cb = pdfWriter.DirectContent;

                int pageRecords = 40; //每頁顯示幾筆
                int pages = (records.Length / pageRecords) + 1;

                //依字體大小計算行高
                float bodyFontSize = 14;
                var bodyAscentPoint = bfChinese.GetAscentPoint("計算高度用", bodyFontSize);
                var bodyDescentPoint = bfChinese.GetDescentPoint("計算高度用", bodyFontSize);
                var bodyFontHeight = bodyAscentPoint - bodyDescentPoint + 0.05f * hcm;

                for (int i = 0; i < pages; i++) {
                    //記錄目前的要顯示的Y軸位置
                    float currentBodyY = doc1.PageSize.Height;
                    if (i > 0)
                        doc1.NewPage();//加新頁

                    #region Header
                    cb.BeginText();
                    cb.SetFontAndSize(bfChinese, 16); //字型及大小
                    cb.SetColorFill(new BaseColor(Color.Black));
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_CENTER, $"刷卡記錄表", doc1.PageSize.Width / 2, doc1.PageSize.Height - 1f * hcm, 0); //主標

                    cb.EndText();
                    #endregion
                    currentBodyY = currentBodyY - 2f * hcm;

                    #region Body
                    //畫線設定 (欄位標題上格線)
                    cb.SetLineWidth(0.1f);
                    cb.SetColorStroke(new BaseColor(Color.Black));
                    cb.MoveTo(shiftX, currentBodyY + bodyFontHeight + 0.05f * hcm);
                    cb.LineTo(doc1.PageSize.Width - shiftX, currentBodyY + bodyFontHeight + 0.05f * hcm);
                    cb.Stroke(); //畫線結束

                    //欄位標題文字
                    cb.BeginText();
                    cb.SetFontAndSize(bfChinese, bodyFontSize);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"#", shiftX, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"姓名", shiftX + 2f * wcm, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"日期", shiftX + 7f * wcm, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"上班時間", shiftX + 12f * wcm, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"下班時間", shiftX + 17f * wcm, currentBodyY, 0);
                    cb.EndText();

                    //畫線設定 (欄位標題下格線)
                    cb.SetLineWidth(0.1f);
                    cb.SetColorStroke(new BaseColor(Color.Black));
                    cb.MoveTo(shiftX, currentBodyY - 0.1f * hcm);
                    cb.LineTo(doc1.PageSize.Width - shiftX, currentBodyY - 0.1f * hcm);
                    cb.Stroke();

                    //內容
                    currentBodyY -= (bodyFontHeight + 0.1f * hcm);
                    cb.BeginText();
                    for (int j = 0; j < pageRecords && (i * pageRecords + j) < records.Length; j++) {
                        cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{(i * pageRecords + j + 1)}", shiftX, currentBodyY, 0);
                        cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{records[i * pageRecords + j].UserName}", shiftX + 2f * wcm, currentBodyY, 0);
                        cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{records[i * pageRecords + j].Date}", shiftX + 7f * wcm, currentBodyY, 0);
                        cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{records[i * pageRecords + j].PunchInTime}", shiftX + 12f * wcm, currentBodyY, 0);
                        cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{records[i * pageRecords + j].PunchOutTime}", shiftX + 17f * wcm, currentBodyY, 0);

                        currentBodyY -= bodyFontHeight;
                    }
                    cb.EndText();
                    #endregion

                    #region Footer
                    float footerFontSize = 8;
                    cb.SetColorFill(new BaseColor(Color.Black));
                    cb.SetFontAndSize(bfChinese, footerFontSize);
                    float footerY = currentBodyY - 0.1f * hcm;
                    var sCurrentDT = $" 列印日期/時間：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 第{i + 1}頁/共{pages}頁 ----";
                    var sCurrentDT_width = bfChinese.GetWidthPoint(sCurrentDT, footerFontSize);
                    var dash_width = bfChinese.GetWidthPoint("-", footerFontSize);
                    var dashCount = Convert.ToInt32((doc1.PageSize.Width - 2 * shiftX - sCurrentDT_width) / dash_width);
                    cb.BeginText();
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_RIGHT, new String('-', dashCount) + sCurrentDT, doc1.PageSize.Width - shiftX, footerY, 0);
                    cb.EndText();
                    #endregion
                }
                doc1.Close();
                #endregion

                var result = Array.Empty<byte>();
                //加密
                using (MemoryStream output = new MemoryStream()) {
                    PdfReader reader = new PdfReader(pdfFileStream.ToArray());
                    //設定密碼 123456 (owner要編輯功能才看得到)
                    PdfEncryptor.Encrypt(reader, output, PdfWriter.ENCRYPTION_AES_128, "123456", null, PdfWriter.AllowPrinting);
                    result = output.ToArray();
                }
                return new FileStreamResult(new MemoryStream(result), "application/pdf")
                {
                    FileDownloadName = $"{month}.pdf",
                };
            }
            catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
        #endregion 匯出PDF
    }
}
