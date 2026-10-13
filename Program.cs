using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using RustWorkshopUploader.Classes;
using RustWorkshopUploader.Localization;
using Steamworks;

namespace RustWorkshopUploader
{
    internal static class Program
    {
        public static AppId RustAppId = 252490;
        public static AppId SdkAppId = 391750;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var culture = new CultureInfo("en");
            Application.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            Language.Load();

            var steamPath = SteamInstall.Find();
            if (steamPath == null)
            {
                MessageBox.Show(strings.SteamInstallNotFound, strings.Message_Error, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                SteamInstall.EnsureRegistry(steamPath);
            }
            catch (Exception)
            {
                // Init can still succeed when Steam is already running and the registry write is denied.
            }

            try
            {
                SteamInstall.EnsureRunning(steamPath);
            }
            catch (Exception)
            {
                // The retry dialog below covers a client that did not start.
            }

            DialogResult result;
            do
            {
                try
                {
                    SteamClient.Init(SdkAppId);
                    result = DialogResult.OK;
                }
                catch (Exception)
                {
                    result = MessageBox.Show(strings.SteamNotFound, strings.Message_Error,
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Error);
                    if (result == DialogResult.Cancel)
                        return;
                }
            } while (result == DialogResult.Retry);

            try
            {
                Application.Run(new FrmMain());
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format(strings.AppError, e), strings.Message_Error, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}