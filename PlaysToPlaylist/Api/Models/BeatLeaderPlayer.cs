using System;
using System.Collections.Generic;
using System.Text;

namespace PlaystoPlaylist.Api.Models;

/// <summary>
/// BeatLeader プレイヤーの情報を表すクラスです。
/// </summary>
public sealed class BeatLeaderPlayer
{
    /// <summary>
    /// BeatLeader プレイヤーの一意の識別子を取得または設定します。
    /// </summary>
    public string Id
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeader プレイヤーの名前を取得または設定します。
    /// </summary>
    public string Name
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeader プレイヤーのアバターの URL を取得または設定します。
    /// </summary>
    public string? Avatar
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeader プレイヤーの国を取得または設定します。
    /// </summary>
    public string? Country
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeader プレイヤーのパフォーマンスポイント (pp) を取得または設定します。
    /// </summary>
    public double Pp
    {
        get;
        set;
    } = 0.0;

    /// <summary>
    /// BeatLeader プレイヤーのランクを取得または設定します。
    /// </summary>
    public int Rank
    {
        get;
        set;
    } = 0;

    /// <summary>
    /// BeatLeader プレイヤーの国別ランクを取得または設定します。
    /// </summary>
    public int CountryRank
    {
        get;
        set;
    } = 0;
}
