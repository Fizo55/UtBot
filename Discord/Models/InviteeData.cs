using Newtonsoft.Json;

namespace Discord.Models;

public class InviteeData
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("uid")]
    public string Uid { get; set; }

    [JsonProperty("registerTime")]
    public string RegisterTime { get; set; }

    [JsonProperty("totalTradingVolume")]
    public string TotalTradingVolume { get; set; }

    [JsonProperty("totalTradingFee")]
    public string TotalTradingFee { get; set; }

    [JsonProperty("totalCommision")]
    public string TotalCommision { get; set; }

    [JsonProperty("totalDeposit")]
    public string TotalDeposit { get; set; }

    [JsonProperty("totalWithdrawal")]
    public string TotalWithdrawal { get; set; }

    [JsonProperty("kycLevel")]
    public string KycLevel { get; set; }

    [JsonProperty("equity")]
    public string Equity { get; set; }
}