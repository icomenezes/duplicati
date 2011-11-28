﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Duplicati.Library.Interface;

namespace Duplicati.GUI.TrayIcon
{
    public static class Program
    {
        public static HttpServerConnection Connection;
        
        private const string TOOLKIT_OPTION = "toolkit";
        private const string TOOLKIT_WINDOWS_FORMS = "winforms";
        private const string TOOLKIT_GTK = "gtk";
        private const string TOOLKIT_GTK_APP_INDICATOR = "gtk-appindicator";
        private const string TOOLKIT_COCOA = "cocoa";
        
        private static readonly string DEFAULT_TOOLKIT = GetDefaultToolKit();
        
        private static string GetDefaultToolKit()
        {
            if (Duplicati.Library.Utility.Utility.IsClientOSX && SupportsCocoaStatusIcon)
            {
                return TOOLKIT_COCOA;
            }
            else if (Duplicati.Library.Utility.Utility.IsClientLinux)
            {
                if (SupportsAppIndicator)
                    return TOOLKIT_GTK_APP_INDICATOR;
                else
                    return TOOLKIT_GTK;
            }
            else
            {
                //Windows users expect a WinForms element
                return TOOLKIT_WINDOWS_FORMS;
            }
        }
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] _args)
        {
            List<string> args = new List<string>(_args);
            Dictionary<string, string> options = Duplicati.Library.Utility.CommandLineParser.ExtractOptions(args);
                     
            string toolkit;
            if (!options.TryGetValue(TOOLKIT_OPTION, out toolkit))
                toolkit = DEFAULT_TOOLKIT;
            else 
            {
                if (TOOLKIT_WINDOWS_FORMS.Equals(toolkit, StringComparison.InvariantCultureIgnoreCase))
                    toolkit = TOOLKIT_WINDOWS_FORMS;
                else if (TOOLKIT_GTK.Equals(toolkit, StringComparison.InvariantCultureIgnoreCase))
                    toolkit = TOOLKIT_GTK;
                else if (TOOLKIT_GTK_APP_INDICATOR.Equals(toolkit, StringComparison.InvariantCultureIgnoreCase))
                    toolkit = TOOLKIT_GTK_APP_INDICATOR;
                else if (TOOLKIT_COCOA.Equals(toolkit, StringComparison.InvariantCultureIgnoreCase))
                    toolkit = TOOLKIT_COCOA;
                else
                    toolkit = DEFAULT_TOOLKIT;
            }
            
            
            //TODO: Fix options for non-hosted
            using (new HostedInstanceKeeper(_args))
            {
                using (Connection = new HttpServerConnection(new Uri("http://localhost:8080/control.cgi"), null))
                {
                    using(var tk = RunTrayIcon(toolkit))
                    {
                        tk.Init(_args);
                    }
                }
            }
        }
  
        private static TrayIconBase RunTrayIcon(string toolkit)
        {
            if (toolkit == TOOLKIT_WINDOWS_FORMS)
                return GetWinformsInstance();
            else if (toolkit == TOOLKIT_GTK)
                return GetGtkInstance();
            else if (toolkit == TOOLKIT_GTK_APP_INDICATOR)
                return GetAppIndicatorInstance();
            else if (toolkit == TOOLKIT_COCOA)
                return GetCocoaRunnerInstance();
            else 
                throw new Exception(string.Format("The selected toolkit '{0}' is invalid", toolkit));
        }
        
        //We keep these in functions to avoid attempting to load the instance,
        // because the required assemblies may not exist on the machine 
        private static TrayIconBase GetGtkInstance() { return new GtkRunner(); }
        private static TrayIconBase GetWinformsInstance() { return new WinFormsRunner(); }
        private static TrayIconBase GetAppIndicatorInstance() { return new AppIndicatorRunner(); }
        private static TrayIconBase GetCocoaRunnerInstance() { return new CocoaRunner(); }
        
        //The functions below simply load the requested type,
        // and if the type is not present, calling the function will result in an exception.
        //This seems to be more reliable than attempting to load the assembly,
        // as there are too many complex rules for when an updated assembly is also
        // acceptable. This is fairly error proof, as it is just asks the runtime
        // to load the required types
        private static bool TryGetGtk()
        {
            return typeof(Gtk.StatusIcon) != null && typeof(Gdk.Image) != null;
        }

        private static bool TryGetWinforms()
        {
            return typeof(System.Windows.Forms.NotifyIcon) != null;
        }

        private static bool TryGetAppIndicator()
        {
            return typeof(AppIndicator.ApplicationIndicator) != null;
        }
        
        private static bool TryGetMonoMac()
        {
            return typeof(MonoMac.AppKit.NSStatusItem) != null;
        }
  
        //The functions below here, simply wrap the call to the above functions,
        // converting the exception to a simple boolean value, so the calling
        // code can be kept free of error handling
        private static bool SupportsGtk
        {
            get 
            {
                try { return TryGetGtk(); }
                catch {}
                
                return false;
            }
        }

        private static bool SupportsAppIndicator
        {
            get 
            {
                try { return TryGetAppIndicator(); }
                catch {}
                
                return false;
            }
        }

                private static bool SupportsCocoaStatusIcon
        {
            get 
            {
                try { return TryGetMonoMac(); }
                catch {}
                
                return false;
            }
        }
        
        private static bool SupportsWinForms
        {
            get 
            {
                try { return TryGetWinforms(); }
                catch {}
                
                return false;
            }
        }

        public static Duplicati.Library.Interface.ICommandLineArgument[] SupportedCommands
        {
            get
            {
                return new Duplicati.Library.Interface.ICommandLineArgument[]
                {
                    new Duplicati.Library.Interface.CommandLineArgument(TOOLKIT_OPTION, CommandLineArgument.ArgumentType.Enumeration, "Selects the toolkit to use", "Choose the toolkit used to generate the TrayIcon, note that it will fail if the selected toolkit is not supported on this machine", DEFAULT_TOOLKIT, null, new string[] {TOOLKIT_WINDOWS_FORMS, TOOLKIT_GTK, TOOLKIT_GTK_APP_INDICATOR, TOOLKIT_COCOA}),
                };
            }
        }
    }
}
