using Newtonsoft.Json;

namespace Discord.Data;

public class TickerData
{
    [JsonProperty("s")] public string Symbol { get; set; }
    [JsonProperty("v")] public decimal Volume { get; set; }
    [JsonProperty("c")] public decimal LastPrice { get; set; }
}