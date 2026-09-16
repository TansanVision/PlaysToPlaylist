using PlaystoPlaylist.States.User;
using Spectre.Console;

namespace PlaystoPlaylist.States;

/// <summary>
/// メインメニューのスクリーンを表します。
/// </summary>
public sealed class MainMenuScreen : IScreen
{
    /// <summary>
    /// 実行します。
    /// </summary>
    /// <param name="context">スクリーンのコンテキスト</param>
    /// <param name="cancellationToken">キャンセレーション トークン</param>
    /// <returns><see cref="Task"/></returns>
    public Task ExecuteAsync(
        ScreenContext context, 
        CancellationToken cancellationToken = default)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold yellow]Attempts To Playlist / Main Menu[/]");
        var menu = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[gray]メニューを選択してください:[/]")
                .AddChoices(["Select User", "Add User", "Exit"]));

        switch (menu)
        {
            case "Select User":
                context.ChangeScreen(new UserSelectScreen());
                break;
            case "Add User":
                context.ChangeScreen(new UserAddScreen());
                break;
            case "Exit":
                context.Exit();
                break;
        }

        return Task.CompletedTask;
    }
}
