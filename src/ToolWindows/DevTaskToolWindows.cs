using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace DhCodetaskExtension.ToolWindows
{
    [Guid(WindowGuidString)]
    public class TrackerToolWindow : ToolWindowPane
    {
        public const string WindowGuidString = "A1B2C3D4-1111-2222-3333-EF0123456789";
        public const string Title = "DevTask Tracker";

        public TrackerToolWindow(object state) : base()
        {
            Caption = Title;
            BitmapImageMoniker = KnownMonikers.Task;
        }

        public void SetContent(System.Windows.UIElement control)
        {
            Content = control;
        }
    }

    [Guid(WindowGuidString)]
    public class HistoryToolWindow : ToolWindowPane
    {
        public const string WindowGuidString = "B2C3D4E5-2222-3333-4444-F01234567890";
        public const string Title = "Task History";

        public HistoryToolWindow(object state) : base()
        {
            Caption = Title;
            BitmapImageMoniker = KnownMonikers.History;
        }

        public void SetContent(System.Windows.UIElement control)
        {
            Content = control;
        }
    }
}
