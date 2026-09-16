namespace PlaystoPlaylist.Data;

/// <summary>
/// データベースの初期化を行うクラスです。
/// </summary>
public sealed class DatabaseInitializer
{
    /// <summary>
    /// データベースのインスタンスを保持するフィールドです。
    /// </summary>
    private readonly Database _database;

    /// <summary>
    /// データベースの初期化を行うためのコンストラクタです。
    /// </summary>
    /// <param name="database">初期化に使用するデータベースのインスタンス。</param>
    public DatabaseInitializer(Database database)
    {
        this._database = database;
    }

    /// <summary>
    /// データベースの初期化を非同期で行います。必要に応じてテーブルを作成します。
    /// </summary>
    /// <returns>非同期操作を表すタスク。</returns>
    public async Task InitializeAsync()
    {
        await this._database.ExecuteTransactionAsync(
            (connection, transaction) =>
            {
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        beatleader_id TEXT NOT NULL UNIQUE,
                        alias TEXT,
                        name TEXT NOT NULL,
                        avatar_url TEXT,
                        created_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                        updated_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
                    );

                    CREATE TABLE IF NOT EXISTS Songs (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        hash TEXT NOT NULL UNIQUE,
                        beatleader_song_id TEXT,
                        name TEXT NOT NULL,
                        sub_name TEXT,
                        created_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                        updated_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
                    );

                    CREATE TABLE IF NOT EXISTS Beatmaps (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        song_id INTEGER NOT NULL,
                        leaderboard_id TEXT NOT NULL UNIQUE,
                        difficulty_name TEXT NOT NULL,
                        mode_name TEXT NOT NULL,
                        difficulty_value INTEGER,
                        created_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                        updated_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                        FOREIGN KEY(song_id) REFERENCES Songs(id) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS Plays (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        beatleader_score_id TEXT NOT NULL UNIQUE,
                        user_id INTEGER NOT NULL,
                        beatmap_id INTEGER NOT NULL,
                        base_score INTEGER NOT NULL,
                        modified_score INTEGER NOT NULL,
                        accuracy REAL NOT NULL,
                        pp REAL,
                        max_combo INTEGER NOT NULL,
                        full_combo INTEGER NOT NULL DEFAULT 0,
                        bad_cuts INTEGER,
                        missed_notes INTEGER,
                        bomb_cuts INTEGER,
                        walls_hit INTEGER,
                        modifiers TEXT,
                        played_at TEXT NOT NULL,
                        fetched_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                        FOREIGN KEY(user_id) REFERENCES Users(id),
                        FOREIGN KEY(beatmap_id) REFERENCES Beatmaps(id)
                    );

                    CREATE TABLE IF NOT EXISTS History_Cache_Days(
                        user_id INTEGER NOT NULL,
                        date TEXT NOT NULL,
                        UNIQUE(user_id, date),
                        FOREIGN KEY(user_id) REFERENCES Users(id) ON DELETE CASCADE
                    );
                ";
                    command.ExecuteNonQuery();
                    using var versionCommand = connection.CreateCommand();
                    versionCommand.CommandText = "PRAGMA user_version;";
                    var version = Convert.ToInt64(versionCommand.ExecuteScalar());
                    if (version < 1)
                    {
                        // Old versions cached only the first API page; retain scores and refetch ranges.
                        command.CommandText = """
                            DELETE FROM History_Cache_Days;
                            DELETE FROM Plays WHERE user_id NOT IN (SELECT id FROM Users);
                            PRAGMA user_version = 1;
                            """;
                        command.ExecuteNonQuery();
                    }
                    command.CommandText = "CREATE INDEX IF NOT EXISTS ix_plays_user_date ON Plays(user_id, played_at);";
                    command.ExecuteNonQuery();
                }
            },
            doCommit: true);
    }
}
