using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using RetroSteam.Steam;

namespace RetroSteam
{
    class Program
    {
        /// <summary>
        /// This is main. Duh. This comment exists as a joke.
        /// </summary>
        /// <param name="args">Args are args</param>
        static void Main(string[] args)
        {
            // Don't do anything if Steam is going!
            if (SteamTools.IsSteamRunning())
            {
                Console.Error.WriteLine("Steam is running! Exit Steam before running this app.");
                return;
            }

            // Parse args. All CLI args override the configuration loaded from the file
            Options o = ParseArgs(args);

            // Read configuration file, or get a new default configuration if no file exists
            AppConfiguration config = ParseConfig(o.ConfigFile, o);

            // Get the emulator configuration
            EmulatorConfiguration emulators = ParseEmulators(config, o);
            if (emulators == null)
            {
                // Error out?
                Console.Error.WriteLine("Couldn't read emulator configuration from file " + o.EmulatorsFile);
                return;
            }

            string shortcutsFile = o.ShortcutsFile;
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
                string user = null;
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
            GenerateSteamShortcutsFromRoms(emulators, shortcuts);

            // Write the steam shortcuts back to the file
            if (o.DryRun)
                SteamTools.PrintShortcuts(shortcuts);
            else
                SteamTools.WriteShortcuts(shortcuts, shortcutsFile);
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
        private static void GenerateSteamShortcutsFromRoms(EmulatorConfiguration emulators, SteamShortcuts shortcuts)
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
        private static void GenerateSteamShortcutsFromRoms(Emulator emulator, SteamShortcuts shortcuts)
        {
            string romBasePath = emulator.RomBasePath;
            
            Regex reg = new Regex(emulator.RomRegex);

            var files = Directory.GetFiles(emulator.RomBasePath, "*", SearchOption.AllDirectories)
                                      .Where(path => reg.IsMatch(path));
            
            foreach (string romPath in files)
            {
                //string romName = emulator.ParseRomTitle(romPath);
                SteamShortcut scut = GenerateSteamShortcutFromRom(emulator, romPath);
                shortcuts.AddShortcut(scut);
                SteamTools.AddGridImage(scut.GridImage, scut.SteamID);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="emulator"></param>
        /// <param name="romPath"></param>
        /// <param name="romName"></param>
        private static SteamShortcut GenerateSteamShortcutFromRom(Emulator emulator, string romPath)
        {
            string romName = emulator.ExpandTitle(romPath);
            string parameters = emulator.ExpandParameters(romPath, romName);

            string imageBasePath = emulator.ExpandImageBasePath(romPath, romName);
            string imageRegex = string.IsNullOrWhiteSpace(emulator.ImageRegex) ? null : emulator.ExpandImageRegex(romPath, romName);
            string imageFile = string.IsNullOrWhiteSpace(emulator.ImageFile) ? null : emulator.ExpandImageFile(romPath, romName);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="emulatorsFilename"></param>
        /// <returns></returns>
        private static EmulatorConfiguration ParseEmulators(AppConfiguration config, Options options)
        {
            if (File.Exists(options.EmulatorsFile))
            {
                return EmulatorConfiguration.Parse(options.EmulatorsFile, config);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configFilename"></param>
        /// <returns></returns>
        private static AppConfiguration ParseConfig(string configFilename, Options cli)
        {
            AppConfiguration config;
            if (File.Exists(configFilename))
            {
                config = AppConfiguration.Parse(configFilename);
            }
            else
            {
                config = AppConfiguration.GetDefault();
                return config;
            }
            
            return config;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="config"></param>
        private static Options ParseArgs(string[] args)
        {
            var options = new Options();
            var isValid = CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
            if (isValid)
                return options;
            else
                return null;
        }
    }
}
