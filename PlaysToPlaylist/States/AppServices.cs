using PlaystoPlaylist.Services;

namespace PlaystoPlaylist.States;

/// <summary>
/// アプリが提供するサービスをまとめたクラスです。
/// </summary>
public sealed class AppServices
{
    /// <summary>
    /// アプリケーションのパスを管理するサービスを提供します。
    /// </summary>
    public AppPaths AppPaths 
    { 
        get; 
    }

    /// <summary>
    /// ユーザー関連のサービスを提供します。
    /// </summary>
    public UserService User
    {
        get;
    }

    /// <summary>
    /// プレイリスト関連のサービスを提供します。
    /// </summary>
    public PlaylistService Playlist
    {
        get;
    }

    /// <summary>
    /// 履歴関連のサービスを提供します。
    /// </summary>
    public HistoryService History
    {
        get;
    }

    /// <summary>
    /// AppServices クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="userService">ユーザー関連のサービス</param>
    /// <param name="playlistService">プレイリスト関連のサービス</param>
    /// <param name="historyService">履歴関連のサービス</param>
    /// <param name="appPaths">アプリケーションのパスを管理するサービス</param>
    public AppServices(
        UserService userService,
        PlaylistService playlistService,
        HistoryService historyService,
        AppPaths appPaths)
    {
        User = userService;
        Playlist = playlistService;
        History = historyService;
        AppPaths = appPaths;
    }
}
