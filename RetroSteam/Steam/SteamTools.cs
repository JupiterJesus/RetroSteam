using System;
using Microsoft.Win32;
using System.IO;
using System.Linq;

namespace RetroSteam.Steam
{
    /// <summary>
    /// 
    /// </summary>
    internal class SteamTools
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal static bool IsSteamRunning()
        {
            // TODO: IsSteamRunning
            return false;
        }

        /// <summary>
        /// Gets the shortcuts.vdf file for a specific user ID in the specified Steam installation folder.
        /// </summary>
        /// <param name="steamBase"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        internal static string GetShortcutsFile(string steamBase, string user)
        {
            string userData = GetUserFolder(steamBase, user);
            string config = userData + Path.DirectorySeparatorChar + "config";
            return config + Path.DirectorySeparatorChar + "shortcuts.vdf";
        }

        /// <summary>
        /// Gets the shortcuts.vdf file for a specific user ID in the registry-derived Steam installation folder.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        internal static string GetShortcutsFile(string user)
        {
            return GetShortcutsFile(GetSteamLocation(), user);
        }

        /// <summary>
        /// Gets the grid subfolder of userdata \ config file for a specific user ID in the registry-derived Steam installation folder.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        internal static string GetGridFolder(string user)
        {
            return GetGridFolder(GetSteamLocation(), user);
        }

        /// <summary
        /// Gets the "userdata" folder from the specified Steam installation.
        /// </summary>
        /// <param name="steamBase"></param>
        /// <returns></returns>
        internal static string GetUserdataFolder(string steamBase)
        {
            return steamBase + Path.DirectorySeparatorChar + "userdata";
        }

        /// <summary>
        /// Gets the subfolder of userdata for a specific user from the specified Steam installation.
        /// </summary>
        /// <param name="steamBase"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private static string GetUserFolder(string steamBase, string user)
        {
            return steamBase + Path.DirectorySeparatorChar + "userdata" + Path.DirectorySeparatorChar + user;
        }

        /// <summary>
        /// Gets the grid subfolder of userdata/config for a specific user from the specified Steam installation.
        /// </summary>
        /// <param name="steamBase"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private static string GetGridFolder(string steamBase, string user)
        {
            string userData = GetUserFolder(steamBase, user);
            string config = userData + Path.DirectorySeparatorChar + "config";
            return config + Path.DirectorySeparatorChar + "grid";
        }

        /// <summary>
        /// Gets the subfolder of userdata for a specific user from the registry-derived Steam installation.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private static string GetUserFolder(string user)
        {
            return GetSteamLocation() + Path.DirectorySeparatorChar + "userdata" + Path.DirectorySeparatorChar + user;
        }

        /// <summary>
        /// Gets the "userdata" folder from the registry-derived Steam installation.
        /// </summary>
        /// <returns></returns>
        internal static string GetUserdataFolder()
        {
            return GetUserdataFolder(GetSteamLocation());
        }

        /// <summary>
        /// Gets the Steam installation directory from the Windows registry.
        /// </summary>
        /// <returns></returns>
        internal static string GetSteamLocation()
        {
            string installPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null);
            return installPath.Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Gets an array of all user IDs from the registry-derived Steam installation.
        /// </summary>
        /// <returns></returns>
        internal static string[] GetUsers()
        {
            return GetUsers(GetSteamLocation());
        }

        /// <summary>
        /// Gets an array of all user IDs from the specified Steam installation.
        /// </summary>
        /// <param name="steamBase"></param>
        /// <returns></returns>
        internal static string[] GetUsers(string steamBase)
        {
            string userdata = GetUserdataFolder(steamBase);
            string[] dirs = Directory.GetDirectories(userdata, "*", SearchOption.TopDirectoryOnly);

            string[] users = new string[dirs.Length];
            for (int i = 0; i < dirs.Length; i++)
            {
                users[i] = (dirs[i].TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar).Last());
            }
            return users;
        }

        internal static SteamShortcuts LoadShortcuts(string shortcutsFile)
        {
            SteamShortcuts shortcuts = SteamShortcuts.Parse(shortcutsFile);
            return shortcuts;
        }

        internal static void WriteShortcuts(SteamShortcuts shortcuts, string shortcutsFile)
        {
            // TODO MAKE A BACKUP OF THE OUTPUT FILE
            Stream o = File.Open(shortcutsFile, FileMode.Create);
            if (shortcuts != null)
                shortcuts.Save(o);
        }

        internal static void PrintShortcuts(SteamShortcuts shortcuts)
        {
            Stream o = Console.OpenStandardOutput();
            if (shortcuts != null)
                shortcuts.Print(o);
        }

        internal static void AddGridImage(string user, string gridImage, string steamID)
        {
            String gridFolder = GetGridFolder(user);
            String newImage = gridFolder + Path.DirectorySeparatorChar + steamID;
            File.Copy(gridImage, newImage);
        }
    }
}