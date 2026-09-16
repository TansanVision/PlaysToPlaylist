using System;
using System.Collections.Generic;
using System.Text;

namespace PlaystoPlaylist.Data;

/// <summary>
/// UserRepository の情報を表すクラスです。
/// </summary>
public sealed class UserRepository
{
    /// <summary>
    /// データベースのインスタンスを保持するフィールドです。
    /// </summary>
    private readonly Database _database;

    /// <summary>
    /// UserRepository クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="database">データベースのインスタンス。</param>
    public UserRepository(Database database)
    {
        _database = database;
    }

    /// <summary>
    /// すべての登録済みユーザーを非同期的に取得します。
    /// </summary>
    /// <returns>登録済みユーザーの配列。</returns>
    public Task<Models.RegisteredUser[]> GetAllAsync()
    {
        return
                this._database.QueryAsync(
                """
                    SELECT
                        id,
                        beatleader_id,
                        alias,
                        name,
                        avatar_url,
                        created_at,
                        updated_at
                    FROM users
                    ORDER BY name
                """,
                row => new Models.RegisteredUser
                {
                    Id = row.GetInt64(0),
                    BeatLeaderId = row.IsDBNull(1) ? string.Empty : row.GetString(1),
                    Alias = row.IsDBNull(2) ? null : row.GetString(2),
                    Name = row.IsDBNull(3) ? string.Empty : row.GetString(3),
                    AvatarUrl = row.IsDBNull(4) ? null : row.GetString(4),
                    CreatedAt = row.IsDBNull(5) ? DateTimeOffset.MinValue : new DateTimeOffset(row.GetDateTime(5)),
                    UpdatedAt = row.IsDBNull(6) ? DateTimeOffset.MinValue : new DateTimeOffset(row.GetDateTime(6))
                });
    }

    /// <summary>
    /// 指定された BeatLeaderId に基づいて、登録済みユーザーを非同期的に検索します。
    /// </summary>
    /// <param name="beatLeaderId">検索するユーザーの BeatLeaderId。</param>
    /// <returns>指定された BeatLeaderId に一致する登録済みユーザー、存在しない場合は null。</returns>
    public async Task<Models.RegisteredUser?> FindByBeatLeaderIdAsync(string beatLeaderId)
    {
        var users = await GetAllAsync();
        return users.FirstOrDefault(user => user.BeatLeaderId == beatLeaderId);
    }
    public async Task AddAsync(
        string beatLeaderId,
        string? alias,
        string name,
        string? avatarUrl)
    {
        await this._database.ExecuteTransactionAsync(
             (connection, transaction) =>
             {
                 using var command = connection.CreateCommand();
                 command.CommandText = @"
                    INSERT INTO
                        Users(
                            beatleader_id,
                            alias,
                            name,
                            avatar_url
                        )
                    VALUES (
                        @beatLeaderId,
                        @alias,
                        @name,
                        @avatarUrl
                    );
                ";

                 command.Parameters.AddWithValue("@beatLeaderId", beatLeaderId);
                 if (alias is null)
                 {
                     command.Parameters.AddWithValue("@alias", System.DBNull.Value);
                 }
                 else
                 {
                     command.Parameters.AddWithValue("@alias", alias);
                 }

                 command.Parameters.AddWithValue("@name", name);
                 if (avatarUrl is null)
                 {
                     command.Parameters.AddWithValue("@avatarUrl", System.DBNull.Value);
                 }
                 else
                 {
                     command.Parameters.AddWithValue("@avatarUrl", avatarUrl);
                 }
                 command.ExecuteNonQuery();
             },
             doCommit: true);
    }

    /// <summary>
    /// 指定された ID の登録済みユーザーの情報を更新します。
    /// </summary>
    /// <param name="id">更新するユーザーの ID。</param>
    /// <param name="alias">更新するユーザーのエイリアス。</param>
    /// <param name="name">更新するユーザーの名前。</param>
    /// <param name="avatarUrl">更新するユーザーのアバター URL。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    public async Task UpdateAsync(
        long id,
        string? alias,
        string name,
        string? avatarUrl)
    {
        await this._database.ExecuteTransactionAsync(
            (connection, transaction) =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE
                        Users
                    SET
                        alias = @alias,
                        name = @name,
                        avatar_url = @avatarUrl,
                        updated_at = datetime('now', 'localtime')
                    WHERE
                        id = @id;
                ";
                command.Parameters.AddWithValue("@id", id);
                if (alias is null)
                {
                    command.Parameters.AddWithValue("@alias", System.DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@alias", alias);
                }

                command.Parameters.AddWithValue("@name", name);
                if (avatarUrl is null)
                {
                    command.Parameters.AddWithValue("@avatarUrl", System.DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@avatarUrl", avatarUrl);
                }
                command.ExecuteNonQuery();
            },
            doCommit: true);
    }

    /// <summary>
    /// 指定された ID の登録済みユーザーを削除します。
    /// </summary>
    /// <param name="id">削除するユーザーの ID。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    public async Task DeleteAsync(long id)
    {
        await this._database.ExecuteTransactionAsync(
            (connection, transaction) =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    DELETE FROM Plays WHERE user_id = @id;
                    DELETE FROM History_Cache_Days WHERE user_id = @id;
                    DELETE FROM
                        Users
                    WHERE
                        id = @id;
                ";
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            },
            doCommit: true);
    }
}
