using PlaystoPlaylist.Models;
using Spectre.Console;

namespace PlaystoPlaylist.States.User;

/// <summary>
/// ユーザー追加のスクリーンを表します。
/// </summary>
public sealed class UserAddScreen : IScreen
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
        bool isErrored = false;
        bool isNotFound = false;
        bool isDuplicate = false;
        RegisteredUser? registeredUser = null;

        do
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold yellow]Attempts To Playlist / Main Menu / Add User[/]");

            if (isErrored)
            {
                AnsiConsole.MarkupLine("[red]Invalid input. Please try again.[/]");
                isErrored = false;
            }

            if (isNotFound)
            {
                AnsiConsole.MarkupLine("[red]User not found. Please try again.[/]");
                isNotFound = false;
            }

            if (isDuplicate)
            {
                AnsiConsole.MarkupLine("[red]User already exists. Please try again.[/]");
                isDuplicate = false;
            }

            if (registeredUser is not null)
            {
                AnsiConsole.MarkupLine($"[green]User {registeredUser.BeatLeaderId} added successfully![/]");
                registeredUser = null;
            }

            var input = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter Beat Leader ID or exit:"));

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                context.ChangeScreen(new MainMenuScreen());
                return;
            }

            if (await context.Services
                       .User
                       .ExistsUserByIdAsync(input, cancellationToken))
            {
                isDuplicate = true;
                continue;
            }

            if (context.Services
                       .User
                       .ExistsBeatLeaderPlayerByIdAsync(input, cancellationToken)
                       .WaitAsync(cancellationToken)
                       .Result)
            {
                registeredUser =
                    context.Services
                           .User
                           .AddUserAsync(input)
                           .WaitAsync(cancellationToken)
                           .Result;

                if (registeredUser is null)
                {
                    isErrored = true;
                }
            }
            else
            {
                isNotFound = true;
            }
        } while (true);
    }
}
