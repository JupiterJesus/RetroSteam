using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace RetroSteam
{
    internal class Program
    {
        #region Console control

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole(); // Create console window

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow(); // Get console window handle

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        static void ShowConsole()
        {
            var handle = GetConsoleWindow();
            if (handle == IntPtr.Zero)
                AllocConsole();
            else
                ShowWindow(handle, SW_SHOW);
        }

        static void HideConsole()
        {
            var handle = GetConsoleWindow();
            if (handle != null)
                ShowWindow(handle, SW_HIDE);
        }

        #endregion

        #region GUI
        
        public static Application WinApp { get; private set; }
        public static Window MainWindow { get; private set; }

        static void InitializeGui()
        {
            WinApp = new Application();
            WinApp.Run(MainWindow = new GuiWindow());
        }

        static void InitializeFrontend()
        {
            WinApp = new Application();
            WinApp.Run(MainWindow = new FrontendWindow());
        }

        #endregion

        /// <summary>
        /// This is main. Duh. This comment exists as a joke.
        /// </summary>
        /// <param name="args">Args are args</param>
        [STAThread]
        static void Main(string[] args)
        {
            // Parse args. All CLI args override the configuration loaded from the file
            Options o = ParseArgs(args);

            // Read configuration file, or get a new default configuration if no file exists
            AppConfiguration config = ParseConfig(o.ConfigFile, o);

            // Get the emulator configuration
            EmulatorConfiguration emulators = ParseEmulators(config, o);

            if (o.Frontend)
            {
                HideConsole();
                InitializeFrontend();
            }
            else if (o.Gui)
            {
                HideConsole();
                InitializeGui();
            }
            else
            {
                ShowConsole();
                ConsoleProcessor.Main(emulators, o);
                Console.ReadKey();
            }
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
