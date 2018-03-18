using Newtonsoft.Json;
using RetroSteam.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IWshRuntimeLibrary;

namespace RetroSteam
{
    internal class ConsoleProcessor
    {
        public static void Main(EmulatorConfiguration emulators, Options o)
        {
            if (emulators == null)
            {
                // Error out?
                Console.Error.WriteLine("Couldn't read emulator configuration from file " + o.EmulatorsFile);
                return;
            }
            SteamTools steam = new SteamTools();

            string shortcutsFile = o.ShortcutsFile;
            string user = null;
            if ("steam".Equals(o.Output))
            {
                if (String.IsNullOrWhiteSpace(shortcutsFile))
                {
                    // Load Steam shortcuts. SteamTools will worry about where the file is and how to read it
                    string steamBase = SteamTools.GetSteamLocation();
                    if (steamBase == null)
                    {
                        Console.Error.WriteLine("No Steam installation found.");
                        return;
                    }
                    Console.WriteLine($"Found Steam installed at '{steamBase}'", steamBase);

                    string[] users = SteamTools.GetUsers(steamBase);
                    if (users.Length == 0)
                    {
                        Console.Error.WriteLine("No Steam users found.");
                        return;
                    }
                    else if (users.Length > 1)
                    {
                        if (!string.IsNullOrWhiteSpace(o.SteamUser))
                        {

                            user = o.SteamUser;
                        }
                        else
                        {
                            Console.Error.WriteLine("More than 1 Steam user found and no Steam User found in command line options, quitting...");
                            return;
                        }
                    }
                    else
                    {
                        user = users[0];
                    }

                    Console.WriteLine($"Using Steam user '{user}'", user);

                    shortcutsFile = SteamTools.GetShortcutsFile(steamBase, user);
                }
                Console.WriteLine($"Loading Steam shortcuts file '{shortcutsFile}'", shortcutsFile);

                SteamShortcuts shortcuts = SteamTools.LoadShortcuts(shortcutsFile);

                // Analyze all emulator configs, read matching roms, create new shortcuts for each rom
                GenerateShortcutsFromRoms(emulators, shortcuts);
                // Don't do anything if Steam is going!
                if (steam.IsSteamRunning())
                {
                    Console.Error.WriteLine("Steam is running! Exit Steam before running this app.");
                    return;
                }

                SteamTools.WriteSteamShortcuts(shortcuts, shortcutsFile);
                SteamTools.AddGridImages(shortcuts, user);
                return;
            }
            else
            {
                ICollection<Shortcut> shortcuts;
                if (!string.IsNullOrWhiteSpace(o.Input))
                {
                    FileStream file = System.IO.File.OpenRead(o.Input);
                    // TODO: error checking on file open.

                    shortcuts = ReadShortcuts(file);
                    file.Close();
                }
                else
                {
                    shortcuts = new List<Shortcut>();

                    // Analyze all emulator configs, read matching roms, create new shortcuts for each rom
                    GenerateShortcutsFromRoms(emulators, shortcuts);
                }

                // Write the steam shortcuts back to the file
                if ("console".Equals(o.Output))
                {
                    PrintShortcuts(shortcuts);
                }
                else if (string.IsNullOrWhiteSpace(o.Output) || "none".Equals(o.Output))
                {
                    // do nothing
                }
                else
                {
                    // Anything else is a file or directory

                    // A path with an extension is hopefully a valid path to a file for saving the results
                    if (Path.HasExtension(o.Output))
                    {
                        try
                        {
                            string path = Path.GetFullPath(o.Output);
                            if (path == null) // I feel like this shouldn't happen, since it would throw an exception rather than returning null
                            {
                                Console.Error.WriteLine($"The output path '{o.Output}' is null, empty or not a valid windows path.");
                                return;
                            }
                            string filename = Path.GetFileName(path);
                            string dirname = Path.GetDirectoryName(path);
                            Directory.CreateDirectory(dirname);
                            StreamWriter outfile = System.IO.File.CreateText(path);

                            WriteShortcutsFile(shortcuts, outfile);
                            outfile.Close();
                        }
                        catch (System.Security.SecurityException)
                        {
                            Console.Error.WriteLine($"Security exception when trying to open file {o.Output} for writing.");
                            return;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Console.Error.WriteLine($"You don't have permission to open file {o.Output} for writing.");
                            return;
                        }
                        catch (ArgumentException)
                        {
                            Console.Error.WriteLine($"The output path '{o.Output}' is null, empty or not a valid windows path.");
                            return;
                        }
                        catch (PathTooLongException)
                        {
                            Console.Error.WriteLine($"The output path '{o.Output}' is too long.");
                            return;
                        }
                        catch (NotSupportedException e)
                        {
                            Console.Error.WriteLine($"Threw NotSupportedException when trying to open file at '{o.Output}', with this message: {e.Message}");
                            return;
                        }
                    }
                    else // otherwise we're looking at a directoy, where we spit out windows shortcuts
                    {
                        try
                        {
                            string path = Path.GetFullPath(o.Output);
                            if (path == null) // I feel like this shouldn't happen, since it would throw an exception rather than returning null
                            {
                                Console.Error.WriteLine($"The output path '{o.Output}' is null, empty or not a valid windows path.");
                                return;
                            }
                            DirectoryInfo dir = null;
                            try
                            {
                                dir = Directory.CreateDirectory(path);
                            }
                            catch (ArgumentException)
                            {
                                Console.Error.WriteLine($"The output directory '{o.Output}' is null, empty or not a valid windows path.");
                                return;
                            }
                            MakeWindowsShortcuts(shortcuts, dir);
                        }
                        catch (System.Security.SecurityException)
                        {
                            Console.Error.WriteLine($"Security exception when trying to write shortcuts to directory '{o.Output}'.");
                            return;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Console.Error.WriteLine($"You don't have permission to access directory '{o.Output}'.");
                            return;
                        }
                        catch (PathTooLongException)
                        {
                            Console.Error.WriteLine($"The output path '{o.Output}' is too long.");
                            return;
                        }
                        catch (NotSupportedException e)
                        {
                            Console.Error.WriteLine($"Threw NotSupportedException when trying to write shortcuts to directory '{o.Output}', with this message: {e.Message}");
                            return;
                        }
                    }
                }
            }
        }

        private static void PrintShortcuts(ICollection<Shortcut> shortcuts)
        {
            if (shortcuts != null)
            {
                Stream o = Console.OpenStandardOutput();
                StreamWriter outfile = new StreamWriter(o);
                foreach (Shortcut scut in shortcuts)
                {
                    outfile.WriteLine("      ID: {0}", scut.ID);
                    outfile.WriteLine("   Title: {0}", scut.Title);
                    outfile.WriteLine("  Target: {0} {1}", scut.Target, scut.LaunchOptions);
                    outfile.WriteLine("Start In: {0}", scut.StartIn);
                    outfile.WriteLine("   Icons: {0}", scut.GridImage);
                    outfile.Write("Categories: | ");
                    foreach (string cat in scut.Categories)
                        outfile.Write(cat + " | ");
                    outfile.WriteLine("\n");

                }
                outfile.Flush();
            }
        }

        private static WshShell shell = new WshShell();
        private static void CreateShortcut(string path, Shortcut scut)
        {
            string category = scut.Categories == null || scut.Categories.Count == 0 ? "" : scut.Categories[0];
            string title = scut.Title;

            string shortcutAddress = path + Path.DirectorySeparatorChar + $"{title}.lnk";

            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = $"Launch game '{title}' for platform '{category}'";
            if (scut.Target != null) shortcut.TargetPath = scut.Target;
            if (scut.LaunchOptions != null) shortcut.Arguments = scut.LaunchOptions;
            if (scut.StartIn != null) shortcut.WorkingDirectory = scut.StartIn;
            if (scut.Icon != null) shortcut.IconLocation = scut.Icon;
            shortcut.Save();
        }

        private static void MakeWindowsShortcuts(ICollection<Shortcut> shortcuts, DirectoryInfo dir)
        {
            if (shortcuts != null)
            {
                foreach (Shortcut scut in shortcuts)
                {
                    DirectoryInfo catDir = scut.Categories == null || scut.Categories.Count == 0 ? dir : dir.CreateSubdirectory(scut.Categories[0]);
                    CreateShortcut(catDir.FullName, scut);
                }
            }
        }

        private static void WriteShortcutsFile(ICollection<Shortcut> shortcuts, StreamWriter outfile)
        {
            if (shortcuts != null)
            {
                string json = JsonConvert.SerializeObject(shortcuts);
                outfile.Write(json);
                outfile.Flush();
            }
        }

        // Deserialize a JSON stream to a User object.  
        public static SteamShortcuts ReadShortcuts(FileStream file)
        {
            StreamReader infile = new StreamReader(file);
            string json = infile.ReadToEnd();
            SteamShortcuts shortcuts = JsonConvert.DeserializeObject<SteamShortcuts>(json);

            return shortcuts;
        }

        /// <summary>
        /// This method walks through every configured emulator and uses it to find roms and 
        /// turn those roms into shortcuts for Steam.
        /// Each rom will be turned into a shortcut where the Target is the configured emulator EXE,
        /// the Launch Options will be the configured emulator parameters (with substitutions), including
        /// rom file and any emulator start parameters like fullscreen mode or config files, and whose
        /// name is parsed from the rom regex grouping, or from the rom filename if no grouping is specified.
        /// Icons will also be assigned, once I figure out how that works. To start with, icons will be
        /// loaded from files using the image regex.
        /// </summary>
        /// <param name="emulators"></param>
        /// <param name="shortcuts"></param>
        private static void GenerateShortcutsFromRoms(EmulatorConfiguration emulators, ICollection<Shortcut> shortcuts)
        {
            foreach (Emulator emu in emulators)
            {
                GenerateSteamShortcutsFromRoms(emu, shortcuts);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="emulator"></param>
        /// <param name="shortcuts"></param>
        private static void GenerateSteamShortcutsFromRoms(Emulator emulator, ICollection<Shortcut> shortcuts)
        {
            string romBasePath = emulator.RomBasePath;

            Regex reg = new Regex(emulator.RomRegex);

            var files = Directory.GetFiles(emulator.RomBasePath, "*", SearchOption.AllDirectories)
                                      .Where(path => reg.IsMatch(path));

            foreach (string romPath in files)
            {
                Shortcut scut = GenerateSteamShortcutFromRom(emulator, romPath);
                shortcuts.Add(scut);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="emulator"></param>
        /// <param name="romPath"></param>
        /// <param name="romName"></param>
        private static Shortcut GenerateSteamShortcutFromRom(Emulator emulator, string romPath)
        {
            string parameters = emulator.ExpandParameters(romPath);

            string romName = emulator.ExpandTitle(romPath);

            string gridBasePath = emulator.ExpandGridBasePath(romPath);
            string gridRegex = string.IsNullOrWhiteSpace(emulator.GridRegex) ? null : emulator.ExpandGridRegex(romPath);
            string gridPath = GetImagePath(gridBasePath, gridRegex);

            string iconBasePath = emulator.ExpandIconBasePath(romPath);
            string iconRegex = string.IsNullOrWhiteSpace(emulator.IconRegex) ? null : emulator.ExpandIconRegex(romPath);
            string iconPath = GetImagePath(iconBasePath, iconRegex);

            string boxartBasePath = emulator.ExpandBoxartBasePath(romPath);
            string boxartRegex = string.IsNullOrWhiteSpace(emulator.BoxartRegex) ? null : emulator.ExpandBoxartRegex(romPath);
            string boxartPath = GetImagePath(boxartBasePath, gridRegex);

            //Console.WriteLine("[" + emulator.Category + "]\nEXE:\t" + emulator.Executable + "\nSTART:\t" + emulator.StartIn + "\nTITLE:\t" + romName + "\nPARAM:\t" + parameters + "\nIMAGE:\t" + imagePath);

            return SteamShortcuts.GenerateShortcut(emulator.Category, romName, emulator.Executable, emulator.StartIn, parameters, gridPath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBase"></param>
        /// <param name="imageRegex"></param>
        /// <returns></returns>
        private static string GetImagePath(string imageBase,  string imageRegex)
        {
            // First try using a straight file. If that isn't provided, do a regex search on the filesystem
            if (imageRegex != null)
            {
                return GetImageByRegex(imageBase, imageRegex);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBase"></param>
        /// <param name="imageRegex"></param>
        /// <returns></returns>
        private static string GetImageByRegex(string imageBase, string imageRegex)
        {
            Regex reg = new Regex(imageRegex);
            string file = Directory.GetFiles(imageBase)
                                   .Where(path => reg.IsMatch(path))
                                   .First();
            return file;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBase"></param>
        /// <param name="imageFile"></param>
        /// <returns></returns>
        private static string GetImageByFilename(string imageBase, string imageFile)
        {
            if (imageBase == null)
                return imageFile;
            else if (imageFile == null)
                return null;
            else
                return imageBase + Path.DirectorySeparatorChar + imageFile;
        }
    }
}