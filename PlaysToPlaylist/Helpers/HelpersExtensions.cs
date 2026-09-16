namespace PlaysToPlaylist.Helpers;

/// <summary>
/// ヘルパー拡張メソッドを提供する静的クラスです。
/// </summary>
public static class HelpersExtensions
{
    /// <summary>
    /// 指定された配列の要素を、日付ごとにグループ化し、最初と最後のグループを除外して返します。
    /// </summary>
    /// <param name="array">日付を持つ要素の配列</param>
    /// <returns>最初と最後のグループを除外した日付ごとのグループ配列</returns>
    public static IGrouping<DateTime, IHasDate>[] TakeBetweenGrouping(IHasDate[] array)
    {
        if (array == null || array.Length == 0)
        {
            return [];
        }

        return
            [.. array.GroupBy(x => x.Date.Date)
                 .OrderBy(g => g.Key)
                 .Skip(1)
                 .SkipLast(1)];
    }
}
