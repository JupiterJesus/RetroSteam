using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetroSteam
{
    class Options
    {
        [Option('e', "emulators", Required = false, DefaultValue = "emulators.ini", HelpText = @"INI file containing emulator configuration. Default is .\emulators.ini")]
        public string EmulatorsFile { get; set; }

        [Option('c', "config", Required = false, DefaultValue = "config.ini", HelpText = @"INI file containing the app configuration. Default is .\config.ini")]
        public string ConfigFile { get; set; }

        [Option('s', "shortcuts", Required = false, DefaultValue = null, HelpText = "Force app to use a specific shortcuts.vdf file instead of finding it in your Steam installation.")]
        public string ShortcutsFile { get; set; }

        [Option('d', "dry", Required = false, DefaultValue = true, HelpText = "Select to only do a dry run, printing everything to the console instead of writing to the shortcuts file. Default is true to make sure you don't run until you are sure you're ready!")]
        public bool DryRun { get; set; }

        [Option('u', "user", Required = false, DefaultValue = null, HelpText = "User ID of the Steam User to use for this run. Not necessary if there's only one user, but required if there's more than one!")]
        public string SteamUser { get; set; }

        [HelpOption(HelpText = "Display this help screen.")]

        public string GetUsage()
        {
            var usage = new StringBuilder();
            usage.AppendLine("RetroSteam");
            usage.AppendLine("Read emulators.ini for help running the app.");
            return usage.ToString();
        }

    }
}
