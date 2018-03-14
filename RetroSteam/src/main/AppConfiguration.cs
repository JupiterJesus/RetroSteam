using System;
using System.IO;
using IniParser;
using IniParser.Model;

namespace RetroSteam
{
    internal class AppConfiguration
    {
        private IniData data;

        public AppConfiguration(IniData data)
        {
            this.data = data;
        }

        private const string SECTION_DEFAULTS = "Defaults";
        private const string SECTION_OPTIONS = "Options";

        private const string NAME_DEFAULT_IMAGE_REGEX = "DefaultImageRegex";
        private const string NAME_DEFAULT_ROM_REGEX   = "DefaultRomRegex";
        private const string NAME_DEFAULT_IMAGE_PATH  = "DefaultImagePath";
        private const string NAME_DEFAULT_ROM_PATH    = "DefaultRomPath";

        private const string NAME_OPTIONS_EMULATORS   = "EmulatorsFile";

        public string DefaultImagePath => this.data?[SECTION_DEFAULTS][NAME_DEFAULT_IMAGE_PATH];

        public string DefaultImageRegex => this.data?[SECTION_DEFAULTS][NAME_DEFAULT_IMAGE_REGEX];

        public string DefaultRomPath => this.data?[SECTION_DEFAULTS][NAME_DEFAULT_ROM_PATH];

        public string DefaultRomRegex => this.data?[SECTION_DEFAULTS][NAME_DEFAULT_ROM_REGEX];

        /// <summary>
        /// Loads default parameters for the app configuration.
        /// The default EmulatorsFilename is "emulators.ini".
        /// The default RomPath is a "roms" folder in your home directory (C:\Users\USER\roms or ~/roms)
        /// The default ImagePath is an "images" folder inside of the roms directory
        /// The default ImageRegex is a JPEG with the same path and name as the matching rom.
        /// The default RomRegex is any file directly inside the roms folder with an extension I recognize as a rom file,
        /// including bin, smc, sfc, fig, gen, nes, rpx, wud, gb, gbc, gba, nds, 3ds, n64, z64, v64, rom, iso, cso, wad, wbfs, and possibly more in the future.
        /// The rom name defaults to the base file name, anything preceding the extension and the dot.
        /// </summary>
        /// <returns></returns>
        internal static AppConfiguration GetDefault()
        {
            return new AppConfiguration(new IniData()).SetDefaults();
        }

        /// <summary>
        /// Read the configuration from an INI file and return it inside an AppConfiguration object.
        /// </summary>
        /// <param name="configFilename"></param>
        /// <returns></returns>
        internal static AppConfiguration Parse(string configFilename)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(configFilename);
            AppConfiguration config = new AppConfiguration(data);

            return config.SetDefaults();
        }

        private AppConfiguration SetDefaults()
        {
            string home = (Environment.OSVersion.Platform == PlatformID.Unix ||
               Environment.OSVersion.Platform == PlatformID.MacOSX)
            ? Environment.GetEnvironmentVariable("HOME")
            : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            if (data[SECTION_DEFAULTS] == null)
                data.Sections.AddSection(SECTION_DEFAULTS);
            if (data[SECTION_DEFAULTS][NAME_DEFAULT_ROM_PATH] == null)
                data[SECTION_DEFAULTS].AddKey(NAME_DEFAULT_ROM_PATH, home + Path.DirectorySeparatorChar + "roms");
            if (data[SECTION_DEFAULTS][NAME_DEFAULT_IMAGE_PATH] == null)
                data[SECTION_DEFAULTS].AddKey(NAME_DEFAULT_IMAGE_PATH, data[SECTION_DEFAULTS][NAME_DEFAULT_ROM_PATH] + Path.DirectorySeparatorChar + "images");
            if (data[SECTION_DEFAULTS][NAME_DEFAULT_IMAGE_REGEX] == null)
                data[SECTION_DEFAULTS].AddKey(NAME_DEFAULT_IMAGE_REGEX, "%I" + Path.DirectorySeparatorChar + "%r.(jpg|jpeg)");
            if (data[SECTION_DEFAULTS][NAME_DEFAULT_ROM_REGEX] == null)
                data[SECTION_DEFAULTS].AddKey(NAME_DEFAULT_ROM_REGEX, "(.+)\\.((bin)|(smc)|(sfc)|(fig)|(gen)|(nes)|(rpx)|(wud)|(gb)|(gbc)|(gba)|(nds)|(3ds)|(n64)|(z64)|(v64)|(rom)|(iso)|(cso)|(wad)|(wbfs))");

            return this;
        }

        /// <summary>
        /// Write the configuration to an INI file.
        /// </summary>
        /// <param name="configFilename"></param>
        internal void Save(string configFilename)
        {
            var parser = new FileIniDataParser();
            parser.WriteFile(configFilename, this.data);
        }
    }
}