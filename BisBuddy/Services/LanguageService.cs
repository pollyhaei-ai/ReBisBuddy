using BisBuddy.Resources;
using Dalamud.Plugin;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace BisBuddy.Services
{
    public delegate void LanguageChangeDelegate(string langCode);

    public class LanguageService(
        ITypedLogger<LanguageService> logger,
        IDalamudPluginInterface pluginInterface
        ) : ILanguageService
    {
        private readonly ITypedLogger<LanguageService> logger = logger;
        private readonly IDalamudPluginInterface pluginInterface = pluginInterface;

        public event LanguageChangeDelegate? OnLanguageChange;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            setLanguage(pluginInterface.UiLanguage);
            pluginInterface.LanguageChanged += handleLanguageChange;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            pluginInterface.LanguageChanged -= handleLanguageChange;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Dalamud's Traditional Chinese UI language is the code "tw".
        /// "zh" is what Dalamud reports when the OS culture is Chinese and no UI language was chosen.
        /// This Taiwan build maps those codes onto the zh-Hant satellite.
        /// </summary>
        internal static string MapUiLanguage(string? langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode))
                return "zh-Hant";

            var code = langCode.Trim();
            if (code.Equals("tw", StringComparison.OrdinalIgnoreCase)
                || code.Equals("zh", StringComparison.OrdinalIgnoreCase)
                || code.Equals("zh-TW", StringComparison.OrdinalIgnoreCase)
                || code.Equals("zh-Hant", StringComparison.OrdinalIgnoreCase)
                || code.Equals("zh-CHT", StringComparison.OrdinalIgnoreCase)
                || code.Equals("cht", StringComparison.OrdinalIgnoreCase))
                return "zh-Hant";

            return code;
        }

        private void setLanguage(string langCode)
        {
            var cultureName = MapUiLanguage(langCode);
            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                logger.Warning($"UI language \"{langCode}\" is not a known culture. Using zh-Hant.");
                culture = CultureInfo.GetCultureInfo("zh-Hant");
            }

            logger.Info($"Setting plugin language to \"{culture}\" (Dalamud UI language \"{langCode}\")");
            Resource.Culture = culture;
        }

        private void handleLanguageChange(string langCode)
        {
            setLanguage(langCode);
            OnLanguageChange?.Invoke(langCode);
        }
    }

    public interface ILanguageService : IHostedService
    {
        public event LanguageChangeDelegate? OnLanguageChange;
    }
}
