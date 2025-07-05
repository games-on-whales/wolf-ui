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
            var apps = await WolfAPI.GetApps();
            App? Wolf_UI = apps?.apps?.FindAll(app => app?.runner?.image is not null
                                            && app.runner.env is not null
                                            && app.runner.image.Contains("wolf-ui")
                                            && app.runner.env.Contains("WOLF_UI_AUTOUPDATE=True"))
                                    .FirstOrDefault();

            WolfAPI.Singleton.ImageUpdated += async (img) =>
            {
                if (Wolf_UI?.runner?.image is not null && img == Wolf_UI?.runner.image)
                {
                    if (await QuestionDialogue.OpenDialogue<bool>("Restart", "Wolf-UI has been updated, please restart.",
                        new Dictionary<string, bool> {
                            { "Restart", true },
                            { "Later", false }
                        }))
                    {
                        await WolfAPI.StartApp(Wolf_UI.runner);
                        await Task.Delay(500);
                        GetTree().Quit();
                    }
                }
            };
            if (Wolf_UI?.runner?.image is not null)
                WolfAPI.PullImage(Wolf_UI.runner.image);
        }
    }
}
