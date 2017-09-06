using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RetroSteam.Steam
{
    internal class SteamShortcuts
    {
        private List<SteamShortcut> shortcuts = new List<SteamShortcut>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scut"></param>
        internal SteamShortcuts AddShortcut(SteamShortcut scut)
        {
            if (!ShortCutExists(scut))
                shortcuts.Add(scut);

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scut"></param>
        /// <returns></returns>
        internal bool ShortCutExists(SteamShortcut scut)
        {
            return shortcuts.Contains(scut);
        }

        internal static SteamShortcut GenerateShortcut(string category, string title, string target, string startIn, string launchOptions, string gridImage, string configs)
        {
            List<string> categories = new List<string>();
            categories.Add(category);
            SteamShortcut scut = new SteamShortcut(categories, title, target, startIn, launchOptions, gridImage, configs);
            return scut;
        }

        internal static SteamShortcut GenerateShortcut(List<string> categories, string title, string target, string startIn, string launchOptions, string gridImage, string configs)
        {
            SteamShortcut scut = new SteamShortcut(categories, title, target, startIn, launchOptions, gridImage, configs);
            return scut;
        }

        internal SteamShortcut GenerateAndAddShortcut(string category, string title, string target, string startIn, string launchOptions, string gridImage, string configs)
        {
            List<string> categories = new List<string>();
            categories.Add(category);
            SteamShortcut scut = GenerateShortcut(categories, title, target, startIn, launchOptions, gridImage, configs);
            AddShortcut(scut);
            return scut;
        }

        internal SteamShortcut GenerateAndAddShortcut(List<string> categories, string title, string target, string startIn, string launchOptions, string gridImage, string configs)
        {
            SteamShortcut scut = GenerateShortcut(categories, title, target, startIn, launchOptions, gridImage, configs);
            AddShortcut(scut);
            return scut;
        }
    }
}