namespace PlaysToPlaylist.Models;

/// <summary>
/// プレイリスト作成結果を表すレコードです。
/// </summary>
/// <param name="Path">作成されたプレイリストのファイルパス</param>
/// <param name="SongCount">プレイリストに含まれる曲の数</param>
/// <param name="History">履歴同期の結果</param>
/// <param name="ThumbnailUnavailable">設定されたアバター画像を取得できなかったかどうか</param>
public sealed record PlaylistCreateResult(
    string Path,
    int SongCount,
    HistorySyncResult History,
    bool ThumbnailUnavailable = false);
