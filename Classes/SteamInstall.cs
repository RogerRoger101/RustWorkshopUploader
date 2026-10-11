using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace RustWorkshopUploader.Classes
{
    internal static class SteamInstall
    {
        private static readonly HashSet<string> SkipDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Windows",
            "$Recycle.Bin",
            "System Volume Information"
        };

        public static string Find()
        {
            foreach (var candidate in RegistryCandidates())
            {
                if (IsSteamClient(candidate))
                    return candidate;
            }

            foreach (var drive in FixedDrives())
            {
                foreach (var relative in new[]
                {
                    @"Program Files (x86)\Steam",
                    @"Program Files\Steam",
                    "Steam",
                    @"Games\Steam"
                })
                {
                    var path = Path.Combine(drive, relative);
                    if (IsSteamClient(path))
                        return path;
                }
            }

            foreach (var drive in FixedDrives())
            {
                var found = Search(drive, 0, 4);
                if (found != null)
                    return found;
            }

            return null;
        }

        public static void EnsureRegistry(string steamPath)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Valve\Steam"))
            {
                if (key == null)
                    return;

                if (IsSteamClient(AsDirectory(key.GetValue("SteamPath") as string)))
                    return;

                key.SetValue("SteamPath", steamPath.Replace('\\', '/'));
                key.SetValue("SteamExe", Path.Combine(steamPath, "steam.exe").Replace('\\', '/'));
            }
        }

        public static void EnsureRunning(string steamPath)
        {
            if (IsSteamProcessRunning())
                return;

            Process.Start(Path.Combine(steamPath, "steam.exe"));
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline)
            {
                if (IsSteamProcessRunning())
                    return;

                System.Threading.Thread.Sleep(500);
            }
        }

        private static IEnumerable<string> RegistryCandidates()
        {
            yield return AsDirectory(ReadRegistry(Registry.CurrentUser, @"Software\Valve\Steam\ActiveProcess", "SteamClientDll64"));
            yield return AsDirectory(ReadRegistry(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"));
            yield return AsDirectory(ReadRegistry(Registry.CurrentUser, @"Software\Valve\Steam", "SteamExe"));
            yield return AsDirectory(ReadRegistry(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"));
            yield return AsDirectory(ReadRegistry(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath"));
        }

        private static string ReadRegistry(RegistryKey root, string subKey, string name)
        {
            try
            {
                using (var key = root.OpenSubKey(subKey))
                {
                    return key?.GetValue(name) as string;
                }
            }
            catch (System.Security.SecurityException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static IEnumerable<string> FixedDrives()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed)
                    continue;

                bool ready;
                try
                {
                    ready = drive.IsReady;
                }
                catch (IOException)
                {
                    continue;
                }

                if (ready)
                    yield return drive.Name;
            }
        }

        private static string Search(string directory, int depth, int maxDepth)
        {
            if (IsSteamClient(directory))
                return directory;
            if (depth >= maxDepth)
                return null;

            string[] children;
            try
            {
                children = Directory.GetDirectories(directory);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }

            foreach (var child in children)
            {
                var name = Path.GetFileName(child);
                if (SkipDirectoryNames.Contains(name))
                    continue;

                var found = Search(child, depth + 1, maxDepth);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static bool IsSteamClient(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                return File.Exists(Path.Combine(path, "steam.exe"))
                    && File.Exists(Path.Combine(path, "steamclient64.dll"));
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static bool IsSteamProcessRunning()
        {
            try
            {
                return Process.GetProcessesByName("steam").Length > 0;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static string AsDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Replace('/', '\\').Trim().Trim('"').TrimEnd('\\');
            var fileName = Path.GetFileName(path);
            if (fileName.Equals("steam.exe", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("steamclient64.dll", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("steamclient.dll", StringComparison.OrdinalIgnoreCase))
                return Path.GetDirectoryName(path);

            return path;
        }
    }
}
