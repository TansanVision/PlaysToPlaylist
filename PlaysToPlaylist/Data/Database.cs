using PlaystoPlaylist.Services;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlaystoPlaylist.Data;

/// <summary>
/// データベースの操作を提供するクラスです。
/// </summary>
public sealed class Database
{
    /// <summary>
    /// データベースの名前を保持するフィールドです。
    /// </summary>
    public readonly string Name = "PlaysToPlaylist.db";

    /// <summary>
    /// データベースの接続文字列を保持するフィールドです。
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// データベースのインスタンスを初期化します。
    /// </summary>
    /// <param name="path">アプリケーションのパスを管理するオブジェクト。</param>
    public Database(AppPaths path)
    {
        this._connectionString = new SqliteConnectionStringBuilder { DataSource = path.DatabasePath, ForeignKeys = true }.ToString();
    }

    /// <summary>
    /// 指定されたアクションをトランザクション内で実行します。トランザクションのコミットは、doCommit パラメータに基づいて行われます。
    /// </summary>
    /// <param name="action">トランザクション内で実行するアクション。</param>
    /// <param name="doCommit">トランザクションをコミットするかどうかを示す値。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    public async Task ExecuteTransactionAsync(
        Action<SqliteConnection, SqliteTransaction> action,
        bool doCommit)
    {
        SqliteConnection? connection = null;
        SqliteTransaction? transaction = null;

        try
        {
            connection = this.CreateConnection();
            await connection.OpenAsync();
            transaction = connection.BeginTransaction();
            action(connection, transaction);
            if (doCommit)
            {
                transaction.Commit();
            }

        }
        catch
        {


            transaction?.Rollback();
            throw;
        }
        finally
        {
            transaction?.Dispose();
            if (connection != null)
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// 指定されたクエリを実行し、結果をマッピングして単一のオブジェクトとして返します。結果が存在しない場合はデフォルト値を返します。
    /// </summary>
    /// <typeparam name="T">マッピングされるオブジェクトの型。</typeparam>
    /// <param name="query">クエリ文字列。</param>
    /// <param name="parameters">クエリパラメータの辞書。</param>
    /// <returns>マッピングされたオブジェクト。結果が存在しない場合はデフォルト値を返します。</returns>
    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string query,
        Dictionary<string, object>? parameters = null)
    {
        using var connection = this.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<T>(query, parameters);
    }

    /// <summary>
    /// 指定されたクエリを実行し、結果をマッピングして単一のオブジェクトとして返します。結果が存在しない場合はデフォルト値を返します。
    /// </summary>
    /// <typeparam name="T">マッピングされるオブジェクトの型。</typeparam>
    /// <param name="query">実行する SQL クエリ。</param>
    /// <param name="map">SqliteDataReader をオブジェクトにマッピングする関数。</param>
    /// <param name="parameters">クエリパラメータの辞書。</param>
    /// <returns>マッピングされたオブジェクトの配列。</returns>
    public async Task<T[]> QueryAsync<T>(
        string query,
        Func<SqliteDataReader, T> map,
        Dictionary<string, object>? parameters = null)
    {
        using var connection = this.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqliteCommand(query, connection);
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }

        using var reader = await command.ExecuteReaderAsync();
        var results = new List<T>();
        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }

        return [.. results];
    }

    /// <summary>
    /// データベースへの接続を作成します。
    /// </summary>
    /// <returns><see cref="SqliteConnection"/>。作成された接続を返します。</returns>
    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection(this._connectionString);
    }
}
