using Newtonsoft.Json;

namespace Discord.Data;

public class BinanceStreamPayload
{
    [JsonProperty("stream")] 
    public string StreamName { get; set; }

    [JsonProperty("data")] 
    public List<TickerData> Data { get; set; }
}