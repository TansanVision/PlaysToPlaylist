namespace PlaystoPlaylist.Api.Models;

/// <summary>
/// BeatLeader スコアの情報を表すクラスです。
/// </summary>
public sealed class BeatLeaderScore
{
    /// <summary>
    /// BeatLeader スコアの一意の識別子を取得または設定します。
    /// </summary>
    public long Id
    {
        get;
        set;
    }

    /// <summary>
    /// スコアを取得または設定します。
    /// </summary>
    public int BaseScore
    {
        get;
        set;
    }

    /// <summary>
    /// 修正後のスコアを取得または設定します。
    /// </summary>
    public int ModifiedScore
    {
        get;
        set;
    }

    /// <summary>
    /// 精度を取得または設定します。
    /// </summary>
    public double Accuracy
    {
        get;
        set;
    }

    /// <summary>
    /// PP（Performance Points）を取得または設定します。
    /// </summary>
    public double Pp
    {
        get;
        set;
    }

    /// <summary>
    /// 最大コンボ数を取得または設定します。
    /// </summary>
    public int MaxCombo
    {
        get;
        set;
    }

    /// <summary>
    /// フルコンボかどうかを取得または設定します。
    /// </summary>
    public bool FullCombo
    {
        get;
        set;
    }

    /// <summary>
    /// 悪いカットの数を取得または設定します。
    /// </summary>
    public int BadCuts
    {
        get;
        set;
    }

    /// <summary>
    /// ミスしたノートの数を取得または設定します。
    /// </summary>
    public int MissedNotes
    {
        get;
        set;
    }

    /// <summary>
    /// 爆弾を切った回数を取得または設定します。
    /// </summary>
    public int BombCuts
    {
        get;
        set;
    }

    /// <summary>
    /// 壁に当たった回数を取得または設定します。
    /// </summary>
    public int WallsHit
    {
        get;
        set;
    }

    /// <summary>
    /// 使用されたモディファイアを取得または設定します。
    /// </summary>
    public string? Modifiers
    {
        get;
        set;
    }

    /// <summary>
    /// スコアが投稿された時間を取得または設定します。
    /// </summary>
    public long Timepost
    {
        get;
        set;
    }

    /// <summary>
    /// リーダーボードを取得または設定します。
    /// </summary>
    public BeatLeaderLeaderboard? Leaderboard
    {
        get;
        set;
    }
}
