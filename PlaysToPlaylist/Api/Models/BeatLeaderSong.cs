namespace PlaystoPlaylist.Api.Models;

/// <summary>
/// BeatLeaderの曲情報を表すクラスです。
/// </summary>
public sealed class BeatLeaderSong
{
    /// <summary>
    /// BeatLeaderの曲の一意の識別子を取得または設定します。
    /// </summary>
    public string Id
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeaderの曲のハッシュ値を取得または設定します。
    /// </summary>
    public string Hash
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeaderの曲の名前を取得または設定します。
    /// </summary>
    public string Name
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeaderの曲のサブネームを取得または設定します。
    /// </summary>
    public string SubName
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeaderの曲のフルネームを取得します。フルネームは、曲の名前とサブネームを組み合わせたものです。
    /// </summary>
    public string FullName => $"{this.Name} {this.SubName}".Trim();

    /// <summary>
    /// BeatLeaderの曲の作者を取得または設定します。
    /// </summary>
    public string? Author
    {
        get;
        set;
    }

    /// <summary>
    /// BeatLeaderの曲のマッパーを取得または設定します。
    /// </summary>
    public string? Mapper
    {
        get;
        set;
    }
}
