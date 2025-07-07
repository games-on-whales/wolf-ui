using System.Threading.Tasks;
using System.Collections.Generic;

namespace Resources.WolfAPI;


public partial class WolfApi
{
    public static async Task<List<Profile>> GetProfiles()
    {
        var profiles = await GetAsync<ProfilesResponse>($"{Api}/profiles");

        if (profiles?.Profiles is not null && profiles.Success) return profiles.Profiles;
        Logger.LogError("Error retrieving Profiles");
        return [];
    }
}