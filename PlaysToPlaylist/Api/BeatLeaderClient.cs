using PlaystoPlaylist.Api.Models;
using System.Net;
using System.Net.Http.Json;

namespace PlaystoPlaylist.Api;

/// <summary>
/// BeatLeader API との通信を行うクライアントクラスです。
/// </summary>
public sealed class BeatLeaderClient
{
    /// <summary>
    /// BeatLeader API への HTTP リクエストを送信するための HttpClient インスタンスを保持するフィールドです。
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// BeatLeader API へのリクエストを同期的に制御するための SemaphoreSlim インスタンスを保持するフィールドです。
    /// </summary>
    private readonly SemaphoreSlim _requestLock = new (1, 1);

    /// <summary>
    /// BeatLeader API へのリクエスト間隔を制御するための TimeSpan インスタンスを保持するフィールドです。
    /// </summary>
    private readonly TimeSpan _requestInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// 最後に BeatLeader API へのリクエストが開始された日時を保持するフィールドです。
    /// </summary>
    private DateTimeOffset _lastRequestStartedAt = DateTimeOffset.MinValue;

    /// <summary>
    /// BeatLeader API へのリクエストの最大リトライ回数を定義する定数です。
    /// </summary>
    private const int MaxRetryCount = 3;

    /// <summary>
    /// BeatLeaderClient クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="httpClient">BeatLeader API への HTTP リクエストを送信するための HttpClient インスタンス。</param>
    public BeatLeaderClient(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    /// <summary>
    /// 指定されたプレイヤー ID が BeatLeader API に存在するかどうかを非同期的に確認します。
    /// </summary>
    /// <param name="playerId">確認するプレイヤーの ID。</param>
    /// <param name="cancellationToken">操作をキャンセルするためのトークン。</param>
    /// <returns>指定されたプレイヤー ID が存在する場合は true、存在しない場合は false。</returns>
    public async Task<bool> ExistPlayerByIdAsync(
        string playerId,
        CancellationToken cancellationToken = default)
    {
        var response =
            await this._httpClient.GetAsync(
                $"/player/{Uri.EscapeDataString(playerId)}/exists",
                cancellationToken);

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// 指定されたプレイヤー ID に基づいて BeatLeader API からプレイヤー情報を非同期的に取得します。
    /// </summary>
    /// <param name="playerId">取得するプレイヤーの ID。</param>
    /// <returns>指定されたプレイヤー ID に対応する BeatLeaderPlayer オブジェクト、または取得に失敗した場合は null。</returns>
    public Task<Models.BeatLeaderPlayer?> GetPlayerAsync(
        string playerId,
        CancellationToken cancellationToken = default)
    {
        return
            GetJsonAsync<Models.BeatLeaderPlayer>(
                $"/player/{Uri.EscapeDataString(playerId)}", 
                cancellationToken);
    }

    /// <summary>
    /// 指定されたプレイヤー ID に基づいて BeatLeader API からプレイヤーのスコア情報を非同期的に取得します。
    /// </summary>
    /// <param name="playerId">取得するプレイヤーの ID。</param>
    /// <param name="timeFrom">取得するスコアの開始日時。</param>
    /// <param name="timeTo">取得するスコアの終了日時。</param>
    /// <param name="cancellationToken">操作をキャンセルするためのトークン。</param>
    /// <returns>指定されたプレイヤー ID に対応する PlayerScoresResponse オブジェクト、または取得に失敗した場合は null。</returns>
    public Task<PlayerScoresResponse?>
        GetPlayerScoresAsync(
            string playerId,
            DateTimeOffset timeFrom,
            DateTimeOffset timeTo,
            CancellationToken cancellationToken = default)
    {
        var url =
            $"player/{Uri.EscapeDataString(playerId)}/scores" +
            $"?sortBy=date" +
            $"&order=desc" +
            $"&time_from={timeFrom.ToUnixTimeSeconds()}" +
            $"&time_to={timeTo.ToUnixTimeSeconds()}";

        return 
            GetJsonAsync<PlayerScoresResponse>(
                url, 
                cancellationToken);
    }

    /// <summary>
    /// 指定された URL から JSON データを非同期的に取得し、指定された型にデシリアライズします。
    /// </summary>
    /// <typeparam name="T">デシリアライズする型。</typeparam>
    /// <param name="url">JSON データを取得する URL。</param>
    /// <param name="cancellationToken">操作をキャンセルするためのトークン。</param>
    /// <returns>指定された型にデシリアライズされたオブジェクト、または取得に失敗した場合は null。</returns>
    private async Task<T?> GetJsonAsync<T>(
        string url,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= MaxRetryCount; attempt++)
        {
            using var response = await this.SendGetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt >= MaxRetryCount)
                {
                    response.EnsureSuccessStatusCode();
                }

                var delay =
                    GetRetryDelay(response);

                await
                    Task.Delay(
                        delay,
                        cancellationToken);

                continue;
            }

            response.EnsureSuccessStatusCode();

            return
                await response.Content
                             .ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }

        return default;
    }

    /// <summary>
    /// 指定された URL に対して GET リクエストを送信し、レスポンスを取得します。リクエスト間隔を制御するために、前回のリクエストから一定時間が経過するまで待機します。
    /// </summary>
    /// <param name="url">GET リクエストを送信する URL。</param>
    /// <param name="cancellationToken">操作をキャンセルするためのトークン。</param>
    /// <returns>HTTP レスポンスメッセージ。</returns>
    private async Task<HttpResponseMessage> SendGetAsync(
        string url,
        CancellationToken cancellationToken)
    {
        await this._requestLock.WaitAsync(cancellationToken);

        try
        {
            var elapsed = DateTimeOffset.UtcNow - this._lastRequestStartedAt;
            var remaining = _requestInterval - elapsed;

            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(
                    remaining, 
                    cancellationToken);
            }

            this._lastRequestStartedAt = DateTimeOffset.UtcNow;

            return await this._httpClient.GetAsync(url, cancellationToken);
        }
        finally
        {
            this._requestLock.Release();
        }
    }

    /// <summary>
    /// 指定された HTTP レスポンスメッセージから、リトライの遅延時間を取得します。レスポンスヘッダーの Retry-After ヘッダーが存在する場合は、その値を使用して遅延時間を計算します。存在しない場合は、デフォルトの遅延時間（2秒）を返します。
    /// </summary>
    /// <param name="response">HTTP レスポンスメッセージ。</param>
    /// <returns>リトライの遅延時間。</returns>
    private static TimeSpan GetRetryDelay(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is not null)
        {
            return retryAfter.Delta.Value;
        }

        if (retryAfter?.Date is not null)
        {
            var delay = 
                retryAfter.Date.Value - 
                DateTimeOffset.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                return delay;
            }
        }

        return TimeSpan.FromSeconds(2);
    }
}
