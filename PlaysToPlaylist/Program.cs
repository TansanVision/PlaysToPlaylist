using PlaystoPlaylist.Api;
using PlaystoPlaylist.Data;
using PlaystoPlaylist.Services;
using PlaystoPlaylist.States;
using PlaysToPlaylist.Data;


var paths = new AppPaths();

var database = new Database(paths);
var initializer = new DatabaseInitializer(database);
await initializer.InitializeAsync();

var userRepository = new UserRepository(database);
var songRepository = new SongRepository(database);
var playRepository = new PlayRepository(database);
var historyCacheRepository = new HistoryCacheRepository(database);
var beatmapRepository = new BeatmapRepository(database);

var httpClient = new HttpClient()
{
    BaseAddress = new Uri("https://api.beatleader.xyz/")
};

httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PlaystoPlaylist/1.0");

var beatLeaderClient = new BeatLeaderClient(httpClient);

var userService = new UserService(beatLeaderClient, userRepository);
var historyService =
    new HistoryService(
        beatLeaderClient,
        songRepository, 
        beatmapRepository, 
        playRepository, 
        historyCacheRepository);
var playlistService =
    new PlaylistService(historyService, playRepository, paths);
var services =
    new AppServices(
        userService, 
        playlistService, 
        historyService, 
        paths);


var context =
    new ScreenContext(services);

context.ChangeScreen(new MainMenuScreen());

await context.RunAsync();

return 0;