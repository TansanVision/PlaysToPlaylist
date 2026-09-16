namespace PlaystoPlaylist.States;

/// <summary>
/// 画面のインターフェースを定義します。
/// </summary>
public interface IScreen
{
    /// <summary>
    /// 画面の処理を実行します。
    /// </summary>
    /// <param name="context">画面のコンテキスト情報。</param>
    /// <param name="cancellationToken">操作のキャンセルを通知するためのトークン。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    Task ExecuteAsync(
        ScreenContext context,
        CancellationToken cancellationToken);
}
