using System.Globalization;
using PlaysToPlaylist.Localization;
using Spectre.Console;

namespace PlaystoPlaylist.States;

public static class ScreenUi
{
    public static void Header(string key)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold yellow]PlaysToPlaylist / {Markup.Escape(Texts.Get(key))}[/]");
    }

    public static Task<string> ChooseAsync(string title, CancellationToken cancellationToken, params string[] keys) => AnsiConsole.PromptAsync(
        new SelectionPrompt<string>().Title(Markup.Escape(title))
            .MoreChoicesText(Markup.Escape(Texts.Get("ChooseMenu")))
            .UseConverter(key => Markup.Escape(Texts.Get(key))).AddChoices(keys), cancellationToken);

    public static void Message(string key, params object?[] args) => AnsiConsole.WriteLine(Texts.Get(key, args));

    public static async Task PauseAsync(CancellationToken cancellationToken)
    {
        Message("Continue");
        await Console.In.ReadLineAsync(cancellationToken);
    }

    public static DateOnly TodayInJapan => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9)).DateTime);

    public static async Task<DateOnly> ReadDateAsync(string key, CancellationToken cancellationToken, DateOnly? minimum = null)
    {
        var input = await AnsiConsole.PromptAsync(new TextPrompt<string>(Texts.Get(key)).AllowEmpty().Validate(value =>
        {
            if (!DateOnly.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return ValidationResult.Error(Texts.Get("InvalidDate"));
            if (date > TodayInJapan) return ValidationResult.Error(Texts.Get("FutureDate"));
            if (minimum.HasValue && date < minimum) return ValidationResult.Error(Texts.Get("InvalidRange"));
            return ValidationResult.Success();
        }), cancellationToken);
        return DateOnly.ParseExact(input.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static string ErrorKey(Exception exception) => exception switch
    {
        OperationCanceledException => "Timeout",
        HttpRequestException => "NetworkError",
        System.Text.Json.JsonException or InvalidDataException => "DataError",
        IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException => "StorageError",
        _ => "Error"
    };
}
