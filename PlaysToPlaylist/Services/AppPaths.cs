using System;
using System.Collections.Generic;
using System.Text;

namespace PlaystoPlaylist.Services;

/// <summary>
/// アプリケーションのパスを管理するクラスです。
/// </summary>
public sealed class AppPaths
{
    /// <summary>
    /// データディレクトリのパスを取得します。
    /// </summary>
    public string DataDirectory
    {
        get;
    }

    /// <summary>
    /// ユーザーディレクトリのパスを取得します。
    /// </summary>
    public string UsersDirectory
    {
        get;
    }

    /// <summary>
    /// データベースファイルのパスを取得します。
    /// </summary>
    public string DatabasePath
    {
        get;
    }

    /// <summary>
    /// <see cref="AppPaths"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    public AppPaths()
    {
        this.DataDirectory =
            Path.Combine(
                System.AppContext.BaseDirectory,
                "Data");

        this.UsersDirectory =
            Path.Combine(
                this.DataDirectory,
                "Users");

        this.DatabasePath =
            Path.Combine(
                this.DataDirectory,
                "PlaysToPlaylists.db");

        Directory.CreateDirectory(this.DataDirectory);
        Directory.CreateDirectory(this.UsersDirectory);
    }

    /// <summary>
    /// 指定された BeatLeader ID に対応するユーザーディレクトリのパスを取得します。
    /// </summary>
    /// <param name="beatleaderId">BeatLeader ID</param>
    /// <returns>ユーザーディレクトリのパス</returns>
    public string GetUserDirectory(string beatleaderId)
    {
        var path =
            Path.Combine(
                this.UsersDirectory,
                beatleaderId);

        Directory.CreateDirectory(path);

        return path;
    }

    /// <summary>
    /// 指定された BeatLeader ID と日付範囲に対応するプレイリストファイルのパスを取得します。
    /// </summary>
    /// <param name="beatleaderId">BeatLeader ID</param>
    /// <param name="from">開始日</param>
    /// <param name="to">終了日</param>
    /// <returns>プレイリストファイルのパス</returns>
    public string GetPlaylistPath(
        string beatleaderId, 
        DateOnly from, 
        DateOnly to)
    {
        var userDirectory = this.GetUserDirectory(beatleaderId);

        string fileName =
            from == to
            ? $"{from:yyyy-MM-dd}.bplist"
            : $"{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.bplist";

        return Path.Combine(
            userDirectory,
            fileName
        );
    }
}
