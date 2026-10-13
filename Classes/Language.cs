using System;
using System.Globalization;
using System.IO;
using System.Resources;

namespace RustWorkshopUploader.Classes
{
    internal static class Language
    {
        public static readonly string[] Codes = { "en", "es", "ru", "uk", "de", "fr" };
        public static readonly string[] Labels = { "EN", "ES", "RU", "UA", "DE", "FR" };

        public static readonly string[] Names =
        {
            "English",
            "Espa\u00f1ol",
            "\u0420\u0443\u0441\u0441\u043a\u0438\u0439",
            "\u0423\u043a\u0440\u0430\u0457\u043d\u0441\u044c\u043a\u0430",
            "Deutsch",
            "Fran\u00e7ais"
        };

        private static readonly ResourceManager[] Managers = new ResourceManager[Codes.Length];

        public static int Current { get; private set; }

        public static ResourceManager Resources
        {
            get
            {
                if (Current <= 0)
                    return null;

                if (Managers[Current] == null)
                    Managers[Current] = new ResourceManager(
                        "RustWorkshopUploader.Localization.strings_" + Codes[Current],
                        typeof(Language).Assembly);
                return Managers[Current];
            }
        }

        public static void Load()
        {
            Current = 0;
            try
            {
                if (!File.Exists(SettingsPath))
                    return;

                var index = Array.IndexOf(Codes, File.ReadAllText(SettingsPath).Trim().ToLowerInvariant());
                if (index >= 0)
                    Current = index;
            }
            catch
            {
                Current = 0;
            }
        }

        public static void Set(int index)
        {
            if (index < 0 || index >= Codes.Length)
                return;

            Current = index;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                File.WriteAllText(SettingsPath, Codes[index]);
            }
            catch
            {
            }
        }

        private static string SettingsPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RustWorkshopUploader",
                    "language.txt");
            }
        }
    }

    internal sealed class LanguageResourceManager : ResourceManager
    {
        public LanguageResourceManager()
            : base("RustWorkshopUploader.Localization.strings", typeof(Language).Assembly)
        {
        }

        public override string GetString(string name, CultureInfo culture)
        {
            var current = Language.Resources;
            if (current != null)
            {
                try
                {
                    var value = current.GetString(name, CultureInfo.InvariantCulture);
                    if (value != null)
                        return value;
                }
                catch (MissingManifestResourceException)
                {
                }
            }

            return base.GetString(name, CultureInfo.InvariantCulture);
        }
    }
}
