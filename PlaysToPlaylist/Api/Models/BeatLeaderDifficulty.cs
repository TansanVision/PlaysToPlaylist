using System;
using System.Collections.Generic;
using System.Text;

namespace PlaystoPlaylist.Api.Models;

/// <summary>
/// BeatLeaderの曲の難易度情報を表すクラスです。
/// </summary>
public sealed class BeatLeaderDifficulty
{
    /// <summary>
    /// BeatLeaderの曲の難易度の値を取得または設定します。
    /// </summary>
    public int Value
    {
        get;
        set;
    }
    
    /// <summary>
    /// BeatLeaderの曲の難易度の名前を取得または設定します。
    /// </summary>
    public string DifficultyName
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeaderの曲のモード名を取得または設定します。
    /// </summary>
    public string ModeName
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeaderの曲の難易度の星評価を取得または設定します。
    /// </summary>
    public double? Stars
    {
        get;
        set;
    }

    /// <summary>
    /// BeatLeaderの曲の難易度の最大スコアを取得または設定します。
    /// </summary>
    public int? MaxScore
    {
        get;
        set;
    }
}
