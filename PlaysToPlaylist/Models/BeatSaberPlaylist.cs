using System.Text.Json.Serialization;

namespace PlaysToPlaylist.Models;

/// <summary>
/// Beat Saber プレイリストを表すクラスです。
/// </summary>
public sealed class BeatSaberPlaylist
{
    /// <summary>
    /// プレイリストのタイトルを取得または設定します。
    /// </summary>
    [JsonPropertyName("playlistTitle")]
    public string PlaylistTitle { get; set; } = string.Empty;

    /// <summary>
    /// プレイリストの作成者を取得または設定します。
    /// </summary>
    [JsonPropertyName("playlistAuthor")]
    public string PlaylistAuthor { get; set; } = string.Empty;

    /// <summary>
    /// プレイリストの曲の配列を取得または設定します。
    /// </summary>
    [JsonPropertyName("songs")]
    public BeatSaberPlaylistSong[] Songs { get; set; } = Array.Empty<BeatSaberPlaylistSong>();
}

/// <summary>
/// Beat Saber プレイリストの曲を表すクラスです。
/// </summary>
public sealed class BeatSaberPlaylistSong
{
    /// <summary>
    /// 曲の名前を取得または設定します。
    /// </summary>
    [JsonPropertyName("songName")]
    public string SongName { get; set; } = string.Empty;

    /// <summary>
    /// 曲の作者の名前を取得または設定します。
    /// </summary>
    [JsonPropertyName("songAuthorName")]
    public string? SongAuthorName { get; set; }

    /// <summary>
    /// 曲のレベル作者の名前を取得または設定します。
    /// </summary>
    [JsonPropertyName("levelAuthorName")]
    public string? LevelAuthorName { get; set; }

    /// <summary>
    /// 曲のキーを取得または設定します。
    /// </summary>
    [JsonPropertyName("key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Key { get; set; }

    /// <summary>
    /// 曲のハッシュを取得または設定します。
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 曲の難易度の配列を取得または設定します。
    /// </summary>
    [JsonPropertyName("difficulties")]
    public BeatSaberPlaylistDifficulty[] Difficulties { get; set; } = [];
}
