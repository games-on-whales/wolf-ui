using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Resources.WolfAPI;

namespace WolfUI;

public partial class Main
{
    private async void SelfUpdateAsync()
    {
        var autoupdateEnable = (System.Environment.GetEnvironmentVariable("WOLF_UI_AUTOUPDATE") ?? "True") == "True";
        if (!autoupdateEnable) return;
        var apps = await WolfApi.GetApps();
        var wolfUi = apps?.Apps?.FindAll(app => app?.Runner?.Image is not null
                                                 && app.Runner.Env is not null
                                                 && app.Runner.Image.Contains("wolf-ui")
                                                 && app.Runner.Env.Contains("WOLF_UI_AUTOUPDATE=True"))
            .FirstOrDefault();

        WolfApi.Singleton.ImageUpdated += async (img) =>
        {
            if (wolfUi?.Runner?.Image is null || img != wolfUi?.Runner.Image) return;
            if (!await QuestionDialogue.OpenDialogue<bool>("Restart", "Wolf-UI has been updated, please restart.",
                    new Dictionary<string, bool>
                    {
                        { "Restart", true },
                        { "Later", false }
                    })) return;
            await WolfApi.StartRunner(wolfUi.Runner);
            await Task.Delay(500);
            GetTree().Quit();
        };
        if (wolfUi?.Runner?.Image is not null)
            WolfApi.PullImage(wolfUi.Runner.Image);
    }
}
