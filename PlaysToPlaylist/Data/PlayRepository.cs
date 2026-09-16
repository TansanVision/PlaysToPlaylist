using PlaystoPlaylist.Models;

namespace PlaystoPlaylist.Data;

/// <summary>
/// Plays テーブルの操作を提供するリポジトリクラスです。
/// </summary>
public sealed class PlayRepository
{
    /// <summary>
    /// データベースのインスタンスを保持するフィールドです。
    /// </summary>
    private readonly Database _database;

    /// <summary>
    /// コンストラクタです。
    /// </summary>
    /// <param name="database">データベースのインスタンス</param>
    public PlayRepository(Database database)
    {
        _database = database;
    }

    /// <summary>
    /// 指定された BeatLeader のスコア ID が Plays テーブルに存在するかどうかを非同期で確認します。
    /// </summary>
    /// <param name="beatLeaderScoreId">確認する BeatLeader のスコア ID</param>
    /// <returns>存在する場合は true、存在しない場合は false</returns>
    public async Task<bool> ExistsAsync(string beatLeaderScoreId)
    {
        string query = """
            SELECT
              EXISTS (
                  SELECT
                    1
                  FROM
                      Plays
                  WHERE
                      beatleader_score_id = @beatLeaderScoreId
              );
        """;

        return
            await _database.QuerySingleOrDefaultAsync<bool>(
                query, 
                new Dictionary<string, object>
                {
                    { "@beatLeaderScoreId", beatLeaderScoreId }
                });
    }

    /// <summary>
    /// 指定された情報を使用して Plays テーブルに新しいレコードを非同期で追加します。
    /// </summary>
    /// <param name="beatLeaderScoreId">BeatLeader のスコア ID</param>
    /// <param name="userId">ユーザー ID</param>
    /// <param name="beatmapId">ビートマップ ID</param>
    /// <param name="baseScore">基本スコア</param>
    /// <param name="modifiedScore">修正後のスコア</param>
    /// <param name="accuracy">精度</param>
    /// <param name="pp">パフォーマンスポイント</param>
    /// <param name="maxCombo">最大コンボ</param>
    /// <param name="fullCombo">フルコンボかどうか</param>
    /// <param name="badCuts">バッドカット数</param>
    /// <param name="missedNotes">ミスしたノート数</param>
    /// <param name="bombCuts">ボムカット数</param>
    /// <param name="wallsHit">壁に当たった回数</param>
    /// <param name="modifiers">修飾子</param>
    /// <param name="playedAt">プレイ日時</param>
    /// <returns><see cref="Task"/>。</returns>
    public async Task AddAsync(
        string beatLeaderScoreId,
        long userId,
        long beatmapId,
        int? baseScore,
        int? modifiedScore,
        double? accuracy,
        double? pp,
        int? maxCombo,
        bool fullCombo,
        int? badCuts,
        int? missedNotes,
        int bombCuts,
        int wallsHit,
        string modifiers,
        DateTimeOffset playedAt)
    {
        await this._database.ExecuteTransactionAsync((connection, transaction) =>
        {
            string query = """
                INSERT OR IGNORE INTO Plays (
                    beatleader_score_id,
                    user_id,
                    beatmap_id,
                    base_score,
                    modified_score,
                    accuracy,
                    pp,
                    max_combo,
                    full_combo,
                    bad_cuts,
                    missed_notes,
                    bomb_cuts,
                    walls_hit,
                    modifiers,
                    played_at
                ) VALUES (
                    @beatLeaderScoreId,
                    @userId,
                    @beatmapId,
                    @baseScore,
                    @modifiedScore,
                    @accuracy,
                    @pp,
                    @maxCombo,
                    @fullCombo,
                    @badCuts,
                    @missedNotes,
                    @bombCuts,
                    @wallsHit,
                    @modifiers,
                    @playedAt
                );
            """;

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@beatLeaderScoreId", beatLeaderScoreId);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@beatmapId", beatmapId);
            command.Parameters.AddWithValue("@baseScore", baseScore ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@modifiedScore", modifiedScore ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@accuracy", accuracy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@pp", pp ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@maxCombo", maxCombo ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@fullCombo", fullCombo ? 1 : 0);
            command.Parameters.AddWithValue("@badCuts", badCuts ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@missedNotes", missedNotes ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@bombCuts", bombCuts);
            command.Parameters.AddWithValue("@wallsHit", wallsHit);
            command.Parameters.AddWithValue("@modifiers", modifiers);
            command.Parameters.AddWithValue("@playedAt", playedAt.ToUniversalTime().ToString("O"));
            command.ExecuteNonQuery();
        }, doCommit: true);
    }

    /// <summary>
    /// 指定されたユーザー ID と期間に基づいて、プレイされた曲の情報を非同期で取得します。
    /// </summary>
    /// <param name="userId">ユーザー ID</param>
    /// <param name="from">取得開始日時</param>
    /// <param name="to">取得終了日時</param>
    /// <returns>プレイされた曲の情報の配列</returns>
    public async Task<PlaylistSong[]> GetPlaylistSongsAsync(
        long userId, 
        DateTimeOffset from,
        DateTimeOffset to)
    {
        string query = """
            SELECT DISTINCT
                s.beatleader_song_id AS Key,
                s.hash AS Hash,
                s.name || s.sub_name AS SongName,
                b.mode_name AS Characteristic,
                b.difficulty_name AS DifficultyName
            FROM plays p
            INNER JOIN beatmaps b ON p.beatmap_id = b.id
            INNER JOIN songs s ON b.song_id = s.id
            WHERE p.user_id = @userId
              AND p.played_at >= @from
              AND p.played_at <= @to
            ORDER BY
              p.played_at DESC,
              b.mode_name ASC,
              b.difficulty_name ASC
            ;
                
        """;

        var result = 
            await _database.QueryAsync(
                query,
                row => new PlaylistSongRow
                {
                    Key = row.GetString(0),
                    Hash = row.GetString(1),
                    Name = row.GetString(2),
                    Characteristic = row.GetString(3),
                    DifficultyName = row.GetString(4),
                },
                new Dictionary<string, object>
                {
                    { "@userId", userId },
                    { "@from", from.UtcDateTime.ToString("O") },
                    { "@to", to.UtcDateTime.ToString("O") }
                });

        return
            [.. result.GroupBy(x => x.Hash, StringComparer.OrdinalIgnoreCase)
                  .Select(group => 
                  {
                      var first = group.First();

                      return new PlaylistSong
                      {
                          Key = first.Key,
                          Hash = first.Hash,
                          Name = first.Name,
                          Dificulties = [.. group.Select(x => new PlaylistDificulty
                          {
                              Characteristic = x.Characteristic,
                              Name = x.DifficultyName
                          })
                          .DistinctBy(x => new { x.Characteristic, x.Name })]
                      };
                  })];
    }
}

/// <summary>
/// プレイリストの曲の行を表す内部クラスです。
/// </summary>
class PlaylistSongRow
{
    /// <summary>
    /// 曲のキーを取得または設定します。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 曲のハッシュを取得または設定します。
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 曲の名前を取得または設定します。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 難易度の特徴を取得または設定します。
    /// </summary>
    public string Characteristic { get; set; } = string.Empty;

    /// <summary>
    /// 難易度の名前を取得または設定します。
    /// </summary>
    public string DifficultyName { get; set; } = string.Empty;
}