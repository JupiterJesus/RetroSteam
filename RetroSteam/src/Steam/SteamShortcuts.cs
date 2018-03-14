using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.ObjectModel;

namespace RetroSteam.Steam
{
    internal class SteamShortcuts : ICollection<Shortcut>
    {
        private const string SHORTCUTS_HEADER = "\0shortcuts\0";

        private enum ShortCutReplacePolicy
        {
            REPLACE, // Conserve shortcuts from all sources, but new duplicate shortcuts should replace existing ones
            IGNORE, // Conserve existing shortcuts - never delete or replace them
            WIPE, // Wipe out all shortcuts that were added by this app any time it runs
            TRIAL, // Make no permanent changes
        }

        private ShortCutReplacePolicy replacementPolicy = ShortCutReplacePolicy.REPLACE;

        private List<Shortcut> shortcuts = new List<Shortcut>();

        public int Count => ((ICollection<Shortcut>)shortcuts).Count;

        public bool IsReadOnly => ((ICollection<Shortcut>)shortcuts).IsReadOnly;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scut"></param>
        public void Add(Shortcut scut)
        {
            if (ShortCutExists(scut))
            {
                if (replacementPolicy == ShortCutReplacePolicy.REPLACE)
                {
                    shortcuts.Remove(scut);
                    shortcuts.Add(scut);
                }
                else if (replacementPolicy == ShortCutReplacePolicy.IGNORE)
                {

                }
            }
            else
            {
                shortcuts.Add(scut);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scut"></param>
        /// <returns></returns>
        internal bool ShortCutExists(Shortcut scut)
        {
            return shortcuts.Contains(scut);
        }

        /// <summary>
        /// Generates a shortcut (should be from a rom). These shortcuts don't have
        /// any of those secondary data keys (like time played and VR), nor do they have
        /// an icon set. We are only setting grid images here.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="title"></param>
        /// <param name="target"></param>
        /// <param name="startIn"></param>
        /// <param name="launchOptions"></param>
        /// <param name="gridImage"></param>
        /// <param name="configs"></param>
        /// <returns></returns>
        internal static Shortcut GenerateShortcut(string category, string title, string target, string startIn, string launchOptions, string gridImage)
        {
            List<string> categories = new List<string> { category };
            Shortcut scut = new Shortcut(categories, title, target, startIn, launchOptions, gridImage, gridImage, null);
            return scut;
        }

        /// <summary>
        /// Generates a shortcut (should be from a rom). These shortcuts don't have
        /// any of those secondary data keys (like time played and VR), nor do they have
        /// an icon set. We are only setting grid images here.
        /// </summary>
        /// <param name="categories"></param>
        /// <param name="title"></param>
        /// <param name="target"></param>
        /// <param name="startIn"></param>
        /// <param name="launchOptions"></param>
        /// <param name="gridImage"></param>
        /// <returns></returns>
        internal static Shortcut GenerateShortcut(List<string> categories, string title, string target, string startIn, string launchOptions, string gridImage)
        {
            Shortcut scut = new Shortcut(categories, title, target, startIn, launchOptions, gridImage, gridImage, null);
            return scut;
        }

        /// <summary>
        /// Generates a shortcut (should be from a rom) and adds it to this shortcut list. These shortcuts don't have
        /// any of those secondary data keys (like time played and VR), nor do they have
        /// an icon set. We are only setting grid images here.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="title"></param>
        /// <param name="target"></param>
        /// <param name="startIn"></param>
        /// <param name="launchOptions"></param>
        /// <param name="gridImage"></param>
        /// <returns></returns>
        internal Shortcut GenerateAndAddShortcut(string category, string title, string target, string startIn, string launchOptions, string gridImage)
        {
            List<string> categories = new List<string> { category };
            Shortcut scut = GenerateShortcut(categories, title, target, startIn, launchOptions, gridImage);
            Add(scut);
            return scut;
        }

        /// <summary>
        /// Generates a shortcut (should be from a rom) and adds it to this shortcut list. These shortcuts don't have
        /// any of those secondary data keys (like time played and VR), nor do they have
        /// an icon set. We are only setting grid images here.
        /// </summary>
        /// <param name="categories"></param>
        /// <param name="title"></param>
        /// <param name="target"></param>
        /// <param name="startIn"></param>
        /// <param name="launchOptions"></param>
        /// <param name="gridImage"></param>
        /// <returns></returns>
        internal Shortcut GenerateAndAddShortcut(List<string> categories, string title, string target, string startIn, string launchOptions, string gridImage)
        {
            Shortcut scut = GenerateShortcut(categories, title, target, startIn, launchOptions, gridImage);
            Add(scut);
            return scut;
        }

        public IEnumerator<Shortcut> GetEnumerator()
        {
            return ((IEnumerable<Shortcut>)shortcuts).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Shortcut>)shortcuts).GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        internal static SteamShortcuts Parse(string filename)
        {
            SteamShortcuts shortcuts = new SteamShortcuts();

            if (File.Exists(filename))
            {
                string fileContents = File.ReadAllText(filename, Encoding.GetEncoding(28591));
                if (fileContents.StartsWith(SHORTCUTS_HEADER))
                {
                    fileContents = fileContents.Remove(0, SHORTCUTS_HEADER.Length);

                    Regex reg = new Regex("\0(.*?)\0" +
                                          "\u0001AppName\0(.*?)\0" +
                                          "\u0001Exe\0(.*?)\0" +
                                          "\u0001StartDir\0(.*?)\0" +
                                          "\u0001icon\0(.*?)\0" +
                                          "\u0001ShortcutPath\0(.*?)\0" +
                                          "\u0001LaunchOptions\0(.*?)\0" +
                                         "(\u0002.*?)\0" +
                                                "tags\0(.*?)\u0008\u0008", RegexOptions.IgnoreCase);
                    MatchCollection mc = reg.Matches(fileContents);
                    foreach (Match m in mc)
                    {
                        Shortcut scut = new Shortcut();
                        string index = m.Groups[1].Value;
                        scut.Index = int.Parse(index);
                        scut.Title = m.Groups[2].Value;
                        scut.Target = m.Groups[3].Value;
                        scut.StartIn = m.Groups[4].Value;
                        scut.Icon = m.Groups[5].Value;
                        scut.ShortCutPath = m.Groups[6].Value;
                        scut.LaunchOptions = m.Groups[7].Value;
                        scut.Configs = m.Groups[8].Value; // This has a bunch of random shit that we want to save but don't care about. It has the pattern "\u0002(key)(5-byte value)REPEAT"
                        string tags = m.Groups[9].Value; // This can be further parsed into a bunch of categories

                        Regex tagsReg = new Regex("\u0001(\\d+?)\0(.*?)\0");
                        MatchCollection tagMatches = tagsReg.Matches(tags);
                        foreach (Match tagMatch in tagMatches)
                        {
                            string tagIndex = tagMatch.Groups[1].Value;
                            int iTagIndex = int.Parse(tagIndex);
                            string tag = tagMatch.Groups[2].Value;
                            scut.Categories.Add(tag);
                        }
                        // category looks like "\u0001(?<index>\d+)\0(?<tag>.*?)\0", then repeats
                        shortcuts.Add(scut);
                    }
                }
                else
                {
                    Console.Error.WriteLine("Passed in an invalid shortcuts file, didn't start with 0shortcuts0");
                }
            }
            else
            {
                Console.Error.WriteLine("Couldn't open shortcuts.vdf");
            }
            return shortcuts;
        }

        internal void Save(Stream output)
        {
            StreamWriter outfile = new StreamWriter(output, Encoding.GetEncoding(28591));
            outfile.Write("\0shortcuts\0");
            foreach (Shortcut scut in shortcuts)
            {
                outfile.Write(string.Format("\0{0}\0\u0001appname\0{1}\0\u0001exe\0{2}\0\u0001StartDir\0{3}\0\u0001icon\0{4}\0\u0001ShortcutPath\0{5}\0\u0001LaunchOptions\0{6}\0",
                    scut.Index,
                    scut.Title,
                    scut.Target,
                    scut.StartIn,
                    scut.Icon,
                    scut.ShortCutPath,
                    scut.LaunchOptions));

                if (scut.Configs != null) outfile.Write(scut.Configs);
                outfile.Write("\0tags\0");

                for (int i = 0; i < scut.Categories.Count; i++)
                {
                    string tag = scut.Categories[i];
                    outfile.Write(string.Format("\u0001{0}\0{1}\0", i, tag));
                }
                outfile.Write("\u0008\u0008");
            }
            outfile.Write("\u0008\u0008");
            outfile.Flush();
        }

        public void Clear()
        {
            ((ICollection<Shortcut>)shortcuts).Clear();
        }

        public bool Contains(Shortcut item)
        {
            return ((ICollection<Shortcut>)shortcuts).Contains(item);
        }

        public void CopyTo(Shortcut[] array, int arrayIndex)
        {
            ((ICollection<Shortcut>)shortcuts).CopyTo(array, arrayIndex);
        }

        public bool Remove(Shortcut item)
        {
            return ((ICollection<Shortcut>)shortcuts).Remove(item);
        }
    }
}