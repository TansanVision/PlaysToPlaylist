using PlaysToPlaylist.Localization;
using Spectre.Console;

namespace PlaystoPlaylist.States.User;

public sealed class UserAddScreen : IScreen
{
    public async Task ExecuteAsync(ScreenContext context, CancellationToken cancellationToken = default)
    {
        ScreenUi.Header("AddUser");
        var input = (await AnsiConsole.PromptAsync(new TextPrompt<string>(Texts.Get("EnterId")).AllowEmpty(), cancellationToken)).Trim();
        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            context.ChangeScreen(new MainMenuScreen());
            return;
        }
        var user = await context.Services.User.AddUserAsync(input, cancellationToken: cancellationToken);
        ScreenUi.Message("Added", user.Name);
        await ScreenUi.PauseAsync(cancellationToken);
    }
}
