using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using WolfUI;

namespace Resources.WolfAPI;

public partial class WolfApi
{
    public static async Task<AppsRequest?> GetApps()
    {
        var message = await GetAsync<AppsRequest>($"{Api}/apps");
        return message ?? new AppsRequest(){ Success = false, Apps = [] };
    }
    public static async Task<List<App>> GetApps(Profile usedProfile)
    {
        var profiles = await GetProfiles();

        foreach (var profile in profiles)
        {
            if (profile.Id is not null && usedProfile.Id is not null && profile.Id == usedProfile.Id)
                return profile.Apps ?? [];
        }
        Logger.LogError("Profile: {0} not found", usedProfile.Name ?? "NULL");
        return [];
    }
    public static async Task<ErrorResponse> AddApp(App app)
    {
        var message = await PostAsync($"/apps/add", app);
        if (message == null) return new ErrorResponse() { Success = false };
        var response = JsonSerializer.Deserialize<ErrorResponse>(message);
        return response ?? new ErrorResponse() { Success = false };
    }

    public static async Task<ErrorResponse> DeleteApp(string appId)
    {
        var message = await PostAsync($"/apps/add", appId);
        if (message == null) return new ErrorResponse() { Success = false };
        var response = JsonSerializer.Deserialize<ErrorResponse>(message);
        return response ?? new ErrorResponse() { Success = false };
    }
}