namespace PlaysToPlaylist.Models;

/// <summary>
/// 履歴同期の結果を表すレコードです。
/// </summary>
/// <param name="RequestedDays">要求された日数を示します。</param>
/// <param name="CachedDays">キャッシュされた日数を示します。</param>
/// <param name="DownloadedRanges">ダウンロードされた範囲の数を示します。</param>
/// <param name="Fetched">取得済みの項目数を示します。</param>
/// <param name="Saved">保存済みの項目数を示します。</param>
/// <param name="AlreadyExists">既に存在する項目数を示します。</param>
/// <param name="Skipped">スキップされた項目数を示します。</param>
public sealed record HistorySyncResult(
    int RequestedDays,
    int CachedDays,
    int DownloadedRanges,
    int Fetched,
    int Saved,
    int AlreadyExists,
    int Skipped
);
