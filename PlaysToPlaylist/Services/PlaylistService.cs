using PlaystoPlaylist.Data;
using PlaystoPlaylist.Models;
using PlaysToPlaylist.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace PlaystoPlaylist.Services;

/// <summary>
/// プレイリストの管理に関連するサービスを提供するクラスです。
/// </summary>
public sealed class PlaylistService
{
    /// <summary>
    /// プレイ履歴の管理に関連するサービスを保持するフィールドです。
    /// </summary>
    private readonly HistoryService _historyService;

    /// <summary>
    /// プレイリストのデータを管理するリポジトリを保持するフィールドです。
    /// </summary>
    private readonly PlayRepository _playRepository;

    /// <summary>
    /// アプリケーションのパスを管理するオブジェクトを保持するフィールドです。
    /// </summary>
    private readonly AppPaths _paths;
    private readonly PlaylistImageService _imageService;

    /// <summary>
    /// JSON シリアライズのオプションを保持するフィールドです。
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    /// <summary>
    /// PlaylistService クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="historyService">プレイ履歴の管理に関連するサービス</param>
    /// <param name="playRepository">プレイリストのデータを管理するリポジトリ</param>
    /// <param name="paths">アプリケーションのパスを管理するオブジェクト</param>
    /// <param name="imageService">カバー画像を取得するサービス</param>
    public PlaylistService(
        HistoryService historyService,
        PlayRepository playRepository,
        AppPaths paths,
        PlaylistImageService imageService)
    {
        this._historyService = historyService;
        this._playRepository = playRepository;
        this._paths = paths;
        this._imageService = imageService;
    }

    /// <summary>
    /// 指定されたユーザーのプレイ履歴を基に、指定された期間のプレイリストを作成します。
    /// </summary>
    /// <param name="user">プレイリストを作成する対象のユーザー</param>
    /// <param name="from">プレイ履歴の開始日</param>
    /// <param name="to">プレイ履歴の終了日</param>
    /// <param name="progress">進行状況を報告するコールバック</param>
    /// <param name="cancellationToken">操作のキャンセルを通知するトークン</param>
    /// <returns>作成されたプレイリストの結果</returns>
    public async Task<PlaylistCreateResult> CreateAsync(
        RegisteredUser user,
        DateOnly from,
        DateOnly to,
        Action<HistorySyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException(PlaysToPlaylist.Localization.Texts.Get("InvalidRange"));
        }

        if (to > PlaystoPlaylist.States.ScreenUi.TodayInJapan)
            throw new ArgumentException(PlaysToPlaylist.Localization.Texts.Get("FutureDate"));
        AppPaths.ValidatePlayerId(user.BeatLeaderId);
        var range = CreateUtcRange(from, to);

        var historyResult =
            await _historyService.SyncAsync(
                user,
                range.From,
                range.To,
                progress,
                cancellationToken);

        var songs =
            await _playRepository.GetPlaylistSongsAsync(
                user.Id,
                range.From,
                range.To);

        var image = await _imageService.GetImageAsync(user.AvatarUrl, cancellationToken);
        var playlist =
            new BeatSaberPlaylist
            {
                PlaylistTitle = CreatePlaylistTitle(user, from, to),
                PlaylistAuthor = $"PlaysToPlaylist",
                Image = image,
                Songs = [.. songs.Select(s => new BeatSaberPlaylistSong
                {
                    SongName = s.Name,
                    Hash = s.Hash,
                    Key = s.Key,
                    Difficulties = [.. s.Dificulties.Select(d => new BeatSaberPlaylistDifficulty
                    {
                        Characteristic = d.Characteristic,
                        Name = d.Name.Replace("+", "Plus", StringComparison.Ordinal)
                    })]
                })]
            };

        var json = JsonSerializer.Serialize(playlist, _jsonOptions);

        var path = this._paths.GetPlaylistPath(user.BeatLeaderId, from, to);

        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }

        return new PlaylistCreateResult(
            Path: path,
            SongCount: songs.Length,
            History: historyResult,
            ThumbnailUnavailable: !string.IsNullOrWhiteSpace(user.AvatarUrl) && image is null
        );
    }

    /// <summary>
    /// 指定されたユーザーと日付範囲に基づいて、プレイリストのタイトルを作成します。
    /// </summary>
    /// <param name="user">プレイリストを作成する対象のユーザー</param>
    /// <param name="from">期間の開始日</param>
    /// <param name="to">期間の終了日</param>
    /// <returns>作成されたプレイリストのタイトル</returns>
    private static string CreatePlaylistTitle(
        RegisteredUser user,
        DateOnly from,
        DateOnly to)
    {
        if (from == to)
        {
            return FormattableString.Invariant($"{user.Name} - {from:yyyy-MM-dd}");
        }

        return FormattableString.Invariant($"{user.Name} - {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
    }

    /// <summary>
    /// 指定された日付範囲を UTC に変換し、DateRange オブジェクトを作成します。
    /// </summary>
    /// <param name="from">期間の開始日</param>
    /// <param name="to">期間の終了日</param>
    /// <returns>UTC に変換された日付範囲を表す DateRange オブジェクト</returns>
    private static DateRange CreateUtcRange(
        DateOnly from,
        DateOnly to)
    {
        var japanTimeZone = GetJapanTimeZone();

        var startLocal =
            from.ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Unspecified);

        var endLocal =
            to.AddDays(1)
              .ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Unspecified);

        var startUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                startLocal,
                japanTimeZone);

        var endUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                endLocal,
                japanTimeZone);

        return
            new DateRange(
                startUtc.ToUniversalTime(),
                endUtc.ToUniversalTime());
    }

    /// <summary>
    /// 日本のタイムゾーン情報を取得します。Windows と非 Windows 環境で異なるタイムゾーン ID を使用します。
    /// </summary>
    /// <returns>日本のタイムゾーン情報</returns>
    private static TimeZoneInfo GetJapanTimeZone()
    {
        if (OperatingSystem.IsWindows())
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
        }

        return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
    }

    /// <summary>
    /// 指定された期間を表す DateRange レコードです。
    /// </summary>
    /// <param name="From">期間の開始日時</param>
    /// <param name="To">期間の終了日時</param>
    private sealed record DateRange(DateTimeOffset From, DateTimeOffset To);
}
