using PlaysToPlaylist.Localization;
using PlaystoPlaylist.Api;
using PlaystoPlaylist.Models;

namespace PlaystoPlaylist.Services;

/// <summary>
/// ユーザーの登録および管理に関連するサービスを提供するクラスです。
/// </summary>
public sealed class UserService
{
    /// <summary>
    /// BeatLeader API への HTTP リクエストを送信するための BeatLeaderClient インスタンスを保持するフィールドです。
    /// </summary>
    private readonly BeatLeaderClient _beatLeaderClient;

    /// <summary>
    /// 登録済みユーザーの情報を管理するための UserRepository インスタンスを保持するフィールドです。
    /// </summary>
    private readonly Data.UserRepository _userRepository;

    /// <summary>
    /// UserService クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="beatLeaderClient">BeatLeader API への HTTP リクエストを送信するための BeatLeaderClient インスタンス。</param>
    /// <param name="userRepository">登録済みユーザーの情報を管理するための UserRepository インスタンス。</param>
    public UserService(
        BeatLeaderClient beatLeaderClient,
        Data.UserRepository userRepository)
    {
        this._beatLeaderClient = beatLeaderClient;
        this._userRepository = userRepository;
    }

    /// <summary>
    /// 登録されているすべてのユーザーを非同期的に取得します。
    /// </summary>
    /// <returns>登録されているすべてのユーザーを含む配列を表すタスク。</returns>
    public Task<Models.RegisteredUser[]> GetAllUsersAsync()
    {
        return this._userRepository.GetAllAsync();
    }

    /// <summary>
    /// 指定されたプレイヤー ID が BeatLeader API に存在するかどうかを非同期的に確認します。
    /// </summary>
    /// <param name="playerId">確認するプレイヤーの ID。</param>
    /// <param name="cancellationToken">操作をキャンセルするためのトークン。</param>
    /// <returns>指定されたプレイヤー ID が存在する場合は true、存在しない場合は false。</returns>
    public Task<bool> ExistsBeatLeaderPlayerByIdAsync(
        string playerId,
        CancellationToken cancellationToken = default)
    {
        return
            this._beatLeaderClient
                      .ExistPlayerByIdAsync(
                            playerId,
                            cancellationToken);
    }

    /// <summary>
    /// 指定された BeatLeader ID が登録されているかどうかを非同期的に確認します。
    /// </summary>
    /// <param name="beatLeaderId">確認する BeatLeader ID。</param>
    /// <param name="cancellationToken">操作をキャンセルするためのトークン。</param>
    /// <returns>指定されたユーザー ID が登録されている場合は true、登録されていない場合は false。</returns>
    public async Task<bool> ExistsUserByIdAsync(
        string beatLeaderId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var allUsers =
            await this._userRepository.GetAllAsync();

        return
            allUsers.Any(user => user.BeatLeaderId == beatLeaderId.Trim());
    }

    /// <summary>
    /// 指定された BeatLeader プレイヤー ID を持つユーザーを登録します。
    /// </summary>
    /// <param name="playerId">登録するユーザーの BeatLeader プレイヤー ID。</param>
    /// <param name="alias">ユーザーのエイリアス（任意）。</param>
    /// <param name="cancellationToken">操作のキャンセルを通知するためのトークン。</param>
    /// <returns>登録されたユーザーの情報を含む RegisteredUser オブジェクト。</returns>
    /// <exception cref="InvalidOperationException">指定されたユーザーが既に登録されている場合、または BeatLeader プレイヤーが見つからない場合にスローされます。</exception>
    public async Task<Models.RegisteredUser> AddUserAsync(
        string playerId,
        string? alias = null,
        CancellationToken cancellationToken = default)
    {
        playerId = playerId.Trim();
        AppPaths.ValidatePlayerId(playerId);
        cancellationToken.ThrowIfCancellationRequested();
        var existing =
            await this._userRepository.FindByBeatLeaderIdAsync(playerId);

        if (existing is not null)
        {
            throw new InvalidOperationException(Texts.Get("Duplicate"));
        }

        var player =
            await this._beatLeaderClient.GetPlayerAsync(playerId, cancellationToken)
            ?? throw new InvalidOperationException(Texts.Get("NotFound"));

        AppPaths.ValidatePlayerId(player.Id);
        if (await _userRepository.FindByBeatLeaderIdAsync(player.Id) is not null)
            throw new InvalidOperationException(Texts.Get("Duplicate"));
        await this._userRepository.AddAsync(
                beatLeaderId: player.Id,
                alias: alias,
                name: player.Name,
                avatarUrl: player.Avatar);

        return
            await this._userRepository.FindByBeatLeaderIdAsync(player.Id)
            ?? throw new InvalidOperationException(Texts.Get("LoadFailed"));
    }

    /// <summary>
    /// 指定されたユーザーを更新します。
    /// </summary>
    /// <param name="user">更新するユーザーの情報を含む RegisteredUser オブジェクト。</param>
    /// <param name="cancellationToken">操作のキャンセルを通知するためのトークン。</param>
    /// <returns>更新されたユーザーの情報を含む RegisteredUser オブジェクト。</returns>
    /// <exception cref="InvalidOperationException">指定されたユーザーが存在しない場合、または BeatLeader プレイヤーが見つからない場合にスローされます。</exception>
    public async Task<Models.RegisteredUser> UpdateUserAsync(
        RegisteredUser user,
        CancellationToken cancellationToken = default)
    {
        var player =
            await this._beatLeaderClient.GetPlayerAsync(user.BeatLeaderId, cancellationToken)
            ?? throw new InvalidOperationException(Texts.Get("NotFound"));

        await this._userRepository.UpdateAsync(
            user.Id,
            user.Alias,
            player.Name,
            player.Avatar);

        return
            await this._userRepository.FindByBeatLeaderIdAsync(player.Id)
            ?? throw new InvalidOperationException(Texts.Get("LoadFailed"));
    }

    /// <summary>
    /// 指定されたユーザーを削除します。
    /// </summary>
    /// <param name="user">削除するユーザーの情報を含む RegisteredUser オブジェクト。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    public async Task RemoveAsync(RegisteredUser user) {
        await this._userRepository.DeleteAsync(user.Id);
    }
}
