using PlaysToPlaylist.States.User;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlaystoPlaylist.States.User;

public sealed class UserSelectScreen : IScreen
{
    /// <summary>
    /// 実行します。
    /// </summary>
    /// <param name="context">スクリーンのコンテキスト</param>
    /// <param name="cancellationToken">キャンセレーション トークン</param>
    /// <returns><see cref="Task"/></returns>
    public async Task ExecuteAsync(
        ScreenContext context,
        CancellationToken cancellationToken = default)
    {
        var allUser = await context.Services.User.GetAllUsersAsync();

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold yellow]Attempts To Playlist / Main Menu / User Select[/]");
        var menu = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[gray]ユーザーを選択してください:[/]")
                .AddChoices(allUser.Select(u => $"{u.Name}({u.Id})").Concat(new[] { "Back" })));

        if (menu != "Back")
        {
            var selectedUser = allUser.FirstOrDefault(u => $"{u.Name}({u.Id})" == menu);
            if (selectedUser != null)
            {
                context.SelectUser(selectedUser);
                context.ChangeScreen(new UserSelectedScreen());
            }
        }
        else
        {
            context.ChangeScreen(new MainMenuScreen());
        }
    }
}
