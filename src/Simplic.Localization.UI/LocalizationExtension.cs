using CommonServiceLocator;
using System;

namespace Simplic.Localization.UI
{
    public class LocalizationExtension : System.Windows.Markup.MarkupExtension
    {
        private ILocalizationService localizationService;        

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return null;

            // lazy loading
            if(localizationService == null)
            {                
                localizationService = ServiceLocator.Current.GetInstance<ILocalizationService>();
            }
                
            return localizationService.Translate(Key);
        }

        /// <summary>
        /// Gets or sets the key to be translated
        /// </summary>
        public string Key { get; set; }
    }
}
 