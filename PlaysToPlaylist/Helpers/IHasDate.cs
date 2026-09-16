namespace PlaysToPlaylist.Helpers
{
    /// <summary>
    /// 日付を持つことを示すインターフェースです。
    /// </summary>
    public interface IHasDate
    {
        /// <summary>
        /// 日付を取得または設定します。
        /// </summary>
        DateTime Date { get; set; }
    }
}
