using System.Threading.Tasks;
using System.Collections.Generic;

namespace Resources.WolfAPI;


public partial class WolfApi
{
    public static async Task<List<Client>> GetClients()
    {
        var wolfClients = await GetAsync<ClientsRequest>($"{Api}/clients");

        if (wolfClients?.Success == true)
            return wolfClients.Clients ?? [];
        return [];
    }
}