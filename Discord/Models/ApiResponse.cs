using Newtonsoft.Json;

namespace Discord.Models;

public class ApiResponse<T>
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("msg")]
    public string Msg { get; set; }

    [JsonProperty("data")]
    public T Data { get; set; }
}