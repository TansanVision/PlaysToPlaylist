using PlaystoPlaylist.States;
using PlaystoPlaylist.States.User;
using PlaysToPlaylist.Localization;

namespace PlaysToPlaylist.States.User;

public sealed class UserPlaylistScreen : IScreen
{
    public async Task ExecuteAsync(ScreenContext context, CancellationToken cancellationToken = default)
    {
        var user = context.SelectedUser;
        if (user is null) { context.ChangeScreen(new UserSelectScreen()); return; }
        ScreenUi.Header("Playlist");
        if (await ScreenUi.ChooseAsync(Texts.Get("SelectedUser", user.Name, user.BeatLeaderId), cancellationToken, "Create", "Back") == "Back")
        {
            context.ChangeScreen(new UserSelectedScreen());
            return;
        }
        do
        {
            var from = await ScreenUi.ReadDateAsync("From", cancellationToken);
            var to = await ScreenUi.ReadDateAsync("To", cancellationToken, from);
            var result = await context.Services.Playlist.CreateAsync(user, from, to, progress =>
                ScreenUi.Message("Progress", Texts.Get(progress.Stage.ToString()), progress.CurrentRange,
                    progress.TotalRanges, progress.Fetched, progress.Saved, progress.AlreadyExists, progress.Skipped), cancellationToken);
            ScreenUi.Message("Created", result.SongCount, result.Path);
            if (result.SongCount == 0) ScreenUi.Message("Empty");
            if (result.History.Skipped > 0) ScreenUi.Message("SkippedWarning");
            if (result.ThumbnailUnavailable) ScreenUi.Message("ThumbnailUnavailable");
        } while (await ScreenUi.ChooseAsync(Texts.Get("Another"), cancellationToken, "No", "Yes") == "Yes");
        context.ChangeScreen(new UserSelectedScreen());
    }
}
