namespace PlaysToPlaylist.Models;

/// <summary>
/// 履歴の日付範囲を表すレコードです。開始日と終了日を保持します。
/// </summary>
/// <param name="From">開始日</param>
/// <param name="To">終了日</param>
public sealed record HistoryDateRange(DateOnly From, DateOnly To);
