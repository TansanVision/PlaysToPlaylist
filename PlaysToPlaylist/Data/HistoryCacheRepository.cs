using PlaystoPlaylist.Data;

namespace PlaysToPlaylist.Data;

/// <summary>
/// 履歴キャッシュのリポジトリクラスです。データベース内の履歴キャッシュに関する操作を提供します。
/// </summary>
public sealed class HistoryCacheRepository
{
    /// <summary>
    /// データベースのインスタンスを保持するフィールドです。
    /// </summary>
    private readonly Database _database;

    /// <summary>
    /// 履歴キャッシュリポジトリのインスタンスを初期化します。
    /// </summary>
    /// <param name="database">データベースのインスタンス。</param>
    public HistoryCacheRepository(Database database)
    {
        this._database = database;
    }

    /// <summary>
    /// 指定されたユーザーIDと日付に対して、履歴キャッシュが存在するかどうかを非同期で確認します。
    /// </summary>
    /// <param name="userId">ユーザーID。</param>
    /// <param name="date">日付。</param>
    /// <returns>履歴キャッシュが存在する場合は true、それ以外の場合は false。</returns>
    public async Task<bool> IsCompleteAsync(string userId, DateTime date)
    {
        bool result = false;

        await this._database.ExecuteTransactionAsync(
            (connection, transaction) =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT 
                        COUNT(*) 
                    FROM 
                        History_Cache_Days 
                    WHERE 
                        user_id = @userId 
                        AND date = @date;
                """;
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
                var t = command.ExecuteScalar();

                if (t is null)
                {
                    result = false;
                    return;
                }

                result = (int)t > 0;
            }, true);
        return result;
    }

    /// <summary>
    /// 指定されたユーザーIDと日付に対して、履歴キャッシュを完了としてマークします。すでに存在する場合は何も行いません。
    /// </summary>
    /// <param name="userId">ユーザーID。</param>
    /// <param name="date">日付。</param>
    /// <returns>非同期操作のタスク。</returns>
    public async Task MarkCompleteAsync(long userId, DateOnly date)
    {
        await this._database.ExecuteTransactionAsync(
            (connection, transaction) =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = """
                    INSERT INTO 
                        History_Cache_Days (
                            user_id, 
                            date
                        )
                    VALUES (
                        @userId, 
                        @date
                    )
                    ON CONFLICT(user_id, date) 
                    DO NOTHING;
                """;
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
                command.ExecuteNonQuery();
            }, true);
    }

    /// <summary>
    /// 指定されたユーザーIDと日付範囲に対して、履歴キャッシュが完了している日付のセットを非同期で取得します。
    /// </summary>
    /// <param name="userId">ユーザーID。</param>
    /// <param name="from">開始日付。</param>
    /// <param name="to">終了日付。</param>
    /// <returns>完了している日付のセット。</returns>
    public async Task<HashSet<DateOnly>> GetCompletedDatesAsync(
        long userId,
        DateOnly from,
        DateOnly to)
    {
        var result = new HashSet<DateOnly>();

        await this._database.ExecuteTransactionAsync(
            (connection, transaction) =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT 
                        date 
                    FROM 
                        History_Cache_Days 
                    WHERE 
                        user_id = @userId
                        AND date BETWEEN @from AND @to
                """;
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["date"] is string dateString && 
                        DateOnly.TryParse(dateString, out var date))
                    {
                        result.Add(date);
                    }
                }
            }, true);

        return result;
    }
}