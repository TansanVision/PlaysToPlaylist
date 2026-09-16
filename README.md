# PlaysToPlaylist

[日本語](#日本語) | [English](#english)

## 日本語

**BeatLeaderに登録されているスコアから、指定した期間のBeat Saberプレイリスト（`.bplist`）を作るコンソールアプリです。**
「昨日遊んだ曲をもう一度遊びたい」「週末にプレイした曲をまとめたい」ときに、プレイヤーと日付を選ぶだけで曲をまとめられます。

### できること

- BeatLeaderのプレイヤーを複数登録し、プロフィールの更新・登録の削除ができます。
- 対象ユーザーのアバターをカバー画像としてプレイリスト内に埋め込みます（PNG・JPEG、5 MiBまで）。画像ファイルを別途コピーする必要はありません。未設定の場合は画像なし、通信失敗・非対応形式の場合はメッセージを表示して画像なしで作成します。取得は最大15秒で打ち切ります。アバターのURLが変わった場合は「プロフィールを更新」してから再作成してください。
- 開始日と終了日を指定して、曲・プレイした難易度・モードをプレイリストに出力します。同じ曲はまとめられます。
- 取得したスコアをローカルのSQLiteデータベースに保存し、取得済みの過去の日付を再利用します。当日分は毎回取得します。
- 日本語・英語で操作できます。メインメニューの「言語 / Language」で切り替えた設定は保存されます。

**対象はBeatLeaderの公開スコアAPIが返すデータです。すべてのプレイ試行・失敗・過去の再プレイを復元するツールではありません。** APIで取得できないスコアや、曲情報が欠けているデータは含まれません。曲ファイルのダウンロードやBeat Saberへの自動インストールも行いません。

### 必要なものと起動方法

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)（ソースからビルド・実行する場合）
- インターネット接続と、対象プレイヤーのBeatLeader ID
- キーボードで対話操作できるターミナル
- 出力をゲームで使う場合は、`.bplist`を読み込める環境

リポジトリのルートで実行します。

```sh
dotnet build
dotnet run --project PlaysToPlaylist -- --language ja
```

英語で起動する場合:

```sh
dotnet run --project PlaysToPlaylist -- --language en
```

`--language`を省略すると、保存済みの言語設定を使用します。未設定ならOSの表示言語が日本語のときは日本語、それ以外は英語です。起動時の`--language`指定はその起動だけに適用されます。ヘルプは`--help`です。

### 使い方

1. **ユーザーを追加**を選択し、BeatLeaderのプレイヤーIDを入力します。プロフィールURLが`https://beatleader.com/u/123456789`なら`123456789`の部分です。URL全体は入力しません。追加後は`exit`でメニューへ戻ります。
2. **ユーザーを選択**から対象のプレイヤーを選びます。
3. **プレイリスト → プレイリストを作成**を選びます。
4. 開始日・終了日を`2026-09-15`のような`yyyy-MM-dd`形式で入力します。**日付は言語やPCのタイムゾーンにかかわらず日本時間（JST、UTC+9）**です。開始日の0時から終了日の翌日0時直前までが対象です。同じ日を2回指定すると1日分になります。未来の日付は指定できません。
5. 完了すると曲数と保存先が表示されます。対象のスコアがない場合は空のプレイリストが出力されます。
6. 生成された`.bplist`を、使用するプレイリスト対応MOD・アプリの手順に従って取り込みます。対応するPC環境ではBeat Saberの`Playlists`フォルダーに配置する方式があります。利用環境に合った取り込み方法を確認してください。

メニューは上下キーで選択し、Enterキーで決定します。「戻る」で前の画面へ戻ります。Ctrl+Cで終了できます。
「プロフィールを更新」はBeatLeaderから名前・アバターを再取得します。「ユーザーを削除」は確認後にローカルの登録とそのスコアキャッシュを削除します。BeatLeader上のアカウントや作成済みのプレイリストは削除しません。

### 保存先・キャッシュ

データは**実行ファイルと同じ場所にある`Data`フォルダー**に保存します。通常の`dotnet run`では`PlaysToPlaylist/bin/Debug/net10.0/Data/`です。

```text
Data/
  PlaysToPlaylists.db                       # 登録ユーザーとスコアキャッシュ
  language.txt                             # メニューで選択した表示言語
  Users/<BeatLeader ID>/
    2026-09-15.bplist                       # 1日分
    2026-09-01_2026-09-15.bplist             # 期間指定
```

同じユーザー・同じ期間で作成すると、正常に作成できた時点で同じファイルを上書きします。残したい場合は先に別名で保存してください。途中で通信に失敗した場合は再実行できます。失敗した期間は取得完了として扱わず、既存のプレイリストも保持します。

旧バージョンからの初回起動時には、不完全な取得済み記録をリセットします。ユーザーと保存済みスコアは残し、次回の作成時に不足分を取得します。過去の日付のキャッシュはBeatLeader側の後日の変更を自動追跡しません。過去データを取り直すには、ユーザーを削除してから再登録してください。

移行・バックアップはアプリ終了後に`Data`フォルダー全体をコピーしてください。書き込み可能な場所から実行してください。Debug/Releaseなど実行ファイルの場所が変わると保存先も変わります。

### 開発・検証

```sh
dotnet build
dotnet run --project PlaysToPlaylist.Tests
```

テストは追加のテストフレームワークを必要としない実行形式です（`dotnet test`ではなく上記コマンド）。HTTP応答を模擬し、一時的なSQLiteデータベースを使ってページ取得・日付境界・キャッシュ・ユーザー更新と削除・翻訳を確認します。BeatLeaderへの実通信はしません。失敗時は終了コード1を返します。

## English

**PlaysToPlaylist is a console app that creates Beat Saber playlists (`.bplist`) from a player's BeatLeader scores within a date range.** Use it to revisit yesterday's songs or collect the maps you played over a weekend.

### Features and scope

- Register multiple BeatLeader players, refresh their profiles, and remove local registrations.
- Embed the selected user's avatar as the playlist cover (PNG/JPEG, up to 5 MiB); no separate image file is needed. Missing avatars are omitted. Failed downloads or unsupported formats produce a message and a playlist without a cover. Downloads time out after 15 seconds. If the avatar URL changes, use **Update profile** before creating the playlist again.
- Export songs with their played difficulties and modes. Repeated songs are grouped into one entry.
- Cache scores locally in SQLite. Completed past dates are reused; today's scores are fetched again each time.
- Switch between Japanese and English from **Language / 言語** in the main menu. Menu selections are saved.

The app uses data returned by BeatLeader's public scores API. **It does not reconstruct every attempt, failed play, or historical replay.** Unavailable scores and entries with incomplete map data cannot be included. It does not download songs or install anything into Beat Saber.

### Requirements and launch

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) to build and run from source. You also need an internet connection, a BeatLeader player ID, and an interactive terminal. Using the exported file in-game requires support for `.bplist` playlists.

From the repository root:

```sh
dotnet build
dotnet run --project PlaysToPlaylist -- --language en
```

Use `--language ja` for Japanese. Without this option, the saved language is used; if none is saved, Japanese is selected for a Japanese OS display language, otherwise English. A command-line override applies only to the current launch. Use `--help` for help.

### Usage

1. Choose **Add user** and enter the player ID from a BeatLeader profile URL. For `https://beatleader.com/u/123456789`, enter `123456789`, not the entire URL. Enter `exit` after registration to return to the menu.
2. Choose **Select user**, then select a player.
3. Choose **Playlist → Create playlist**.
4. Enter start and end dates in `yyyy-MM-dd` format, such as `2026-09-15`. **All dates use Japan Standard Time (JST, UTC+9), regardless of the UI language or computer time zone.** Both dates are inclusive: the range ends just before midnight after the end date. Use the same date twice for one day. Future dates are not accepted.
5. The app displays the song count and output path. If there are no matching scores, it saves an empty playlist.
6. Import the `.bplist` using the instructions for your playlist-capable mod or app. Compatible PC setups may load files from Beat Saber's `Playlists` folder; check the import procedure for your environment.

Use arrow keys and Enter to navigate, **Back** to return, and Ctrl+C to exit. **Update profile** refreshes the player's name and avatar. **Remove user** asks for confirmation and deletes the local registration and cached scores; the BeatLeader account and exported playlists are kept.

### Files and caching

Files are stored in **`Data` beside the executable**. With a normal `dotnet run`, this is `PlaysToPlaylist/bin/Debug/net10.0/Data/`:

- `PlaysToPlaylists.db`: registered users and cached scores.
- `language.txt`: language chosen in the menu.
- `Users/<BeatLeader ID>/2026-09-15.bplist`: a single day.
- `Users/<BeatLeader ID>/2026-09-01_2026-09-15.bplist`: a date range.

Creating the same range for the same player replaces the existing file only after successful generation. Rename files first if you want to keep earlier exports. Failed downloads can be retried; incomplete ranges remain retryable and existing playlist files are preserved.

On the first launch after upgrading from the old version, incomplete cache markers are reset. Registered users and saved scores are kept, and missing data is fetched on the next export. Cached past dates do not automatically reflect later changes on BeatLeader. Remove and re-register a user to fetch their past scores again.

To back up or move your data, close the app and copy the entire `Data` folder. Run from a writable directory. Changing the executable location, including switching between Debug and Release, changes the data location.

### Development and checks

```sh
dotnet build
dotnet run --project PlaysToPlaylist.Tests
```

The regression suite is an executable with no additional test framework; use the command above rather than `dotnet test`. It uses fake HTTP responses and temporary SQLite databases to check pagination, date boundaries, cache behavior, user updates/deletion, and translations. It does not contact BeatLeader and exits with code 1 on failure.

## References / 参考

- [BeatLeader API implementation: player scores](https://github.com/BeatLeader/beatleader-server/blob/master/Controllers/PlayerScoresController.cs)
- [PlaylistManager: playlist support for Beat Saber](https://github.com/rithik-b/PlaylistManager)
- [BeatSaberPlaylistsLib](https://github.com/Zingabopp/BeatSaberPlaylistsLib)
- License: [MIT](LICENSE.txt)
