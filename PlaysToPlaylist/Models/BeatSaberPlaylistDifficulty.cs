using System.Text.Json.Serialization;

namespace PlaysToPlaylist.Models;

/// <summary>
/// Beat Saber プレイリストの難易度を表すクラスです。
/// </summary>
public sealed class BeatSaberPlaylistDifficulty
{
    /// <summary>
    /// 難易度の特徴を取得または設定します。
    /// </summary>
    [JsonPropertyName("characteristic")]
    public string Characteristic { get; set; } = string.Empty;

    /// <summary>
    /// 難易度の名前を取得または設定します。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
