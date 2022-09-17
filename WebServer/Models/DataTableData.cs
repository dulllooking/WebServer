using System.Text.Json.Serialization;

namespace WebServer.Models
{
    // 自訂資料結構 for DataTable
    public class DataTableData
    {
        [JsonPropertyName("draw")]
        public int Draw { get; set; } // DataTable 套件內部對應名稱，不可修改
        [JsonPropertyName("data")]
        public object? Data { get; set; }
        [JsonPropertyName("recordsTotal")]
        public int RecordsTotal { get; set; }
        [JsonPropertyName("recordsFiltered")]
        public int RecordsFiltered { get; set; }
    }
}
