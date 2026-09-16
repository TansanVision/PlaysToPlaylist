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
    public AppPaths(string? dataDirectory = null)
    {
        this.DataDirectory = dataDirectory ??
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
    /// ID が安全なディレクトリ名であることを検証します。
    /// </summary>
    /// <param name="id">BeatLeader ID</param>
    public static void ValidatePlayerId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 128 ||
            id.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_') ||
            System.Text.RegularExpressions.Regex.IsMatch(id, "^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new ArgumentException(PlaysToPlaylist.Localization.Texts.Get("InvalidId"));
    }

    public string GetUserDirectory(string beatleaderId)
    {
        ValidatePlayerId(beatleaderId);
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
            ? FormattableString.Invariant($"{from:yyyy-MM-dd}.bplist")
            : FormattableString.Invariant($"{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.bplist");

        return Path.Combine(
            userDirectory,
            fileName
        );
    }
}
