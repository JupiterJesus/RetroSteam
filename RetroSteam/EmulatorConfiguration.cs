using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace RetroSteam
{
    internal class EmulatorConfiguration : IEnumerable<Emulator>
    {
        private List<Emulator> emulators;

        /// <summary>
        /// Document this, here and in the actual code
        /// </summary>
        /// <param name="emulatorsFilename"></param>
        /// <returns></returns>
        internal static EmulatorConfiguration Parse(string emulatorsFilename, AppConfiguration appConfig)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(emulatorsFilename);
            EmulatorConfiguration config = new EmulatorConfiguration();

            config.emulators = new List<Emulator>();
            foreach (SectionData section in data.Sections)
            {
                Emulator emu = new Emulator();
                emu.Category = section.SectionName;

                foreach (KeyData key in section.Keys)
                {
                    switch (key.KeyName)
                    {
                        case "Executable":    emu.Executable = key.Value; break;
                        case "StartIn":       emu.StartIn = key.Value; break;
                        case "Parameters":    emu.Parameters = key.Value; break;
                        case "RomBasePath":   emu.RomBasePath = key.Value;    break;
                        case "RomRegex":      emu.RomRegex = key.Value;   break;
                        case "ImageBasePath": emu.ImageBasePath = key.Value;  break;
                        case "ImageFile":     emu.ImageFile = key.Value; break;
                        case "ImageRegex":    emu.ImageRegex = key.Value; break;
                        case "TitlePattern":   emu.TitlePattern = key.Value; break;
                        default: break;
                    }
                }

                emu.SetDefaults(appConfig);
                emu.ExpandVariables();
                config.emulators.Add(emu);
            }
            return config;
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