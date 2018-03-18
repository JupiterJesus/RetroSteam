using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

namespace RetroSteam
{
    internal class EmulatorConfiguration : IEnumerable<Emulator>
    {
        private List<Emulator> emulators;
        private Dictionary<string, string> keys = new Dictionary<string, string>();

        /// <summary>
        /// Document this, here and in the actual code
        /// </summary>
        /// <param name="emulatorsFilename"></param>
        /// <returns></returns>
        internal static EmulatorConfiguration Parse(string emulatorsFilename, AppConfiguration appConfig)
        {
            // TODO: Should the defined variables be cleared between calls to parse?
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(emulatorsFilename);
            EmulatorConfiguration config = new EmulatorConfiguration
            {
                emulators = new List<Emulator>()
            };

            foreach (KeyData key in data.Global)
            {
                config.keys[key.KeyName] = config.ExpandVariables(key.Value, config.keys);
            }

            foreach (SectionData section in data.Sections)
            {
                Emulator emu = new Emulator
                {
                    Category = section.SectionName
                };

                // config.keys has global keys, and we have a local set of keys that has global keys and keys defined in this section
                Dictionary<string, string> emuKeys = new Dictionary<string, string>(config.keys);
                foreach (KeyData key in section.Keys)
                {
                    // see the documentation for Validate for more info on these attributes and what they should be
                    switch (key.KeyName)
                    {
                        case "Category":       if (!string.IsNullOrWhiteSpace(key.Value)) emu.Category = config.ExpandVariables(key.Value, emuKeys); break;
                        case "Platform":       emu.Executable = config.ExpandVariables(key.Value, emuKeys); break;
                        case "Executable":     emu.Executable = config.ExpandVariables(key.Value, emuKeys); break;
                        case "StartIn":        emu.StartIn = config.ExpandVariables(key.Value, emuKeys); break;
                        case "Parameters":     emu.Parameters = config.ExpandVariables(key.Value, emuKeys); break;
                        case "RomBasePath":    emu.RomBasePath = config.ExpandVariables(key.Value, emuKeys);    break;
                        case "TitlePattern":   emu.TitlePattern = config.ExpandVariables(key.Value, emuKeys); break;
                        case "RomRegex":       emu.RomRegex = config.ExpandVariables(key.Value, emuKeys);   break;
                        case "GridBasePath":   emu.GridBasePath = config.ExpandVariables(key.Value, emuKeys);  break;
                        case "GridRegex":      emu.GridRegex = config.ExpandVariables(key.Value, emuKeys); break;
                        case "IconBasePath":   emu.IconBasePath = config.ExpandVariables(key.Value, emuKeys); break;
                        case "IconRegex":      emu.IconRegex = config.ExpandVariables(key.Value, emuKeys); break;
                        case "BoxartBasePath": emu.BoxartBasePath = config.ExpandVariables(key.Value, emuKeys); break;
                        case "BoxartRegex":    emu.BoxartRegex = config.ExpandVariables(key.Value, emuKeys); break;
                        default: // Default is to add new key/value pairs for use elsewhere
                            emuKeys[key.KeyName] = config.ExpandVariables(key.Value, emuKeys);
                            break;
                    }
                }

                emu.SetDefaults(appConfig);
                emu.ExpandVariables();
                config.emulators.Add(emu);
            }
            Validate(config);
            return config;
        }

        /// <summary>
        /// Validates the emu config, ensuring that all required variables are provided,
        /// values are in the correct format and paths are valid. 
        /// This doesn't validate that the executable exists or that the parameters
        /// are valid for the executable, but paths must be valid.
        /// The values stored in each emulator will be validated.
        /// Any failures to valid will be thrown altogether in an exception.
        /// Also, ANY attributes that contain an unexpanded %{...} variable will result in a warning.
        /// 
        /// Category: (required) Replaces the emu category if it is not empty, otherwise the category is the section name. Should always be valid.
        ///           Error if missing (shouldn't be missing).
        ///           Error if contains characters not allowed in a file path.
        /// Platform: (optional) Should be a lowercase platform "code" like nes,snes,wii. For now I don't validate this value, just store it.
        ///           No errors or warnings.
        /// Executable: (required) An executable program. 
        ///             Error if missing.
        ///             Error if not a valid URI.
        ///             Warning if file doesn't exist.
        /// StartIn: (required) Technically optional but if missing it will be defaulted to the parent of the executable. This is the working directory for the executable and should be a valid URI. 
        ///          Error if missing (shouldn't be missing).
        ///          Error if not a valid URI.
        ///          Warning if directory doesn't exist.
        /// Parameters: (optional) The parameters to pass to the executable. Can be anything.
        ///          No errors or warnings.
        /// RomBasePath: (required) The directory that you keep all your roms in. RomRegex will match child paths to this directory.
        ///              Error if missing.
        ///              Error if not a valid path.
        ///              Error if directory doesn't exist.
        /// RomRegex: (required) Regular expression to match relative rom paths.
        ///           Error if missing.
        ///           Error if not a valid regular expression.
        /// TitlePattern: (optional) Text to use as each rom's title. There should be at least one rom-specific substitution parameter in it or every rom will have the same title!
        ///               If missing, defaults to %n.
        ///               Warning if no rom-specific substitutions are included - %P, %p, %R, %r, %n, %D, %d
        /// IconBasePath: (optional) The base directory that contains .ico or .exe files to use for rom shortcut icons.
        ///               Error if path not valid.
        ///               Error if directory doesn't exist.
        /// IconRegex: (optional) Regex file used to locate an icon for each rom. Can be the same icon for each rom, or use rom-specific substitution parameters to get different icons for each rom.
        ///            Error if path not valid.
        ///            Error if directory doesn't exist.
        /// GridBasePath: (optional) The base directory that contains image files to use for the steam grid.
        ///                Error if path not valid.
        ///                Error if directory doesn't exist.
        /// GridRegex: (optional) Regex file used to locate a steam grid image for each rom. Can be the same image for each rom, or use rom-specific substitution parameters to get different icons for each rom.
        ///            Error if not a valid regular expression.
        /// BoxartBasePath: (optional) The base directory that contains box art image files.
        ///                 Error if path not valid.
        ///                 Error if directory doesn't exist.
        /// BoxartRegex: (optional) Regex file used to locate the box art image for each rom. Can be the same image for each rom, or use rom-specific substitution parameters to get different icons for each rom.
        ///              Error if not a valid regular expression.
        /// </summary>
        /// <exception cref="ParseError">Errors and warnings indicated any part of the config that failed validation.</exception>
        /// <param name="config">Emulator config to validate.</param>
        private static void Validate(EmulatorConfiguration config)
        {
            // TODO: Validate parsed configuration
        }

        /// <summary>
        /// Expand variables defined in the INI files (not built-in variables).
        /// 
        /// For example,
        /// EMULATORS_PATH=E:\Emulation
        /// WII_PATH=%{EMULATORS_PATH}\wii
        /// Executable=%{WII_PATH}\emulators\dolphin\dolphin.exe
        /// </summary>
        internal string ExpandVariables(string value, Dictionary<string,string> keyValuePairs)
        {
            foreach (string key in keyValuePairs.Keys)
            {
                string var = "%{" + key + "}";
                value = value.Replace(var, keyValuePairs[key]);
            }
            return value;
        }

        public IEnumerator<Emulator> GetEnumerator()
        {
            return ((IEnumerable<Emulator>)emulators).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Emulator>)emulators).GetEnumerator();
        }
    }
}