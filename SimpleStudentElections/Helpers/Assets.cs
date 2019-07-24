using System.Web.Mvc;

namespace SimpleStudentElections.Helpers
{
    /// <summary>
    /// Helper for conditionally including assets in views. Accounts for including same asset multiple times as well as
    /// needing multiple assets (eg. css and js) for an asset
    /// </summary>
    public static class AssetsHtmlExtensions
    {
        private const string IncludeDataTablesKey = "_includeDataTables";
        private const string IncludeCkeditorKey = "_includeCkeditor";
        
        public static void IncludeDataTables(this HtmlHelper htmlHelper)
        {
            MarkIncluded(htmlHelper, IncludeDataTablesKey);
        }
        
        public static bool ShouldIncludeDataTables(this HtmlHelper htmlHelper)
        {
            return ShouldInclude(htmlHelper, IncludeDataTablesKey);
        }
        
        public static void IncludeCkeditor(this HtmlHelper htmlHelper)
        {
            MarkIncluded(htmlHelper, IncludeCkeditorKey);
        }
        
        public static bool ShouldIncludeCkeditor(this HtmlHelper htmlHelper)
        {
            return ShouldInclude(htmlHelper, IncludeCkeditorKey);
        }

        private static void MarkIncluded(HtmlHelper htmlHelper, string key)
        {
            htmlHelper.ViewData[key] = true;
        }
        
        private static bool ShouldInclude(HtmlHelper htmlHelper, string key)
        {
            return (bool) (htmlHelper.ViewData[key] ?? false);
        }
    }
}