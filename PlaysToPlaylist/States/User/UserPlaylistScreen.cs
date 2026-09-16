using PlaystoPlaylist.States;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlaysToPlaylist.States.User;

public sealed class UserPlaylistScreen : IScreen
{
    public async Task ExecuteAsync(
        ScreenContext context,
        CancellationToken cancellationToken = default)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold yellow]Attempts To Playlist / Main Menu / User Playlist[/]");
        var menu = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[gray]メニューを選択してください(現在選択しているユーザー: {context.SelectedUser?.Name}({context.SelectedUser?.Id}) ):[/]")
                .AddChoices(new[] { "Create", "Back" }));

        switch (menu)
        {
            case "Create":
                while (true)
                {
                    var from = AnsiConsole.Prompt<DateOnly>(new TextPrompt<DateOnly>("[gray]Enter the start date (yyyy-MM-dd):[/]").Validate(date =>
                    {
                        if (date > DateOnly.FromDateTime(DateTime.Now))
                        {
                            return ValidationResult.Error("[red]The date cannot be in the future.[/]");
                        }
                        return ValidationResult.Success();
                    }));

                    var to = AnsiConsole.Prompt<DateOnly>(new TextPrompt<DateOnly>("[gray]Enter the end date (yyyy-MM-dd):[/]").Validate(date =>
                    {
                        if (date > DateOnly.FromDateTime(DateTime.Now))
                        {
                            return ValidationResult.Error("[red]The date cannot be in the future.[/]");
                        }
                        if (date < from)
                        {
                            return ValidationResult.Error("[red]The end date cannot be earlier than the start date.[/]");
                        }
                        return ValidationResult.Success();
                    }));

                    if (from > to)
                    {
                        AnsiConsole.MarkupLine("[red]The start date cannot be later than the end date. Please try again.[/]");
                        continue;
                    }

                    await context.Services.Playlist.CreateAsync(context.SelectedUser!, from, to, (progress) =>
                    {
                        AnsiConsole.MarkupLine($"[gray]{progress}[/]");
                    }, cancellationToken);

                    var confirmation = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[green]Playlist creation completed. Do you want to create another playlist?[/]")
                            .AddChoices(new[] { "Yes", "No" }));

                    if (confirmation == "No")
                        break;
                }
                break;
            case "Back":
                context.ChangeScreen(new UserSelectedScreen());
                break;
        }
    }
}
