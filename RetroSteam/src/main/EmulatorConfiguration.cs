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
                    switch (key.KeyName)
                    {
                        case "Executable":    emu.Executable = config.ExpandVariables(key.Value, emuKeys); break;
                        case "StartIn":       emu.StartIn = config.ExpandVariables(key.Value, emuKeys); break;
                        case "Parameters":    emu.Parameters = config.ExpandVariables(key.Value, emuKeys); break;
                        case "RomBasePath":   emu.RomBasePath = config.ExpandVariables(key.Value, emuKeys);    break;
                        case "TitlePattern": emu.TitlePattern = config.ExpandVariables(key.Value, emuKeys); break;
                        case "RomRegex":      emu.RomRegex = config.ExpandVariables(key.Value, emuKeys);   break;
                        case "ImageBasePath": emu.ImageBasePath = config.ExpandVariables(key.Value, emuKeys);  break;
                        case "ImageFile": emu.ImageFile = config.ExpandVariables(key.Value, emuKeys); break;
                        case "ImageRegex": emu.ImageRegex = config.ExpandVariables(key.Value, emuKeys); break;
                        case "IconFile": emu.IconFile = config.ExpandVariables(key.Value, emuKeys); break;
                        case "IconRegex": emu.IconRegex = config.ExpandVariables(key.Value, emuKeys); break;
                        case "IconBasePath": emu.IconBasePath = config.ExpandVariables(key.Value, emuKeys); break;
                        default: // Default is to add new key/value pairs for use elsewhere
                            emuKeys[key.KeyName] = config.ExpandVariables(key.Value, emuKeys);
                            break;
                    }
                }

                emu.SetDefaults(appConfig);
                emu.ExpandVariables();
                config.emulators.Add(emu);
            }
            return config;
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