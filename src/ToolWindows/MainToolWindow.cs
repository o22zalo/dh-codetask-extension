using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace VS2017ExtensionTemplate.ToolWindows
{
    /// <summary>
    /// Main dockable tool window for the extension.
    /// Opens via: View > Other Windows > Extension Template
    ///
    /// HOW TO CUSTOMIZE:
    ///   - Change WindowGuidString to a newly generated GUID
    ///   - Change Title to your window name
    ///   - Change BitmapImageMoniker to any KnownMonikers icon
    /// </summary>
    [Guid(WindowGuidString)]
    public class MainToolWindow : ToolWindowPane
    {
        // TODO: Generate a new GUID for your tool window
        public const string WindowGuidString = "F1A2B3C4-D5E6-7890-ABCD-EF0123456789";
        public const string Title            = "Extension Template";

        /// <summary>
        /// The "state" parameter is the object returned from MyPackage.InitializeToolWindowAsync.
        /// </summary>
        public MainToolWindow(MainToolWindowState state) : base()
        {
            Caption            = Title;
            BitmapImageMoniker = KnownMonikers.Extension; // TODO: Choose your icon
            Content            = new MainToolWindowControl(state);
        }
    }
}
