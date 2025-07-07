using System.Threading.Tasks;
using System.Collections.Generic;

namespace Resources.WolfAPI;


public partial class WolfApi
{
    public static async Task StartRunner(Runner runner, bool joinable = false)
    {
        var starter = new Starter()
        {
            StopStreamWhenOver = false,
            SessionId = SessionId,
            Runner = runner
        };
        var result = await PostAsync("/runners/start", starter);
    }
}