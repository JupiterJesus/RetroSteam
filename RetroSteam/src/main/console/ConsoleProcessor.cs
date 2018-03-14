using Newtonsoft.Json;
using RetroSteam.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
                    FileStream file = File.OpenRead(o.Input);
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
                else if (!string.IsNullOrWhiteSpace(o.Output))
                {
                    FileStream outfile = null;
                    try
                    {
                        outfile = File.OpenWrite(o.Output);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Console.Error.WriteLine($"The directory of your output file {o.Output} doesn't exist.");
                        return;
                    }
                    catch (FileNotFoundException)
                    {
                        Console.Error.WriteLine($"Couldn't open file {o.Output} for writing.");
                        return;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.Error.WriteLine($"You don't have permission to open file {o.Output} for writing.");
                        return;
                    }
                    WriteShortcuts(shortcuts, outfile);
                    outfile.Close();
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

        private static void WriteShortcuts(ICollection<Shortcut> shortcuts, FileStream file)
        {
            if (shortcuts != null)
            {
                StreamWriter outfile = new StreamWriter(file);
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
            string imageBasePath = emulator.ExpandImageBasePath(romPath);
            string imageRegex = string.IsNullOrWhiteSpace(emulator.ImageRegex) ? null : emulator.ExpandImageRegex(romPath);
            string imageFile = string.IsNullOrWhiteSpace(emulator.ImageFile) ? null : emulator.ExpandImageFile(romPath);
            string imagePath = GetImagePath(imageBasePath, imageFile, imageRegex);

            //Console.WriteLine("[" + emulator.Category + "]\nEXE:\t" + emulator.Executable + "\nSTART:\t" + emulator.StartIn + "\nTITLE:\t" + romName + "\nPARAM:\t" + parameters + "\nIMAGE:\t" + imagePath);

            return SteamShortcuts.GenerateShortcut(emulator.Category, romName, emulator.Executable, emulator.StartIn, parameters, imagePath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBase"></param>
        /// <param name="imageFile"></param>
        /// <param name="imageRegex"></param>
        /// <returns></returns>
        private static string GetImagePath(string imageBase, string imageFile, string imageRegex)
        {
            // First try using a straight file. If that isn't provided, do a regex search on the filesystem
            if (imageFile != null)
            {
                return GetImageByFilename(imageBase, imageFile);
            }
            else if (imageRegex != null)
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