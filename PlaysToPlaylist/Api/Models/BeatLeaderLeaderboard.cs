using System;
using System.Collections.Generic;
using System.Text;

namespace PlaystoPlaylist.Api.Models;

/// <summary>
/// BeatLeaderのリーダーボード情報を表すクラスです。
/// </summary>
public sealed class BeatLeaderLeaderboard
{
    /// <summary>
    /// BeatLeaderのリーダーボードの一意の識別子を取得または設定します。
    /// </summary>
    public string Id
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// BeatLeaderのリーダーボードに関連する曲の情報を取得または設定します。
    /// </summary>
    public BeatLeaderSong? Song
    {
        get;
        set;
    } = null;

    /// <summary>
    /// BeatLeaderのリーダーボードに関連する難易度の情報を取得または設定します。
    /// </summary>
    public BeatLeaderDifficulty? Difficulty
    {
        get;
        set;
    } = null;
}
