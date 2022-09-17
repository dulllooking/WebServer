using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebServer.Extensions
{
    // 回傳JSON時, 依 JsonPropertyName 名稱
    public class SystemTextJsonResult : ContentResult
    {
        private const string ContentTypeApplicationJson = "application/json";

        public SystemTextJsonResult(object value, JsonSerializerOptions options = null)
        {
            ContentType = ContentTypeApplicationJson;
            Content = options == null ? JsonSerializer.Serialize(value) : JsonSerializer.Serialize(value, options);
        }
    }
}
