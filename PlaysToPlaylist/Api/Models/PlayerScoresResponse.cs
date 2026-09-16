using System;
using System.Collections.Generic;
using System.Text;

namespace PlaystoPlaylist.Api.Models;

/// <summary>
/// プレイヤーのスコア情報を表すレスポンスクラスです。
/// </summary>
public sealed class PlayerScoresResponse
{
    /// <summary>
    /// プレイヤーのスコア情報のリストを取得または設定します。
    /// </summary>
    public List<BeatLeaderScore> Data
    {
        get;
        set;
    }

    /// <summary>
    /// ページネーション情報を取得または設定します。
    /// </summary>
    public Metadata? Metadata
    {
        get;
        set;
    }
}

/// <summary>
/// ページネーション情報を表すクラスです。
/// </summary>
public sealed class Metadata
{
    /// <summary>
    /// 1ページあたりのアイテム数を取得または設定します。
    /// </summary>
    public int ItemsPerPage
    {
        get;
        set;
    }

    /// <summary>
    /// 現在のページ番号を取得または設定します。
    /// </summary>
    public int Page
    {
        get;
        set;
    }

    /// <summary>
    /// 総アイテム数を取得または設定します。
    /// </summary>
    public int Total
    {
        get;
        set;
    }
}
