using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace DhCodetaskExtension.ToolWindows
{
    [Guid(WindowGuidString)]
    public class MainToolWindow : ToolWindowPane
    {
        public const string WindowGuidString = "F1A2B3C4-D5E6-7890-ABCD-EF0123456789";
        public const string Title = "DH Codetask";
        public MainToolWindow(MainToolWindowState state) : base()
        {
            Caption = Title;
            BitmapImageMoniker = KnownMonikers.Extension;
            Content = new MainToolWindowControl(state);
        }
    }
}
