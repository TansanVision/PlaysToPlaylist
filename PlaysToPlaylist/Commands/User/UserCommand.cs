//using PlaystoPlaylist.Services;
//using System.CommandLine;

//namespace PlaystoPlaylist.Commands.User;

///// <summary>
///// ユーザー関連のコマンドを提供するクラスです。
///// </summary>
//public static class UserCommand
//{
//    /// <summary>
//    /// ユーザー関連のコマンドを作成します。
//    /// </summary>
//    /// <param name="userService">ユーザーサービスのインスタンス</param>
//    /// <returns>作成されたコマンド</returns>
//    public static Command Create(UserService userService)
//    {
//        var command =
//            new Command(
//                "user", 
//                "Manage BeatLeader users");

//        command.Subcommands.Add(AddUserCommand.Create(userService));
//        return command;
//    }
//}
