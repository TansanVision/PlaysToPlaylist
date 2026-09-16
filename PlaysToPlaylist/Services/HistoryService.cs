using PlaystoPlaylist.Api;
using PlaystoPlaylist.Api.Models;
using PlaystoPlaylist.Data;
using PlaystoPlaylist.Models;
using PlaysToPlaylist.Data;
using PlaysToPlaylist.Models;

namespace PlaystoPlaylist.Services;

/// <summary>
/// 履歴の同期および管理に関連するサービスを提供するクラスです。
/// </summary>
public sealed class HistoryService
{
    /// <summary>
    /// 履歴のページサイズを定義します。
    /// </summary>
    private const int PageSize = 100;

    /// <summary>
    /// BeatLeader API クライアントを保持するフィールドです。
    /// </summary>
    private readonly BeatLeaderClient _beatLeaderClient;

    /// <summary>
    /// 曲のリポジトリを保持するフィールドです。
    /// </summary>
    private readonly SongRepository _songRepository;

    /// <summary>
    /// ビートマップのリポジトリを保持するフィールドです。
    /// </summary>
    private readonly BeatmapRepository _beatmapRepository;

    /// <summary>
    /// プレイのリポジトリを保持するフィールドです。
    /// </summary>
    private readonly PlayRepository _playRepository;

    /// <summary>
    /// 履歴キャッシュのリポジトリを保持するフィールドです。
    /// </summary>
    private readonly HistoryCacheRepository _historyCacheRepository;

    /// <summary>
    /// コンストラクタです。必要な依存関係を注入します。
    /// </summary>
    /// <param name="beatLeaderClient">BeatLeader API クライアント</param>
    /// <param name="songRepository">曲のリポジトリ</param>
    /// <param name="beatmapRepository">ビートマップのリポジトリ</param>
    /// <param name="playRepository">プレイのリポジトリ</param>
    /// <param name="historyCacheRepository">履歴キャッシュのリポジトリ</param>
    public HistoryService(
        BeatLeaderClient beatLeaderClient,
        SongRepository songRepository,
        BeatmapRepository beatmapRepository,
        PlayRepository playRepository,
        HistoryCacheRepository historyCacheRepository)
    {
        _beatLeaderClient = beatLeaderClient;
        _songRepository = songRepository;
        _beatmapRepository = beatmapRepository;
        _playRepository = playRepository;
        _historyCacheRepository = historyCacheRepository;
    }

    /// <summary>
    /// 指定されたユーザーの履歴を同期します。指定された期間内で、キャッシュされていない日付の履歴を取得し、データベースに保存します。進捗状況はコールバック関数を通じて報告されます。
    /// </summary>
    /// <param name="user">同期対象のユーザー</param>
    /// <param name="from">同期開始日時</param>
    /// <param name="to">同期終了日時</param>
    /// <param name="progress">進捗状況を報告するコールバック関数</param>
    /// <param name="cancellationToken">操作をキャンセルするためのトークン</param>
    /// <returns>同期結果を表す HistorySyncResult オブジェクト</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<HistorySyncResult> SyncAsync(
        RegisteredUser user,
        DateTimeOffset from,
        DateTimeOffset to,
        Action<HistorySyncProgress> progress,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException("from must be before to");
        }

        var fromDate = GetJapanDate(from);
        var toDate = GetJapanDate(to.AddTicks(-1));
        var requestedDays = toDate.DayNumber - fromDate.DayNumber + 1;

        progress?.Invoke(
            new HistorySyncProgress(
            HistorySyncStage.CheckingCache,
            CurrentRange: 0,
            TotalRanges: 0,
            From: fromDate,
            To: toDate,
            Fetched: 0,
            Saved: 0,
            AlreadyExists: 0,
            Skipped: 0
        ));

        var completedDates = await _historyCacheRepository.GetCompletedDatesAsync(
            user.Id,
            fromDate,
            toDate);

        var missingDates = CreateMissingRanges(
            fromDate,
            toDate,
            completedDates);

        if (missingDates.Length == 0)
        {
            return new HistorySyncResult(
                RequestedDays: requestedDays,
                CachedDays: requestedDays,
                DownloadedRanges: 0,
                Fetched: 0,
                Saved: 0,
                AlreadyExists: 0,
                Skipped: 0
            );
        }

        var fetched = 0;
        var saved = 0;
        var alreadyExists = 0;
        var skipped = 0;

        for (var i = 0; i < missingDates.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var range = missingDates[i];
            var apiFrom = ToUtcStartOfJapanDate(range.From);
            var apiTo = ToUtcStartOfJapanDate(range.To.AddDays(1));

            progress?.Invoke(
                new HistorySyncProgress(
                    HistorySyncStage.Fetching,
                    CurrentRange: i + 1,
                    TotalRanges: missingDates.Length,
                    From: range.From,
                    To: range.To,
                    Fetched: fetched,
                    Saved: saved,
                    AlreadyExists: alreadyExists,
                    Skipped: skipped
                ));

            var response = await _beatLeaderClient.GetPlayerScoresAsync(
                user.BeatLeaderId,
                apiFrom,
                apiTo,
                cancellationToken: cancellationToken);

            if (response is null)
            {
                continue;
            }

            foreach (var score in response.Data)
            {
                cancellationToken.ThrowIfCancellationRequested();

                fetched++;

                if (score.Timepost <= 0)
                {
                    skipped++;
                    continue;
                }

                var playedAt = DateTimeOffset.FromUnixTimeSeconds(score.Timepost);

                if (playedAt < apiFrom || playedAt >= apiTo)
                {
                    continue;
                }

                progress?.Invoke(
                    new HistorySyncProgress(
                        HistorySyncStage.Saving,
                        CurrentRange: i + 1,
                        TotalRanges: missingDates.Length,
                        From: range.From,
                        To: range.To,
                        Fetched: fetched,
                        Saved: saved,
                        AlreadyExists: alreadyExists,
                        Skipped: skipped
                    ));

                var result = await SaveScoreAsync(
                    user,
                    score,
                    playedAt);

                switch (result)
                {
                    case SaveScoreResult.Saved:
                        saved++;
                        break;
                    case SaveScoreResult.AlreadyExists:
                        alreadyExists++;
                        break;
                    case SaveScoreResult.Skipped:
                        skipped++;
                        break;
                }
            }

            foreach (var date in EnumerableDates(range.From, range.To))
            {
                await _historyCacheRepository.MarkCompleteAsync(
                    user.Id,
                    date);
            }
        }

        progress?.Invoke(
            new HistorySyncProgress(
                HistorySyncStage.Completed,
                CurrentRange: missingDates.Length,
                TotalRanges: missingDates.Length,
                From: fromDate,
                To: toDate,
                Fetched: fetched,
                Saved: saved,
                AlreadyExists: alreadyExists,
                Skipped: skipped
            ));

        return new HistorySyncResult(
            RequestedDays: requestedDays,
            CachedDays: completedDates.Count,
            DownloadedRanges: missingDates.Length,
            Fetched: fetched,
            Saved: saved,
            AlreadyExists: alreadyExists,
            Skipped: skipped
        );
    }

    /// <summary>
    /// 指定された日付範囲内で、完了していない日付の範囲を作成します。
    /// </summary>
    /// <param name="from">開始日</param>
    /// <param name="to">終了日</param>
    /// <param name="completedDates">完了した日付の集合</param>
    /// <returns>完了していない日付の範囲の配列</returns>
    private static HistoryDateRange[] CreateMissingRanges(
        DateOnly from,
        DateOnly to,
        HashSet<DateOnly> completedDates)
    {
        var result = new List<HistoryDateRange>();

        DateOnly? rangeStart = null;

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var complete = completedDates.Contains(date);

            if (!complete)
            {
                if (rangeStart is null)
                {
                    rangeStart ??= date;
                }

                continue;
            }

            if (rangeStart is not null)
            {
                result.Add(new HistoryDateRange(
                    rangeStart.Value,
                    date.AddDays(-1)
                ));

                rangeStart = null;
            }
        }

        if (rangeStart is not null)
        {
            result.Add(new HistoryDateRange(
                rangeStart.Value,
                to
            ));
        }

        return result.ToArray();
    }

    /// <summary>
    /// 指定された日付範囲内のすべての日付を列挙します。
    /// </summary>
    /// <param name="from">開始日</param>
    /// <param name="to">終了日</param>
    /// <returns>日付の列挙</returns>
    private static IEnumerable<DateOnly> EnumerableDates(DateOnly from, DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    /// <summary>
    /// 指定されたスコアをデータベースに保存します。スコアが既に存在する場合や、無効なスコアの場合は適切な結果を返します。
    /// </summary>
    /// <param name="user">スコアを保存するユーザー</param>
    /// <param name="score">保存するスコア</param>
    /// <param name="playedAt">スコアが記録された日時</param>
    /// <returns>スコアの保存結果</returns>
    private async Task<SaveScoreResult> SaveScoreAsync(
        RegisteredUser user,
        BeatLeaderScore score,
        DateTimeOffset playedAt)
    {
        if (score.Id <= 0)
        {
            return SaveScoreResult.Skipped;
        }

        var scoreId = score.Id.ToString();

        if (await _playRepository.ExistsAsync(scoreId))
        {
            return SaveScoreResult.AlreadyExists;
        }

        var leaderboard = score.Leaderboard;

        var song = leaderboard?.Song;

        var difficulty = leaderboard?.Difficulty;

        if (leaderboard is null ||
            difficulty is null ||
            song is null)
        {
            return SaveScoreResult.Skipped;
        }

        if (string.IsNullOrWhiteSpace(song.Hash))
        {
            return SaveScoreResult.Skipped;
        }

        var songId =
            await _songRepository.GetOrCreateAsync(
                song.Hash.Trim().ToUpperInvariant(),
                song.Id,
                song.Name,
                song.SubName);

        var beatmapId =
            await _beatmapRepository.GetOrCreateAsync(
                songId: songId,
                leaderBoardId: leaderboard.Id,
                difficultyName: difficulty.DifficultyName,
                difficultyValue: difficulty.Value,
                modeName: difficulty.ModeName);

        await this._playRepository.AddAsync(
            beatLeaderScoreId: scoreId,
            userId: user.Id,
            beatmapId: beatmapId,
            playedAt: playedAt,
            baseScore: score.BaseScore,
            modifiedScore: score.ModifiedScore,
            accuracy: score.Accuracy,
            pp: score.Pp,
            maxCombo: score.MaxCombo,
            fullCombo: score.FullCombo,
            badCuts: score.BadCuts,
            missedNotes: score.MissedNotes,
            bombCuts: score.BombCuts,
            wallsHit: score.WallsHit,
            modifiers: score.Modifiers);

        return SaveScoreResult.Saved;
    }

    /// <summary>
    /// 指定された DateTimeOffset を日本時間に変換し、DateOnly として返します。
    /// </summary>
    /// <param name="value">変換する DateTimeOffset</param>
    /// <returns>日本時間に変換された DateOnly</returns>
    private static DateOnly GetJapanDate(DateTimeOffset value)
    {
        var japanTimeZone = TimeZoneInfo.ConvertTime(value, GetJapanTimeZone());
        return DateOnly.FromDateTime(japanTimeZone.DateTime);
    }

    /// <summary>
    /// 指定された日付を日本時間の開始時刻に変換し、UTC に変換します。
    /// </summary>
    /// <param name="date">変換する日付</param>
    /// <returns>UTC に変換された日本時間の開始時刻</returns>
    private static DateTimeOffset ToUtcStartOfJapanDate(DateOnly date)
    {
        var timeZone = GetJapanTimeZone();
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var offset = timeZone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    /// <summary>
    /// 日本のタイムゾーン情報を取得します。Windows とそれ以外の OS で異なるタイムゾーン ID を使用します。
    /// </summary>
    /// <returns>日本のタイムゾーン情報</returns>
    private static TimeZoneInfo GetJapanTimeZone()
    {
        if (OperatingSystem.IsWindows())
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
        }
        else
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
        }
    }

    /// <summary>
    /// 保存結果を表す列挙型です。
    /// </summary>
    private enum SaveScoreResult
    {
        /// <summary>
        /// スコアが正常に保存されたことを示します。
        /// </summary>
        Saved,
        /// <summary>
        /// スコアが既に存在していたため、保存がスキップされたことを示します。
        /// </summary>
        AlreadyExists,
        /// <summary>
        /// スコアが無効または不完全であったため、保存がスキップされたことを示します。
        /// </summary>
        Skipped
    }
}
