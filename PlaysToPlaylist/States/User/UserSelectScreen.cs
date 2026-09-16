using PlaysToPlaylist.Localization;
using PlaysToPlaylist.States.User;
using Spectre.Console;

namespace PlaystoPlaylist.States.User;

public sealed class UserSelectScreen : IScreen
{
    public async Task ExecuteAsync(ScreenContext context, CancellationToken cancellationToken = default)
    {
        var users = await context.Services.User.GetAllUsersAsync();
        ScreenUi.Header("SelectUser");
        if (users.Length == 0) ScreenUi.Message("NoUsers");
        var selected = await AnsiConsole.PromptAsync(new SelectionPrompt<int>()
            .Title(Texts.Get("ChooseUser"))
            .MoreChoicesText(Texts.Get("ChooseMenu"))
            .UseConverter(i => i < 0 ? Texts.Get("Back") : Markup.Escape($"{users[i].Name} ({users[i].BeatLeaderId})"))
            .AddChoices(Enumerable.Range(0, users.Length).Append(-1)), cancellationToken);
        if (selected < 0) context.ChangeScreen(new MainMenuScreen());
        else
        {
            context.SelectUser(users[selected]);
            context.ChangeScreen(new UserSelectedScreen());
        }
    }
}
