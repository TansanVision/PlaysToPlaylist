using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Globalization;
using Microsoft.Data.Sqlite;
using PlaystoPlaylist.Api;
using PlaystoPlaylist.Api.Models;
using PlaystoPlaylist.Data;
using PlaystoPlaylist.Models;
using PlaystoPlaylist.Services;
using PlaysToPlaylist.Data;
using PlaysToPlaylist.Localization;
using PlaystoPlaylist.States;

var tests = new (string Name, Func<Task> Run)[]
{
    ("User registration, canonical ID, profile update and duplicate detection", Users),
    ("Full pagination, cache reuse, grouped playlist and date boundaries", Pagination),
    ("Incomplete sync is retryable and preserves existing output", FailedSync),
    ("Current-day scores are fetched again", Today),
    ("Null database values and user deletion/re-registration", DeleteUser),
    ("Legacy partial cache invalidation runs once", Migration),
    ("Malformed response, cancellation and skipped score recovery", InvalidResponses),
    ("Both resource sets, culture independence and unsafe IDs", Localization),
    ("HTTP not-found, error and rate-limit behavior", HttpErrors),
    ("Non-Gregorian culture, invalid ranges and repeat pages", EdgeCases),
    ("Selected user's avatar is embedded and refreshed on export", ThumbnailEmbedding),
    ("Missing, invalid and unavailable thumbnails preserve playlist creation", ThumbnailFallback),
    ("Thumbnail download limits, formats and cancellation", ThumbnailLimits)
};
var failed = 0;
foreach (var test in tests)
{
    try { await test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception ex) { failed++; Console.Error.WriteLine($"FAIL {test.Name}: {ex}"); }
}
Console.WriteLine($"{tests.Length - failed}/{tests.Length} passed");
return failed == 0 ? 0 : 1;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}
static DateTimeOffset Start(DateOnly day) => new(day.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(9));
static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
static BeatLeaderScore Score(long id, DateTimeOffset at, string hash = "ABC", string difficulty = "Expert+") => new()
{
    Id = id, Timepost = at.ToUnixTimeSeconds(), Modifiers = null,
    Leaderboard = new() { Id = hash + difficulty, Song = new() { Id = "123", Hash = hash, Name = "Song", SubName = "Subtitle" },
        Difficulty = new() { DifficultyName = difficulty, ModeName = "Standard", Value = 9 } }
};
static HttpResponseMessage Page(HttpRequestMessage request, List<BeatLeaderScore> scores)
{
    var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query);
    var page = int.Parse(query["page"]!);
    var count = int.Parse(query["count"]!);
    Check(count == 100 && query["sortBy"] == "date", "Missing pagination query");
    return Json(new PlayerScoresResponse { Data = scores.Skip((page - 1) * count).Take(count).ToList(),
        Metadata = new() { Page = page, ItemsPerPage = count, Total = scores.Count } });
}
static async Task Users()
{
    using var f = await Fixture.Create();
    f.Handler.Respond = _ => Json(new BeatLeaderPlayer { Id = "12345", Name = "[red]旧名[/]", Avatar = null });
    var user = await f.Users.AddUserAsync(" alias ");
    Check(user.BeatLeaderId == "12345" && user.Id > 0 && user.AvatarUrl is null && user.CreatedAt != default, "User mapping failed");
    await Throws<InvalidOperationException>(() => f.Users.AddUserAsync("alias"));
    f.Handler.Respond = _ => Json(new BeatLeaderPlayer { Id = "12345", Name = "Updated", Avatar = "new.png" });
    var updated = await f.Users.UpdateUserAsync(user);
    Check(updated.Name == "Updated" && updated.AvatarUrl == "new.png", "Profile stayed stale");
    Check(!await f.Users.ExistsUserByIdAsync(user.Id.ToString()), "Local ID treated as BeatLeader ID");
}
static async Task Pagination()
{
    using var f = await Fixture.Create();
    var user = await f.Register();
    var date = new DateOnly(2025, 1, 2);
    var start = Start(date);
    var scores = Enumerable.Range(1, 101).Select(i => Score(i, start.AddSeconds(i))).ToList();
    scores[0] = Score(1, start);
    scores[1] = Score(2, start.AddSeconds(1), difficulty: "Hard");
    scores.Add(Score(102, start.AddDays(1), "OUTSIDE"));
    f.Handler.Respond = r => Page(r, scores);
    var result = await f.Playlist.CreateAsync(user, date, date);
    Check(result.History.Fetched == 102 && result.History.Saved == 101 && f.Handler.Calls == 2, "Lost scores on later pages");
    using var json = JsonDocument.Parse(await File.ReadAllTextAsync(result.Path));
    var songs = json.RootElement.GetProperty("songs");
    Check(songs.GetArrayLength() == 1, "Duplicate songs or included next day");
    Check(songs[0].GetProperty("songName").GetString() == "Song Subtitle", "Incorrect song name");
    Check(songs[0].GetProperty("difficulties").GetArrayLength() == 2, "Missing difficulties");
    Check(songs[0].GetProperty("difficulties").EnumerateArray().Any(d => d.GetProperty("name").GetString() == "ExpertPlus"), "Expert+ was not normalized");
    Check(await f.Cache.IsCompleteAsync(user.Id.ToString(), date.ToDateTime(TimeOnly.MinValue)), "Cache conversion failed");
    // Include a pre-cached score exactly at the next midnight to test SQL bounds independently of API filtering.
    await f.History.SyncAsync(user, start.AddDays(1), start.AddDays(2), null);
    var cached = await f.Playlist.CreateAsync(user, date, date);
    Check(cached.History.DownloadedRanges == 0 && cached.SongCount == 1, "Cache or SQL date boundary failed");
    var stored = await f.Plays.GetPlaylistSongsAsync(user.Id, start, start.AddDays(1));
    Check(stored.Length == 1, "SQL includes next day");
}
static async Task FailedSync()
{
    using var f = await Fixture.Create();
    var user = await f.Register();
    var date = new DateOnly(2025, 2, 1);
    var scores = Enumerable.Range(1, 101).Select(i => Score(i, Start(date).AddSeconds(i))).ToList();
    var path = f.Paths.GetPlaylistPath(user.BeatLeaderId, date, date);
    await File.WriteAllTextAsync(path, "existing playlist");
    f.Handler.Respond = r => r.RequestUri!.Query.Contains("page=2") ? new(HttpStatusCode.ServiceUnavailable) : Page(r, scores);
    await Throws<HttpRequestException>(() => f.Playlist.CreateAsync(user, date, date));
    Check((await f.Cache.GetCompletedDatesAsync(user.Id, date, date)).Count == 0, "Failed range cached");
    Check(await File.ReadAllTextAsync(path) == "existing playlist", "Existing file destroyed");
    f.Handler.Respond = r => Page(r, scores);
    var result = await f.Playlist.CreateAsync(user, date, date);
    Check(result.History.Saved == 1 && result.History.AlreadyExists == 100, "Retry did not resume correctly");
}
static async Task Today()
{
    using var f = await Fixture.Create();
    var user = await f.Register();
    var today = ScreenUi.TodayInJapan;
    var scores = new List<BeatLeaderScore> { Score(1, Start(today)) };
    f.Handler.Respond = r => Page(r, scores);
    await f.Playlist.CreateAsync(user, today, today);
    await f.Cache.MarkCompleteAsync(user.Id, today); // old application's stale marker
    scores.Add(Score(2, Start(today).AddSeconds(1), "SECOND"));
    var result = await f.Playlist.CreateAsync(user, today, today);
    Check(result.SongCount == 2 && result.History.Saved == 1, "Today was cached permanently");
}
static async Task DeleteUser()
{
    using var f = await Fixture.Create();
    var user = await f.Register();
    var date = new DateOnly(2025, 3, 1);
    var song = await f.Songs.GetOrCreateAsync("ABC", null, "Title", null);
    var map = await f.Maps.GetOrCreateAsync(song, "map", "Hard", "Standard", null);
    await f.Plays.AddAsync("1", user.Id, map, 0, 0, 0, null, 0, false, null, null, 0, 0, null, Start(date));
    var songs = await f.Plays.GetPlaylistSongsAsync(user.Id, Start(date), Start(date).AddDays(1));
    Check(songs.Length == 1 && songs[0].Key is null && songs[0].Name == "Title", "NULL columns broke reading");
    await f.Cache.MarkCompleteAsync(user.Id, date);
    await f.Users.RemoveAsync(user);
    Check(!await f.Plays.ExistsAsync("1") && (await f.Cache.GetCompletedDatesAsync(user.Id, date, date)).Count == 0, "Orphan cache left behind");
    var again = await f.Register();
    Check(again.Id != user.Id, "Registration failed after deletion");
    f.Handler.Respond = r => Page(r, [Score(1, Start(date))]);
    var result = await f.Playlist.CreateAsync(again, date, date);
    Check(result.SongCount == 1 && result.History.Saved == 1, "Old score ID blocked re-registration");
}
static async Task Migration()
{
    using var f = await Fixture.Create();
    var user = await f.Register();
    var date = new DateOnly(2025, 4, 1);
    await f.Cache.MarkCompleteAsync(user.Id, date);
    await f.Database.ExecuteTransactionAsync((c, t) => { using var command = c.CreateCommand(); command.CommandText = "PRAGMA user_version = 0;"; command.ExecuteNonQuery(); }, true);
    await new DatabaseInitializer(f.Database).InitializeAsync();
    Check((await f.Cache.GetCompletedDatesAsync(user.Id, date, date)).Count == 0, "Legacy cache not cleared");
    await f.Cache.MarkCompleteAsync(user.Id, date);
    await new DatabaseInitializer(f.Database).InitializeAsync();
    Check((await f.Cache.GetCompletedDatesAsync(user.Id, date, date)).Count == 1, "Migration repeats");
}
static async Task InvalidResponses()
{
    using var f = await Fixture.Create();
    var user = await f.Register();
    var day = new DateOnly(2025, 5, 1);
    f.Handler.Respond = _ => Json(new { metadata = new { page = 1, itemsPerPage = 100, total = 1 } });
    await Throws<InvalidDataException>(() => f.Playlist.CreateAsync(user, day, day));
    f.Handler.Respond = _ => Json(new { metadata = new { page = 1, itemsPerPage = 100, total = 0 } });
    var emptyDay = day.AddDays(-1);
    Check((await f.Playlist.CreateAsync(user, emptyDay, emptyDay)).SongCount == 0, "Empty API response rejected");
    f.Handler.Respond = _ => new(HttpStatusCode.NotFound);
    await Throws<InvalidDataException>(() => f.Playlist.CreateAsync(user, day, day));
    using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
    await Throws<OperationCanceledException>(() => f.Playlist.CreateAsync(user, day, day, cancellationToken: cancelled.Token));
    var score = Score(1, Start(day)); score.Leaderboard = null;
    f.Handler.Respond = r => Page(r, [score]);
    var result = await f.Playlist.CreateAsync(user, day, day);
    Check(result.History.Skipped == 1 && (await f.Cache.GetCompletedDatesAsync(user.Id, day, day)).Count == 0, "Skipped data permanently cached");
    f.Handler.Respond = r => Page(r, [Score(1, Start(day))]);
    Check((await f.Playlist.CreateAsync(user, day, day)).SongCount == 1, "Skipped score cannot recover");
}
static Task Localization()
{
    var assembly = typeof(Texts).Assembly;
    Dictionary<string, string> Load(string language)
    {
        using var stream = assembly.GetManifestResourceStream($"PlaysToPlaylist.Localization.{language}.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
    var en = Load("en"); var ja = Load("ja");
    Check(en.Keys.Order().SequenceEqual(ja.Keys.Order()), "Translation keys differ");
    foreach (var lang in new[] { "en", "ja" })
    {
        Texts.SetLanguage(lang);
        foreach (var key in en.Keys) Check(!string.IsNullOrWhiteSpace(Texts.Get(key, 1, 2, 3, 4, 5, 6, 7)), "Missing text");
    }
    Check(Texts.Get("Back") == "戻る", "Japanese lookup failed");
    Texts.SetLanguage("en"); Check(Texts.Get("Back") == "Back", "English lookup failed");
    foreach (var id in new[] { "../bad", "C:\\bad", "CON", "a/b", "", "https://beatleader.com/u/1" })
    {
        try { AppPaths.ValidatePlayerId(id); throw new Exception("Unsafe ID accepted"); }
        catch (ArgumentException) { }
    }
    return Task.CompletedTask;
}
static async Task HttpErrors()
{
    using var f = await Fixture.Create();
    f.Handler.Respond = _ => new(HttpStatusCode.NotFound);
    Check(!await f.Client.ExistPlayerByIdAsync("missing"), "404 treated as existing");
    f.Handler.Respond = _ => new(HttpStatusCode.InternalServerError);
    await Throws<HttpRequestException>(() => f.Client.ExistPlayerByIdAsync("error"));
    var count = 0;
    f.Handler.Respond = _ =>
    {
        if (++count > 1) return Json(new BeatLeaderPlayer { Id = "123" });
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new(TimeSpan.Zero); return response;
    };
    Check((await f.Client.GetPlayerAsync("123"))?.Id == "123" && count == 2, "429 not retried");
}

static async Task EdgeCases()
{
    using var f = await Fixture.Create();
    var user = await f.Register();
    var day = new DateOnly(2025, 6, 1);
    var oldCulture = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
        Check(Path.GetFileName(f.Paths.GetPlaylistPath(user.BeatLeaderId, day, day)) == "2025-06-01.bplist", "Filename used a different calendar");
        await f.Cache.MarkCompleteAsync(user.Id, day);
        Check((await f.Cache.GetCompletedDatesAsync(user.Id, day, day)).Contains(day), "Culture changed cache dates");
    }
    finally { CultureInfo.CurrentCulture = oldCulture; }
    await Throws<ArgumentException>(() => f.Playlist.CreateAsync(user, day.AddDays(1), day));
    await Throws<ArgumentException>(() => f.Playlist.CreateAsync(user, ScreenUi.TodayInJapan, ScreenUi.TodayInJapan.AddDays(1)));
    var nextDay = day.AddDays(1);
    f.Handler.Respond = r =>
    {
        var page = int.Parse(System.Web.HttpUtility.ParseQueryString(r.RequestUri!.Query)["page"]!);
        return Json(new PlayerScoresResponse { Data = [Score(1, Start(nextDay))], Metadata = new() { Page = page, ItemsPerPage = 1, Total = 2 } });
    };
    await Throws<InvalidDataException>(() => f.Playlist.CreateAsync(user, nextDay, nextDay));
    Check((await f.Cache.GetCompletedDatesAsync(user.Id, nextDay, nextDay)).Count == 0, "Repeated page was cached");
}
static byte[] Png() => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a5FoAAAAASUVORK5CYII=");

static HttpResponseMessage ImageResponse(byte[] bytes) => new(HttpStatusCode.OK)
{
    Content = new ByteArrayContent(bytes)
};

static async Task ThumbnailEmbedding()
{
    using var f = await Fixture.Create();
    var user = await f.Register();
    user.AvatarUrl = "https://avatars.example.invalid/first";
    var date = new DateOnly(2025, 7, 1);
    var requests = new List<string>();
    var bytes = Png();
    f.Handler.Respond = r =>
    {
        if (r.RequestUri!.Host != "avatars.example.invalid") return Page(r, [Score(1, Start(date))]);
        requests.Add(r.RequestUri.AbsoluteUri);
        return ImageResponse(bytes); // No MIME type or file extension required.
    };
    var result = await f.Playlist.CreateAsync(user, date, date);
    using var json = JsonDocument.Parse(await File.ReadAllTextAsync(result.Path));
    var embedded = json.RootElement.GetProperty("image").GetString()!;
    Check(embedded == "data:image/png;base64," + Convert.ToBase64String(bytes), "PNG bytes not embedded in image field");
    Check(!result.ThumbnailUnavailable && result.SongCount == 1, "Cover affected song export");

    user.AvatarUrl = "https://avatars.example.invalid/updated";
    await f.Playlist.CreateAsync(user, date, date);
    await f.UserRepository.AddAsync("67890", null, "Second user", "https://avatars.example.invalid/second");
    var second = (await f.UserRepository.FindByBeatLeaderIdAsync("67890"))!;
    await f.Cache.MarkCompleteAsync(second.Id, date);
    await f.Playlist.CreateAsync(second, date, date);
    Check(requests.SequenceEqual(new[] { "https://avatars.example.invalid/first", "https://avatars.example.invalid/updated", "https://avatars.example.invalid/second" }), "Used wrong user's or stale avatar URL");
}

static async Task ThumbnailFallback()
{
    using var f = await Fixture.Create();
    var user = await f.Register();
    var date = new DateOnly(2025, 7, 2);
    f.Handler.Respond = r => Page(r, [Score(1, Start(date))]);
    var result = await f.Playlist.CreateAsync(user, date, date);
    using (var json = JsonDocument.Parse(await File.ReadAllTextAsync(result.Path)))
        Check(!json.RootElement.TryGetProperty("image", out _) && !result.ThumbnailUnavailable, "Missing avatar should be omitted without warning");

    user.AvatarUrl = "https://avatars.example.invalid/unavailable";
    Func<HttpRequestMessage, HttpResponseMessage>[] failures =
    [
        _ => new(HttpStatusCode.NotFound),
        _ => throw new HttpRequestException("Offline"),
        _ => throw new TaskCanceledException("Timeout"),
        _ => ImageResponse("<html>error</html>"u8.ToArray()),
        _ => ImageResponse([]),
        _ => ImageResponse(new byte[PlaylistImageService.MaxImageBytes + 1])
    ];
    foreach (var failure in failures)
    {
        f.Handler.Respond = failure;
        result = await f.Playlist.CreateAsync(user, date, date);
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(result.Path));
        Check(result.SongCount == 1 && result.ThumbnailUnavailable && !json.RootElement.TryGetProperty("image", out _), "Image failure broke export or stored invalid data");
    }
}

static async Task ThumbnailLimits()
{
    var handler = new FakeHandler();
    using var http = new HttpClient(handler);
    var service = new PlaylistImageService(http);
    foreach (var url in new string?[] { null, "", "invalid", "file:///C:/avatar.png", "ftp://example.invalid/a.png" })
        Check(await service.GetImageAsync(url) is null, "Invalid URL accepted");
    Check(handler.Calls == 0, "Invalid URL triggered a request");
    // Test JPEG format detection and exact byte preservation independently of image decoding.
    byte[] jpeg = [255, 216, 255, 224, 0, 2, 255, 217];
    handler.Respond = _ => ImageResponse(jpeg);
    Check(await service.GetImageAsync("https://example.invalid/avatar") == "data:image/jpeg;base64," + Convert.ToBase64String(jpeg), "JPEG MIME detection failed");
    handler.Respond = _ => new(HttpStatusCode.OK)
    {
        Content = new StreamContent(new NonSeekableMemoryStream(new byte[PlaylistImageService.MaxImageBytes + 1]))
    };
    Check(await service.GetImageAsync("https://example.invalid/avatar") is null, "Unknown-length image exceeded the limit");

    using var f = await Fixture.Create();
    var user = await f.Register();
    var date = new DateOnly(2025, 7, 3);
    await f.Cache.MarkCompleteAsync(user.Id, date);
    user.AvatarUrl = "https://example.invalid/avatar";
    var path = f.Paths.GetPlaylistPath(user.BeatLeaderId, date, date);
    await File.WriteAllTextAsync(path, "previous playlist");
    using var cancelled = new CancellationTokenSource();
    f.Handler.Respond = _ => { cancelled.Cancel(); throw new OperationCanceledException(cancelled.Token); };
    await Throws<OperationCanceledException>(() => f.Playlist.CreateAsync(user, date, date, cancellationToken: cancelled.Token));
    Check(await File.ReadAllTextAsync(path) == "previous playlist", "Cancellation overwrote existing export");
}

sealed class NonSeekableMemoryStream(byte[] buffer) : MemoryStream(buffer)
{
    public override bool CanSeek => false;
}

sealed class FakeHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => throw new Exception("Unexpected request");
    public int Calls { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); Calls++;
        return Task.FromResult(Respond(request));
    }
}
sealed class Fixture : IDisposable
{
    public AppPaths Paths { get; }
    public Database Database { get; }
    public UserRepository UserRepository { get; }
    public SongRepository Songs { get; }
    public BeatmapRepository Maps { get; }
    public PlayRepository Plays { get; }
    public HistoryCacheRepository Cache { get; }
    public FakeHandler Handler { get; } = new();
    private readonly HttpClient http;
    public BeatLeaderClient Client { get; }
    public UserService Users { get; }
    public HistoryService History { get; }
    public PlaylistService Playlist { get; }
    private Fixture()
    {
        Paths = new AppPaths(Path.Combine(Path.GetTempPath(), "PlaysToPlaylist-tests", Guid.NewGuid().ToString("N")));
        Database = new(Paths); UserRepository = new(Database); Songs = new(Database); Maps = new(Database); Plays = new(Database); Cache = new(Database);
        http = new(Handler) { BaseAddress = new Uri("https://example.invalid/") }; Client = new(http);
        Users = new(Client, UserRepository); History = new(Client, Songs, Maps, Plays, Cache); Playlist = new(History, Plays, Paths, new PlaylistImageService(http));
    }
    public static async Task<Fixture> Create() { var f = new Fixture(); await new DatabaseInitializer(f.Database).InitializeAsync(); return f; }
    public async Task<RegisteredUser> Register()
    {
        await UserRepository.AddAsync("12345", null, "Test User", null);
        return (await UserRepository.FindByBeatLeaderIdAsync("12345"))!;
    }
    public void Dispose()
    {
        http.Dispose(); SqliteConnection.ClearAllPools();
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "PlaysToPlaylist-tests")) + Path.DirectorySeparatorChar;
        var directory = Path.GetFullPath(Paths.DataDirectory);
        if (!directory.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unexpected test path");
        Directory.Delete(directory, recursive: true);
    }
}
