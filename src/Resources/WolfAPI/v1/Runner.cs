using System.Threading.Tasks;
using System.Collections.Generic;

namespace Resources.WolfAPI;


public partial class WolfApi
{
    public static async Task StartRunnerEvent(Runner runner, bool joinable = false)
    {
        var starter = new Starter()
        {
            StopStreamWhenOver = false,
            SessionId = SessionId,
            Runner = runner
        };
        var result = await PostAsync("/runners/start", starter);
    }
    
    public static async Task PauseRunner(Runner runner, string sessionId)
    {
        var pauseRunner = new PauseRunnerRecord()
        {
            SessionId = SessionId,
            Runner = runner
        };

        var result = await PostAsync("/runners/pause", pauseRunner);
    }
    
    public static async Task ResumeRunner(Runner runner, string sessionId)
    {
        var resumeRunner = new ResumeRunnerRecord()
        {
            SessionId = SessionId,
            Runner = runner
        };

        var result = await PostAsync("/runners/resume", resumeRunner);
    }
}