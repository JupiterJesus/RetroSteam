using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RetroSteam.Steam
{
    internal class SteamShortcutsParser
    {
        private const string SHORTCUTS_HEADER = "\0shortcuts\0";
        internal SteamShortcutsParser()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        internal SteamShortcuts Parse(string filename)
        {
            SteamShortcuts shortcuts = new SteamShortcuts();

            if (File.Exists(filename))
            {
                string fileContents = File.ReadAllText(filename);
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
                        string index = m.Groups[1].Value;
                        int iIndex = int.Parse(index);
                        string title = m.Groups[2].Value;
                        string target = m.Groups[3].Value;
                        string startIn = m.Groups[4].Value;
                        string gridImage = m.Groups[5].Value;
                        string shortcutPath = m.Groups[6].Value;
                        string launchOptions = m.Groups[7].Value;
                        string misc = m.Groups[8].Value; // This has a bunch of random shit that we want to save but don't care about. It has the pattern "\u0002(key)(5-byte value)REPEAT"
                        string tags = m.Groups[9].Value; // This can be further parsed into a bunch of categories

                        Regex tagsReg = new Regex("\u0001(\\d+?)\0(.*?)\0");
                        MatchCollection tagMatches = tagsReg.Matches(tags);
                        List<string> categoryList = new List<string>();
                        foreach (Match tagMatch in tagMatches)
                        {
                            string tagIndex = tagMatch.Groups[1].Value;
                            int iTagIndex = int.Parse(tagIndex);
                            string tag = tagMatch.Groups[2].Value;
                            categoryList.Add(tag);
                        }
                        // category looks like "\u0001(?<index>\d+)\0(?<tag>.*?)\0", then repeats
                        shortcuts.GenerateAndAddShortcut(categoryList, title, target, startIn, launchOptions, gridImage, misc);
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
    }
}
