namespace PlaystoPlaylist.Data;

/// <summary>
/// 曲のデータを管理するリポジトリを提供するクラスです。
/// </summary>
public sealed class SongRepository
{
    /// <summary>
    /// データベースのインスタンスを保持するフィールドです。
    /// </summary>
    private readonly Database _database;

    /// <summary>
    /// SongRepository クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="database">データベースのインスタンス。</param>
    public SongRepository(Database database)
    {
        _database = database;
    }

    /// <summary>
    /// 指定されたハッシュに基づいて、曲を取得するか、存在しない場合は新しい曲を作成します。
    /// </summary>
    /// <param name="hash">曲のハッシュ値。</param>
    /// <param name="beatLeadrerSongId">BeatLeader の曲 ID。</param>
    /// <param name="name">曲の名前。</param>
    /// <param name="subName">曲のサブ名前。</param>
    /// <returns>曲の ID。</returns>
    public async Task<long> GetOrCreateAsync(
        string hash,
        string? beatLeadrerSongId,
        string name,
        string? subName)
    {
        long result = -1;

        await this._database.ExecuteTransactionAsync(
            (connection, transaction) =>
            {
                string query = """
                INSERT INTO songs (
                    hash,
                    beatleader_song_id,
                    name,
                    sub_name
                ) VALUES (
                    @hash,
                    @beatLeadrerSongId,
                    @name,
                    @subName
                )
                ON CONFLICT(hash) 
                DO UPDATE SET
                    beatleader_song_id = excluded.beatleader_song_id,
                    name = excluded.name,
                    sub_name = excluded.sub_name,
                    updated_at = datetime('now', 'localtime')
                RETURNING id;
                """;

                using var command = connection.CreateCommand();
                command.CommandText = query;
                command.Parameters.AddWithValue("@hash", hash);
                command.Parameters.AddWithValue("@beatLeadrerSongId", beatLeadrerSongId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@subName", subName ?? (object)DBNull.Value);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result = reader.GetInt64(0);
                }
            },
            doCommit: true);

        return result;
    }
}
