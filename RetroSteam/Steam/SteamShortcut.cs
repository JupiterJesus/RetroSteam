using System.Collections.Generic;
using Crc32C;
using System.Text;

namespace RetroSteam
{
    internal class SteamShortcut
    {
        /* The following properties of the shortcut are used in my app */
        /// <summary>
        /// A list of tags assigned to the shortcut.
        /// </summary>
        public List<string> Categories { get; internal set; }

        /// <summary>
        /// The shortcut's title. This is the rom's name, or whatever the parsed shortcut's title is.
        /// </summary>
        public string Title { get; internal set; }

        /// <summary>
        /// This is the executable's path and filename.
        /// </summary>
        public string Target { get; internal set; }

        /// <summary>
        /// This is the Start In directory, usually just the executable directory.
        /// </summary>
        public string StartIn { get; internal set; }

        /// <summary>
        /// This is a parameter string to pass to the Target.
        /// </summary>
        public string LaunchOptions { get; internal set; }

        /// <summary>
        /// This is the grid image. It isn't part of the shortcut per se, since the shortcut only stores icons.
        /// Instead, the grid image is stored elsewhere, even though it is associated with a shortcut.
        /// </summary>
        public string GridImage { get; internal set; }

        /* The following are superfluous to my app, and are parsed for completeness, or just for
           writing back into the file so I don't break anything.
        */

        /// <summary>
        /// Icon is set in standard steam mode, and exists in many shortcuts already in steam, but
        /// this tool DOESN'T add any icons, so these are always parsed from a file.
        /// </summary>
        public string Icon { get; internal set; }

        /// <summary>
        /// This is a string made up of all the miscellaneous key/value pairs that I don't care about.
        /// It only comes from parsed shortcuts, and should be written back out exactly as it was read.
        /// </summary>
        public string Configs { get; internal set; }

        /// <summary>
        /// I don't know what this is, but it is parsed from shortcuts.vdf. I don't add this, or mess with it,
        /// and it should be written back exactly as-is.
        /// </summary>
        public string ShortCutPath { get; internal set; }

        /// <summary>
        /// This is read from the shortcuts file. It is just an ordinal index indicating the order of the shortcut in the file.
        /// I don't need this, and I can generate the index by the order in the list. Keeping it here anyway.
        /// </summary>
        public int Index { get; internal set; }


        /// <summary>
        /// Generates the app id for a given shortcut.Steam uses app ids as a unique
        /// identifier for games, but since shortcuts dont have a canonical serverside
        /// representation they need to be generated on the fly.The important part 
        /// about this function is that it will generate the same app id as Steam does
        /// for a given shortcut
        /// </summary>
        public string SteamID
        {
            get
            {
                Crc32CAlgorithm algo = new Crc32CAlgorithm();
                string input = this.Target + this.Title;
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                ulong result = Crc32CAlgorithm.Compute(inputBytes);
                result |= 0x80000000;
                result = (result << 32) | 0x02000000;
                return result.ToString();
            }
        }

        /// <summary>
        /// Equality is based on the Category and Title. Two shortcuts with the same category + title are equal,
        /// and can't coexist in a list. I use this to look up if a list of shortcuts already has
        /// a shortcut of the same title within the same category
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
            /*
            if (obj == null)
                return false; // This isn't null but that is, so can't be equal
            else if (!(obj is SteamShortcut))
                return false;
            else
            {
                SteamShortcut that = (SteamShortcut)obj;
                string test1, test2;
                if (string.IsNullOrWhiteSpace(this.Category) && string.IsNullOrWhiteSpace(this.Target))
                    test1 = "";
                else if (string.IsNullOrWhiteSpace(this.Category))
                    test1 = this.Target;
                else if (string.IsNullOrWhiteSpace(this.Target))
                    test1 = this.Category;
                else
                    test1 = (this.Category + this.Target);

                if (string.IsNullOrWhiteSpace(that.Category) && string.IsNullOrWhiteSpace(that.Target))
                    test2 = "";
                else if (string.IsNullOrWhiteSpace(that.Category))
                    test2 = that.Target;
                else if (string.IsNullOrWhiteSpace(that.Target))
                    test2 = that.Category;
                else
                    test2 = (that.Category + that.Target);

                if (string.IsNullOrWhiteSpace(test1) && string.IsNullOrWhiteSpace(test2))
                    return true;
                else if (string.IsNullOrWhiteSpace(test1) || string.IsNullOrWhiteSpace(test2))
                    return false;
                else
                    return test1.Equals(test2);
            }
            */
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
            /*
            if (string.IsNullOrWhiteSpace(this.Category) && string.IsNullOrWhiteSpace(this.Target))
                return 0;
            else if (string.IsNullOrWhiteSpace(this.Category))
                return this.Target.GetHashCode();
            else if (string.IsNullOrWhiteSpace(this.Target))
                return this.Category.GetHashCode();
            else
                return (this.Category + this.Target).GetHashCode();
                */
        }

        /// <summary>
        /// Creates a new steam shortcut, fully populated with data.
        /// This constructor is only to be used when generating new shortcuts from roms.
        /// The shortcuts parsed from steam use the default constructor and property setters.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="target"></param>
        /// <param name="startIn"></param>
        /// <param name="launchOptions"></param>
        /// <param name="gridImage"></param>
        internal SteamShortcut(List<string> categories, string title, string target, string startIn, string launchOptions, string icon, string gridImage, string configs)
        {
            this.Categories = categories;
            this.Title = title == null ? null : title.Trim();
            this.Target = target == null ? null : target.Trim();
            this.StartIn = startIn == null ? null : startIn.Trim();
            this.LaunchOptions = launchOptions == null ? null : launchOptions.Trim();
            this.Icon = Icon == null ? null : icon.Trim();
            this.GridImage = gridImage == null ? null : gridImage.Trim();
            this.Configs = configs;
        }

        /// <summary>
        /// Creates a new, empty steam shortcut. Categories is initialized to an empty list.
        /// </summary>
        internal SteamShortcut()
        {
            this.Categories = new List<string>();
        }
    }
}