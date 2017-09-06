using System.Collections.Generic;

namespace RetroSteam
{
    internal class SteamShortcut
    {
        public List<string> Categories { get; internal set; }
        public string Category { get; internal set; }
        public string Title { get; internal set; }
        public string Target { get; internal set; }
        public string StartIn { get; internal set; }
        public string LaunchOptions { get; internal set; }
        public string GridImage { get; internal set; }
        public string Configs { get; internal set; }

        /// <summary>
        /// Equality is based on the Category and Title. Two shortcuts with the same category + title are equal,
        /// and can't coexist in a list. I use this to look up if a list of shortcuts already has
        /// a shortcut of the same title within the same category
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (string.IsNullOrWhiteSpace(this.Category) && string.IsNullOrWhiteSpace(this.Target))
                return 0;
            else if (string.IsNullOrWhiteSpace(this.Category))
                return this.Target.GetHashCode();
            else if (string.IsNullOrWhiteSpace(this.Target))
                return this.Category.GetHashCode();
            else
                return (this.Category + this.Target).GetHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="target"></param>
        /// <param name="startIn"></param>
        /// <param name="launchOptions"></param>
        /// <param name="gridImage"></param>
        internal SteamShortcut(List<string> categories, string title, string target, string startIn, string launchOptions, string gridImage, string configs)
        {
            this.Categories = categories;
            this.Title = title == null ? null : title.Trim();
            this.Target = target == null ? null : target.Trim();
            this.StartIn = startIn == null ? null : startIn.Trim();
            this.LaunchOptions = launchOptions == null ? null : launchOptions.Trim();
            this.GridImage = gridImage == null ? null : gridImage.Trim();
            this.Configs = configs;
        }
    }
}