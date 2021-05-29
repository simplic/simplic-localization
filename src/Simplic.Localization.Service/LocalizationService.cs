using Newtonsoft.Json;
using Simplic.Framework.Repository;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Simplic.Localization.Service
{
    /// <summary>
    /// Localization service returns strings based on language keys.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private Dictionary<string, string> translations;
        private bool loadFromDBComplete;
        private const string ConfigName = "CurrentLanguage";

        public LocalizationService()
        {
            loadFromDBComplete = false;

            // We need to load the current language from the local system, not from the database
            CurrentLanguage = new CultureInfo("de-DE");
            translations = new Dictionary<string, string>();

            LoadSystemLocalization();
        }

        /// <summary>
        /// Load system localization files
        /// </summary>
        private void LoadSystemLocalization()
        {
            try
            {
                var path = Directory.GetCurrentDirectory();
                var file = File.ReadAllText($"{path}\\Localization\\global.{CurrentLanguage.Name}.json");
                var keys = JsonConvert.DeserializeObject<Dictionary<string, string>>(file);
                if (keys == null || keys.Count <= 0) return;

                foreach (var key in keys)
                {
                    translations[key.Key] = key.Value;
                }
            }
            catch
            {
                /* swallow */
            }
        }

        /// <summary>
        /// Load localization from the database
        /// </summary>
        public void LoadDatabaseLocalization()
        {
            var privateDirectory = RepositoryManager.Singleton.GetDirectory("/private/Localization/");
            var publicDirectory = RepositoryManager.Singleton.GetDirectory("/public/Localization/");

            AddLanguageKeysFromJson(publicDirectory);
            AddLanguageKeysFromJson(privateDirectory);

            loadFromDBComplete = true;
        }

        /// <summary>
        /// Reads language files inside a repo and adds keys to the cache.
        /// </summary>
        /// <param name="repoInfo">Repository to search for</param>
        private void AddLanguageKeysFromJson(RepositoryDirectoryInfo repoInfo)
        {
            if (repoInfo != null)
            {
                var files = RepositoryManager.Singleton.GetDirectoryContent(repoInfo.Guid)
                    .Where(x => x.Name.ToLower().EndsWith(CurrentLanguage.Name.ToLower()) && x.Extension?.ToLower() == "json");

                foreach (var file in files.OrderBy(x => x.Name))
                {
                    try
                    {
                        var keys = JsonConvert.DeserializeObject<Dictionary<string, string>>(file.ContentAsString);
                        if (keys == null || keys.Count <= 0) continue;

                        foreach (var key in keys)
                        {
                            translations[key.Key] = key.Value;
                        }
                    }
                    catch
                    {
                        throw new System.Exception($"Could not load localization file: {file.FullPath}. The localization file must be saved as UTF-8. " +
                            $"Furthermore, the localization file must be a valid JSON file.");
                    }
                }
            }
        }

        /// <summary>
        /// Translates a key to a language
        /// </summary>
        /// <param name="key">Key to translate</param>
        /// <returns>Translated text</returns>
        public string Translate(string key)
        {
            if (key == null)
                return "<NULL-KEY>";

            if (translations.ContainsKey(key) == false) return key;

            return translations[key];
        }

        /// <summary>
        /// Translates a key formatted to a language
        /// </summary>
        /// <param name="key">Key to translate</param>
        /// <param name="formatValues">Values to put in the string interpolation</param>
        /// <returns>Translated formatted text</returns>
        public string Translate(string key, params string[] formatValues)
        {
            var translated = Translate(key);

            return string.Format(translated, formatValues);
        }

        /// <summary>
        /// Changes the current language
        /// </summary>
        /// <param name="language">Language to change</param>
        public void ChangeLanguage(CultureInfo language)
        {
            // skip if same language
            if (CurrentLanguage == language) return;

            CurrentLanguage = language;

            Thread.CurrentThread.CurrentCulture = CurrentLanguage;
            Thread.CurrentThread.CurrentUICulture = CurrentLanguage;

            CultureInfo.DefaultThreadCurrentCulture = CurrentLanguage;
            CultureInfo.DefaultThreadCurrentUICulture = CurrentLanguage;

            translations.Clear();
            LoadSystemLocalization();

            if (loadFromDBComplete)
                LoadDatabaseLocalization();
        }

        /// <summary>
        /// Gets a list of available languages
        /// </summary>
        /// <returns>a list of available languages</returns>
        public IList<CultureInfo> GetAvailableLanguages()
        {
            return new List<CultureInfo> {
                { new CultureInfo("de-DE") },
                { new CultureInfo("en-US") }
            };
        }

        /// <summary>
        /// Gets the current language
        /// </summary>
        /// <returns><see cref="CultureInfo"/> Current language</returns>
        public CultureInfo CurrentLanguage { get; private set; }

        /// <summary>
        /// Searches the key list and returns the matching keys
        /// </summary>
        /// <param name="searchKey">Search text</param>
        /// <returns>Result</returns>
        public IDictionary<string, string> Search(string searchKey)
        {
            return translations.Where(x => x.Key.StartsWith(searchKey)).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}