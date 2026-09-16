namespace PlaystoPlaylist.Models;

/// <summary>
/// プレイリストの曲を表すクラスです。
/// </summary>
public sealed class PlaylistSong
{
    /// <summary>
    /// 曲のキーを取得または設定します。
    /// </summary>
    public string? Key
    {
        get;
        set;
    }

    /// <summary>
    /// 曲のハッシュ値を取得または設定します。
    /// </summary>
    public string Hash
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// 曲の名前を取得または設定します。
    /// </summary>
    public string Name
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// 難易度を取得または設定します。
    /// </summary>
    public PlaylistDificulty[] Dificulties
    {
        get;
        set;
    } = [];
}

/// <summary>
/// プレイリストの難易度を表すクラスです。
/// </summary>
public sealed class PlaylistDificulty
{
    /// <summary>
    /// 難易度の特徴を取得または設定します。
    /// </summary>
    public string Characteristic
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// 難易度の名前を取得または設定します。
    /// </summary>
    public string Name
    {
        get;
        set;
    } = string.Empty;
}