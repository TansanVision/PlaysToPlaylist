using PlaystoPlaylist.States;
using PlaystoPlaylist.States.User;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlaysToPlaylist.States.User;

public sealed class UserSelectedScreen : IScreen
{
    public async Task ExecuteAsync(
        ScreenContext context,
        CancellationToken cancellationToken = default)
    {
        bool isUpdated = false;

        AnsiConsole.Clear();

        if (isUpdated)
        {
            AnsiConsole.MarkupLine("[bold green]ユーザー情報を更新しました。[/]");
        }

        AnsiConsole.MarkupLine("[bold yellow]Attempts To Playlist / Main Menu / User Selected[/]");
        var menu = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[gray]メニューを選択してください(現在選択しているユーザー: {context.SelectedUser?.Name}({context.SelectedUser?.Id}) ):[/]")
                .AddChoices(new[] { "Update", "Remove", "Playlist", "Back" }));

        switch (menu)
        {
            case "Update":
                await context.Services.User.UpdateUserAsync(context.SelectedUser!, cancellationToken);
                isUpdated = true;
                break;
            case "Remove":
                await context.Services.User.RemoveAsync(context.SelectedUser!);
                context.ClearSelectedUser();
                context.ChangeScreen(new UserSelectScreen());
                break;
            case "Playlist":
                context.ChangeScreen(new UserPlaylistScreen());
                break;
            case "Back":
                context.ClearSelectedUser();
                context.ChangeScreen(new UserSelectScreen());
                break;
        }
    }
}
