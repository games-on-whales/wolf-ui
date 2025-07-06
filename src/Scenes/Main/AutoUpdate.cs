using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Resources.WolfAPI;

namespace WolfUI;

public partial class Main
{
    private async void SelfUpdateAsync()
    {
        bool AutoupdateEnable = (System.Environment.GetEnvironmentVariable("WOLF_UI_AUTOUPDATE") ?? "True") == "True";
        if (AutoupdateEnable)
        {
            var apps = await WolfApi.GetApps();
            App? Wolf_UI = apps?.Apps?.FindAll(app => app?.Runner?.Image is not null
                                                                             && app.Runner.Env is not null
                                                                             && app.Runner.Image.Contains("wolf-ui")
                                                                             && app.Runner.Env.Contains("WOLF_UI_AUTOUPDATE=True"))
                                    .FirstOrDefault();

            WolfApi.Singleton.ImageUpdated += async (img) =>
            {
                if (Wolf_UI?.Runner?.Image is not null && img == Wolf_UI?.Runner.Image)
                {
                    if (await QuestionDialogue.OpenDialogue<bool>("Restart", "Wolf-UI has been updated, please restart.",
                        new Dictionary<string, bool> {
                            { "Restart", true },
                            { "Later", false }
                        }))
                    {
                        await WolfApi.StartApp(Wolf_UI.Runner);
                        await Task.Delay(500);
                        GetTree().Quit();
                    }
                }
            };
            if (Wolf_UI?.Runner?.Image is not null)
                WolfApi.PullImage(Wolf_UI.Runner.Image);
        }
    }
}
