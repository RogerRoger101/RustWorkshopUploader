using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RustWorkshopUploader.Classes
{
    internal static class WindowPosition
    {
        public static void Load(Form form)
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return;

                var parts = File.ReadAllText(SettingsPath).Trim().Split(',');
                int x;
                int y;
                if (parts.Length < 2 || !int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y))
                    return;

                var bounds = new Rectangle(x, y, form.Width, form.Height);
                if (!IsVisibleOnScreen(bounds))
                    return;

                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(x, y);
            }
            catch
            {
            }
        }

        public static void Save(Form form)
        {
            try
            {
                if (form.WindowState != FormWindowState.Normal)
                    return;

                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                File.WriteAllText(SettingsPath, form.Left + "," + form.Top);
            }
            catch
            {
            }
        }

        private static bool IsVisibleOnScreen(Rectangle bounds)
        {
            const int margin = 40;
            var visible = new Rectangle(bounds.Left + margin, bounds.Top + margin,
                Math.Max(1, bounds.Width - margin * 2), Math.Max(1, bounds.Height - margin * 2));
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(visible))
                    return true;
            }
            return false;
        }

        private static string SettingsPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RustWorkshopUploader",
                    "position.txt");
            }
        }
    }
}
