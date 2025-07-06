using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Resources.WolfAPI;

public partial class WolfApi
{
    public static async Task<Session?> GetSession()
    {
        var sessions = await WolfApi.GetAsync<SessionsResponse>("http://localhost/api/v1/sessions");
        if (sessions?.Sessions is null) return null;
        var currSession = sessions.Sessions.FirstOrDefault(session => session.ClientId == SessionId);
        if (currSession is not null) return currSession;
        Logger.LogWarning("No owned Session found. Is this run without Wolf?");

        return new Session
        {
            VideoWidth = 1920,
            VideoHeight = 1080,
            VideoRefreshRate = 60,
            AudioChannelCount = 2,
            ClientSettings = new ClientSettings()
        };
    }
    
    public static async void StopSession(string sessionId)
    {
        var url = "http://localhost/api/v1/session/stop";
        try
        {
            var data = $$"""
                         {
                             "session_id": "{{sessionId}}"
                         }
                         """;
            Logger.LogDebug("API call POST: {0} - {1}", url, data);
            StringContent content = new(data);
            var result = await _httpClient.PostAsync(url, content);
            var returnData = await result.Content.ReadAsStringAsync();
            Logger.LogDebug("API answer from: {0} - {1}", url, returnData);
        }
        catch (Exception e)
        {
            Logger.LogError("Error POST request failed: {0} Message: {1}", url, e.Message);
        }
    }
}