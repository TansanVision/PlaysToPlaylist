using PlaystoPlaylist.States;
using PlaystoPlaylist.States.User;
using PlaysToPlaylist.Localization;

namespace PlaysToPlaylist.States.User;

public sealed class UserSelectedScreen : IScreen
{
    public async Task ExecuteAsync(ScreenContext context, CancellationToken cancellationToken = default)
    {
        var user = context.SelectedUser;
        if (user is null) { context.ChangeScreen(new UserSelectScreen()); return; }
        ScreenUi.Header("UserMenu");
        switch (await ScreenUi.ChooseAsync(Texts.Get("SelectedUser", user.Name, user.BeatLeaderId), cancellationToken, "Update", "Remove", "Playlist", "Back"))
        {
            case "Update":
                context.SelectUser(await context.Services.User.UpdateUserAsync(user, cancellationToken));
                ScreenUi.Message("Updated");
                await ScreenUi.PauseAsync(cancellationToken);
                break;
            case "Remove":
                if (await ScreenUi.ChooseAsync(Texts.Get("ConfirmRemove", user.Name), cancellationToken, "No", "Yes") != "Yes") break;
                await context.Services.User.RemoveAsync(user);
                context.ClearSelectedUser();
                context.ChangeScreen(new UserSelectScreen());
                break;
            case "Playlist": context.ChangeScreen(new UserPlaylistScreen()); break;
            case "Back":
                context.ClearSelectedUser();
                context.ChangeScreen(new UserSelectScreen());
                break;
        }
    }
}
