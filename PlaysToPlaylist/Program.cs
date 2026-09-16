using PlaystoPlaylist.Api;
using PlaystoPlaylist.Data;
using PlaystoPlaylist.Services;
using PlaystoPlaylist.States;
using PlaysToPlaylist.Data;
using PlaysToPlaylist.Localization;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Texts.SetLanguage(Texts.Language);
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
try
{
    string? language = null;
    var help = false;
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] is "--help" or "-h") help = true;
        else if (args[i] == "--language" && i + 1 < args.Length) language = args[++i];
        else { Console.Error.WriteLine(Texts.Get("Usage")); return 1; }
    }
    if (language is not null) Texts.SetLanguage(language);
    if (help) { Console.WriteLine(Texts.Get("Usage")); return 0; }
    if (Console.IsInputRedirected) { Console.Error.WriteLine(Texts.Get("InteractiveRequired")); return 1; }

    var paths = new AppPaths();
    var languagePath = Path.Combine(paths.DataDirectory, "language.txt");
    if (language is null && File.Exists(languagePath))
    {
        var saved = (await File.ReadAllTextAsync(languagePath, cancellation.Token)).Trim();
        if (saved is "ja" or "en") Texts.SetLanguage(saved);
    }
    var database = new Database(paths);
    await new DatabaseInitializer(database).InitializeAsync();
    var users = new UserRepository(database);
    var plays = new PlayRepository(database);
    using var http = new HttpClient { BaseAddress = new Uri("https://api.beatleader.xyz/") };
    http.DefaultRequestHeaders.UserAgent.ParseAdd("PlaysToPlaylist/1.0");
    var client = new BeatLeaderClient(http);
    var history = new HistoryService(client, new SongRepository(database), new BeatmapRepository(database),
        plays, new HistoryCacheRepository(database));
    var context = new ScreenContext(new AppServices(new UserService(client, users),
        new PlaylistService(history, plays, paths, new PlaylistImageService(http)), history, paths));
    context.ChangeScreen(new MainMenuScreen());
    await context.RunAsync(cancellation.Token);
    return 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.WriteLine(Texts.Get("Cancelled"));
    return 130;
}
catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException
    or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or ArgumentException)
{
    Console.Error.WriteLine(Texts.Get(ScreenUi.ErrorKey(ex), ex.Message));
    return 1;
}
