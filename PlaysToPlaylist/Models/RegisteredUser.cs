namespace PlaystoPlaylist.Models;

/// <summary>
/// 登録済みユーザーの情報を表すクラスです。
/// </summary>
public sealed class RegisteredUser
{
    /// <summary>
    /// 一意の識別子を取得または設定します。
    /// </summary>
    public long Id
    {
        get;
        set;
    }

    /// <summary>
    /// BeatLeader プレイヤーの一意の識別子を取得または設定します。
    /// </summary>
    public string BeatLeaderId
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// 別名を取得または設定します。
    /// </summary>
    public string? Alias
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// 名前を取得または設定します。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// アバターの URL を取得または設定します。
    /// </summary>
    public string? AvatarUrl
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// 作成日時を取得または設定します。
    /// </summary>
    public DateTimeOffset CreatedAt
    {
        get;
        set;
    }

    /// <summary>
    /// 最終更新日時を取得または設定します。
    /// </summary>
    public DateTimeOffset UpdatedAt
    {
        get;
        set;
    }
}
