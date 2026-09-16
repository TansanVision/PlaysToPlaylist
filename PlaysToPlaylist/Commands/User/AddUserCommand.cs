//using PlaystoPlaylist.Services;
//using Spectre.Console;
//using System.CommandLine;
//namespace PlaystoPlaylist.Commands.User;

///// <summary>
///// ユーザーを登録するコマンドを提供するクラスです。
///// </summary>
//public static class AddUserCommand
//{
//    /// <summary>
//    /// ユーザーを登録するコマンドを作成します。
//    /// </summary>
//    /// <param name="userService"></param>
//    /// <returns></returns>
//    public static Command Create(UserService userService)
//    {
//        var playerIdArgument =
//            new Argument<string>("player-id")
//            {
//                Description = "BeatLeader Player ID"
//            };

//        var aliasOption =
//            new Option<string?>("--alias")
//            {
//                Description = "User alias (optional)"
//            };

//        var command =
//            new Command("add", "Register a BeatLeader player");

//        command.Arguments.Add(playerIdArgument);
//        command.Options.Add(aliasOption);

//        command.SetAction(async parseResult =>
//        {
//            var playerId =
//                parseResult.GetValue(playerIdArgument);
//            var alias = 
//                parseResult.GetValue(aliasOption);

//            if (string.IsNullOrWhiteSpace(playerId))
//            {
//                AnsiConsole.MarkupLine("[red]Player ID is required.[/]");
//                return;
//            }

//            try
//            {
//                var user = 
//                    await AnsiConsole.Status()
//                        .Spinner(Spinner.Known.Dots)
//                        .StartAsync("Fetching BeatLeader player...", 
//                            async _ =>
//                            {
//                                return
//                                    await userService.AddUserAsync(playerId, alias);
//                            });

//                AnsiConsole.MarkupLine($"[green]User registered successfully![/]");

//                AnsiConsole.MarkupLine($"");

//                var table = new Table();
//                table.AddColumn("Property");
//                table.AddColumn("Value");

//                table.AddRow("BeatLeader ID", Markup.Escape(user.BeatLeaderId));
//                table.AddRow("Alias", Markup.Escape(user.Alias ?? "-"));

//                AnsiConsole.Write(table);
//            }
//            catch (Exception ex)
//            {
//                AnsiConsole.MarkupLine($"[red]Error registering user: {ex.Message}[/]");
//                return;
//            }
//        });
//        return command;
//    }
//}
