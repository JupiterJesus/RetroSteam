using System;
using System.IO;
using System.Text.RegularExpressions;

namespace RetroSteam
{
    internal class Emulator
    {
        // Auto-implemented properties
        public string Category { get; internal set; }
        public string Executable { get; internal set; }
        public string StartIn { get; internal set; }
        public string Parameters { get; internal set; }
        public string IconBasePath { get; internal set; }
        public string IconRegex { get; internal set; }
        public string IconFile { get; internal set; }
        public string ImageBasePath { get; internal set; }
        public string ImageRegex { get; internal set; }
        public string ImageFile { get; internal set; }
        public string RomBasePath { get; internal set; }
        public string RomRegex { get; internal set; }
        public string TitlePattern { get; internal set; }

        internal Emulator()
        {

        }

        // TODO: Fill in the substitutions that are applicable without a rom
        // 
        /// <summary>
        /// Expands the variables in all of the individual settings, but only the ones that 
        /// don't require a rom path, filename or title (%E, %F, %f, %L, %B, %C, %I).
        /// Call this once per emulator, either upon instantiation of the emulator configuration
        /// or before parsing roms.
        /// </summary>
        internal void ExpandVariables()
        {
            // TODO implement ExpandVariables
            return;
        }

        /// <summary>
        /// E F f L R r D B P p N C I
        /// </summary>
        /// <param name="romPath"></param>
        /// <returns></returns>
        internal string ExpandParameters(string romPath)
        {
            return Expand(Parameters, romPath);
        }

        internal string ExpandImageBasePath(string romPath)
        {
            return Expand(ImageBasePath, romPath);
        }

        internal string ExpandImageRegex(string romPath)
        {
            return Expand(ImageRegex, romPath);
        }

        internal string ExpandImageFile(string romPath)
        {
            return Expand(ImageFile, romPath);
        }

        internal string ExpandTitle(string romPath)
        {
            return Expand(TitlePattern, romPath);
        }

        /// <summary>
        /// Gets the rom title by searching for an explicit capture in the RomRegex called "title".
        /// </summary>
        /// <param name="romPath"></param>
        /// <returns></returns>
        internal string ParseRomTitle(string romPath)
        {
            Regex reg = new Regex(RomRegex, RegexOptions.ExplicitCapture);
            Match m = reg.Match(romPath);
            if (m.Success)
            {
                return m.Groups["title"].Value;
            }
            else
                return Path.GetFileNameWithoutExtension(romPath);
        }

        /// <summary>
        /// Expands built-in variables in a string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="romPath"></param>
        /// <param name="romTitle"></param>
        /// <returns></returns>
        private string Expand(string str, string romPath)
        {
            string result = str
            .Replace("%E", Executable) // %E - Executable path
            .Replace("%F", Path.GetFileName(Executable)) // %F - Executable filename, no path
            .Replace("%f", Path.GetFileNameWithoutExtension(Executable)) // %f - Executable filename, no path or extension
            .Replace("%L", Path.GetDirectoryName(Executable)) // %L - Executable directory, no filename
            .Replace("%p", GetRelativePath(romPath, Path.GetDirectoryName(Executable))) // %p - rom's path relative to exe, using ../ as necessary
            .Replace("%R", GetRelativePath(romPath, RomBasePath)) // %R - Rom's path relative to the RomBasePath. Same as %r if there are no subfolders in RomBasePath
            .Replace("%r", Path.GetFileName(romPath)) // %r - Rom's filename, no path at all
            .Replace("%n", Path.GetFileNameWithoutExtension(romPath)) // %n - Rom's filename without extension
            .Replace("%D", GetParentDirectory(romPath)) // %D - Rom's directory, absolute path.
            .Replace("%d", Path.GetDirectoryName(romPath)) // %d - Rom's parent directory name. Same as RomBasePath if there are no subfolders
            .Replace("%B", RomBasePath) // %B - RomBasePath, from the emu config
            .Replace("%C", Category) // %C - Category, from the emu config
            // %I - Image path. Probably won't do this one
            // %i - Icon path. Probably won't do this one
            ;

            string romTitle = ParseRomTitle(romPath);
            result = result.Replace("%T", romTitle); // %T - Rom Title, parsed from RomRegex if a group is included to capture the title from the filename. Default is the same as %n.

            // When expanding the rompath, don't honor requests to put a rompath in the rompath
            if (romPath != null)
                result = result.Replace("%P", romPath); // %P - full rom path and filename

            return result;
        }

        /// <summary>
        /// Fills in empty/missing emulator configuration elements using the appropriate defaults
        /// from the application's configuration file.
        /// </summary>
        /// <param name="config"></param>
        internal void SetDefaults(AppConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(StartIn))
                StartIn = Path.GetDirectoryName(Executable);
            if (string.IsNullOrWhiteSpace(ImageBasePath))
                ImageBasePath = config.DefaultImagePath;
            if (string.IsNullOrWhiteSpace(ImageRegex))
                ImageRegex = config.DefaultImageRegex;
            if (string.IsNullOrWhiteSpace(RomBasePath))
                RomBasePath = config.DefaultRomPath;
            if (string.IsNullOrWhiteSpace(RomRegex))
                RomRegex = config.DefaultRomRegex;
        }

        /// <summary>
        /// Returns a relative path string from a full path based on a base path
        /// provided.
        /// </summary>
        /// <param name="fullPath">The path to convert. Can be either a file or a directory</param>
        /// <param name="basePath">The base path on which relative processing is based. Should be a directory.</param>
        /// <returns>
        /// String of the relative path.
        /// 
        /// Examples of returned values:
        ///  test.txt, ..\test.txt, ..\..\..\test.txt, ., .., subdir\test.txt
        /// </returns>
        public static string GetRelativePath(string fullPath, string basePath)
        {
            string separator = Path.DirectorySeparatorChar.ToString();

            // Require trailing backslash for path
            if (!basePath.EndsWith(separator))
                basePath += separator;

            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            
            // Uri's use forward slashes so convert back to backward slashes
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace("/", separator);

        }

        public static string GetParentDirectory(string fullPath)
        {
            int index = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
            if (index < 0)
                return fullPath;
            else
                return fullPath.Substring(index);
        }
}