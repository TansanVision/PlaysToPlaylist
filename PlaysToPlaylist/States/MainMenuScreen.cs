using PlaystoPlaylist.States.User;
using PlaysToPlaylist.Localization;
using Spectre.Console;

namespace PlaystoPlaylist.States;

public sealed class MainMenuScreen : IScreen
{
    public async Task ExecuteAsync(ScreenContext context, CancellationToken cancellationToken = default)
    {
        ScreenUi.Header("MainMenu");
        switch (await ScreenUi.ChooseAsync(Texts.Get("ChooseMenu"), cancellationToken, "SelectUser", "AddUser", "Language", "Exit"))
        {
            case "SelectUser": context.ChangeScreen(new UserSelectScreen()); break;
            case "AddUser": context.ChangeScreen(new UserAddScreen()); break;
            case "Language":
                var language = await AnsiConsole.PromptAsync(new SelectionPrompt<string>()
                    .Title("言語 / Language").AddChoices("日本語", "English"), cancellationToken);
                Texts.SetLanguage(language == "日本語" ? "ja" : "en");
                File.WriteAllText(Path.Combine(context.Services.AppPaths.DataDirectory, "language.txt"), Texts.Language);
                break;
            case "Exit": context.Exit(); break;
        }

    }
}
