namespace PlaystoPlaylist.Data;

/// <summary>
/// ビートマップのリポジトリを表すクラスです。
/// </summary>
public sealed class BeatmapRepository
{
    /// <summary>
    /// データベースのインスタンスを保持するフィールドです。
    /// </summary>
    private readonly Database _database;

    /// <summary>
    /// BeatmapRepository クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="database">データベースのインスタンス。</param>
    public BeatmapRepository(Database database)
    {
        _database = database;
    }

    /// <summary>
    /// 指定された情報に基づいて、ビートマップを取得するか、存在しない場合は新しいビートマップを作成します。
    /// </summary>
    /// <param name="songId">曲のID。</param>
    /// <param name="leaderBoardId">リーダーボードのID。</param>
    /// <param name="difficultyName">難易度の名前。</param>
    /// <param name="modeName">モードの名前。</param>
    /// <param name="difficultyValue">難易度の値。</param>
    /// <returns>ビートマップのID。</returns>
    public async Task<long> GetOrCreateAsync(
        long songId,
        string leaderBoardId,
        string difficultyName,
        string modeName,
        int? difficultyValue)
    {
        long result = -1;
        await this._database.ExecuteTransactionAsync(
            (connection, transaction) =>
            {
                string query = """
                INSERT INTO beatmaps (
                    song_id,
                    leaderboard_id,
                    difficulty_name,
                    mode_name,
                    difficulty_value
                ) VALUES (
                    @songId,
                    @leaderBoardId,
                    @difficultyName,
                    @modeName,
                    @difficultyValue
                )
                ON CONFLICT(leaderboard_id) 
                DO UPDATE SET
                    song_id = excluded.song_id,
                    difficulty_name = excluded.difficulty_name,
                    mode_name = excluded.mode_name,
                    difficulty_value = excluded.difficulty_value,
                    updated_at = datetime('now', 'localtime')
                RETURNING id;
                """;

                using var command = connection.CreateCommand();
                command.CommandText = query;
                command.Parameters.AddWithValue("@songId", songId);
                command.Parameters.AddWithValue("@leaderBoardId", leaderBoardId);
                command.Parameters.AddWithValue("@difficultyName", difficultyName);
                command.Parameters.AddWithValue("@modeName", modeName);
                command.Parameters.AddWithValue("@difficultyValue", difficultyValue);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result = reader.GetInt64(0);
                }
            },
            true);
        return result;
    }
}
