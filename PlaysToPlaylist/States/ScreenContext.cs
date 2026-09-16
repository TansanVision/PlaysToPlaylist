using PlaystoPlaylist.Models;

namespace PlaystoPlaylist.States;

/// <summary>
/// <see cref="ScreenContext"/> クラスは、アプリケーションの画面遷移を管理するコンテキストを表します。
/// </summary>
/// <remarks>
/// 初期画面を指定して <see cref="ScreenContext"/> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="services">アプリケーションのサービス。</param>
public sealed class ScreenContext(AppServices services)
{
    /// <summary>
    /// 現在の画面を保持するフィールド
    /// </summary>
    private IScreen? _currentScreen;

    /// <summary>
    /// アプリケーションのサービスを保持するフィールド
    /// </summary>
    public AppServices Services
    {
        get;
    } = services;

    /// <summary>
    /// 現在の画面が実行中かどうかを示します。
    /// </summary>
    public bool IsRunning 
    { 
        get; 
        private set; 
    } = true;

    /// <summary>
    /// 選択されたユーザーを取得します。
    /// </summary>
    public RegisteredUser? SelectedUser
    {
        get;
        private set;
    }

    /// <summary>
    /// 画面を変更します。
    /// </summary>
    /// <param name="newScreen">新しい画面。</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void ChangeScreen(IScreen newScreen)
    {
        if (newScreen is null)
        {
            throw new ArgumentNullException(nameof(newScreen), "New screen cannot be null.");
        }

        this._currentScreen = newScreen;
    }

    /// <summary>
    /// 選択されたユーザーを設定します。
    /// </summary>
    /// <param name="user">設定するユーザー。</param>
    public void SelectUser(RegisteredUser user)
    {
        this.SelectedUser = user;
    }

    /// <summary>
    /// 選択されたユーザーをクリアします。
    /// </summary>
    public void ClearSelectedUser()
    {
        this.SelectedUser = null;
    }

    /// <summary>
    /// 現在の画面を取得または設定します。
    /// </summary>
    public void Exit()
    {
        this.IsRunning = false;

    }

    /// <summary>
    /// 現在の画面を実行し続ける非同期メソッドです。
    /// </summary>
    /// <param name="cancellationToken">キャンセレーション トークン。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (this.IsRunning)
        {
            if (this._currentScreen is null)
            {
                throw new InvalidOperationException("Current screen is null.");
            }

            await this._currentScreen.ExecuteAsync(this, cancellationToken);
        }
    }
}
