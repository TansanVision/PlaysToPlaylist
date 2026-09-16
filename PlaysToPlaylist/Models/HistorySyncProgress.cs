namespace PlaysToPlaylist.Models;

/// <summary>
/// 履歴同期の進行状況を表す列挙型です。
/// </summary>
public enum HistorySyncStage
{
    /// <summary>
    /// 履歴同期の進行状況がキャッシュを確認中であることを示します。
    /// </summary>
    CheckingCache,
    /// <summary>
    /// 履歴同期の進行状況が保存中であることを示します。
    /// </summary>
    Saving,
    /// <summary>
    /// 履歴同期の進行状況が取得中であることを示します。
    /// </summary>
    Fetching,
    /// <summary>
    /// 履歴同期の進行状況が完了したことを示します。
    /// </summary>
    Completed
}

/// <summary>
/// 履歴同期の進行状況を表すレコードです。
/// </summary>
/// <param name="Stage">履歴同期の進行状況のステージを示します。</param>
/// <param name="CurrentRange">現在の範囲のインデックスを示します。</param>
/// <param name="TotalRanges">総範囲数を示します。</param>
/// <param name="From">取得する履歴の開始日を示します。</param>
/// <param name="To">取得する履歴の終了日を示します。</param>
/// <param name="Fetched">取得済みの項目数を示します。</param>
/// <param name="Saved">保存済みの項目数を示します。</param>
/// <param name="AlreadyExists">既に存在する項目数を示します。</param>
/// <param name="Skipped">スキップされた項目数を示します。</param>
public sealed record HistorySyncProgress
(
    HistorySyncStage Stage,
    int CurrentRange,
    int TotalRanges,
    DateOnly? From,
    DateOnly? To,
    int Fetched,
    int Saved,
    int AlreadyExists,
    int Skipped
);
