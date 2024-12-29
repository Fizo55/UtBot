using Discord.Interactions;
using Discord.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Discord.Commands.Modules;

public class GeneralCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public GeneralCommands(IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = new HttpClient();
    }

    [SlashCommand("get-invitees", "Fetch invitee data from the API")]
    public async Task GetInviteesAsync(string uid = null, long? begin = null, long? end = null, int? limit = 10)
    {
        string baseApiUrl = _configuration["BaseBlofinAPIUri"]!;
        if (string.IsNullOrEmpty(baseApiUrl))
        {
            await RespondAsync("Api base url missing.");
            return;
        }

        string apiKey = _configuration["BlofinAPIKey"]!;
        string secretKey = _configuration["BlofinAPISecret"]!;
        string passphrase = _configuration["BlofinPassphrase"]!;

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(passphrase))
        {
            await RespondAsync("Api credentials missing.");
            return;
        }

        RestAuthenticator restAuthenticator = new(apiKey, secretKey, passphrase);

        var queryParams = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(uid)) queryParams["uid"] = uid;
        if (begin.HasValue) queryParams["begin"] = begin.Value.ToString();
        if (end.HasValue) queryParams["end"] = end.Value.ToString();
        queryParams["limit"] = limit?.ToString() ?? "10";

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var requestPath = $"/api/v1/affiliate/invitees?{queryString}";
        var url = $"{baseApiUrl}{requestPath}";

        var (accessKey, accessSign, accessTimestamp, accessNonce, accessPassphrase) = restAuthenticator.GetHeaders("GET", requestPath);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("ACCESS-KEY", accessKey);
        request.Headers.Add("ACCESS-SIGN", accessSign);
        request.Headers.Add("ACCESS-TIMESTAMP", accessTimestamp);
        request.Headers.Add("ACCESS-NONCE", accessNonce);
        request.Headers.Add("ACCESS-PASSPHRASE", accessPassphrase);

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                await RespondAsync("Couldn't fetch invitees datas.");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<List<InviteeData>>>(content);

            if (result?.Code != 200)
            {
                await RespondAsync($"Api returned an error: {result?.Msg ?? "Unknown error"}");
                return;
            }

            if (result.Data.Count == 0)
            {
                await RespondAsync("No referees for this account.");
                return;
            }

            var responseMessage = "Referees data:\n" + string.Join("\n", result.Data.Select(inv =>
                $"- **ID**: {inv.Id}, **UID**: {inv.Uid}, **Register Time**: {inv.RegisterTime}, **KYC Level**: {inv.KycLevel}"));
            await RespondAsync(responseMessage);
        }
        catch (Exception ex)
        {
            await RespondAsync("An error occurred while fetching referees data.");
        }
    }
}
