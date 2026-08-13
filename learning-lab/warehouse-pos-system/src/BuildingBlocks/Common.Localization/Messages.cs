using System.Globalization;
using System.Resources;

namespace Common.Localization
{
    // A hand-written accessor over Messages.resx/Messages.ar.resx instead of
    // the VS-only ResXFileCodeGenerator designer — SDK-style projects already
    // compile .resx files into the main assembly (default culture) plus one
    // satellite assembly per culture-suffixed .resx (Messages.ar.resx ->
    // ar/Common.Localization.resources.dll) with zero extra MSBuild config.
    // Every lookup reads CultureInfo.CurrentUICulture, which
    // Common.RequestCulture's request-localization middleware sets per
    // request from the Accept-Language header — no DI/IStringLocalizer
    // needed for a plain exception constructor to resolve the right string.
    public static class Messages
    {
        private static readonly ResourceManager ResourceManager =
            new("Common.Localization.Messages", typeof(Messages).Assembly);

        public static string EntityNotFound(string entityName, object key) =>
            string.Format(CultureInfo.CurrentUICulture, Get("EntityNotFound"), entityName, key);

        public static string UnexpectedError => Get("UnexpectedError");

        public static string PasswordRequiresUppercase => Get("PasswordRequiresUppercase");

        public static string PasswordRequiresLowercase => Get("PasswordRequiresLowercase");

        public static string PasswordRequiresDigit => Get("PasswordRequiresDigit");

        public static string StockTransferLocationsMustDiffer => Get("StockTransferLocationsMustDiffer");

        public static string PromotionPercentageExceeds100 => Get("PromotionPercentageExceeds100");

        public static string PromotionFixedAmountUnreasonable => Get("PromotionFixedAmountUnreasonable");

        private static string Get(string key) =>
            ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
