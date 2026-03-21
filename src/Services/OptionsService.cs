using System;
using System.ComponentModel;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace DhCodetaskExtension.Services
{
    public class SampleOptionsPage : DialogPage
    {
        [Category("Connection")] [DisplayName("Server URL")] [Description("URL của backend server.")]
        public string ServerUrl { get; set; } = "https://api.example.com";
        [Category("Behavior")] [DisplayName("Enable Debug Logging")] [Description("Log debug vào Output pane.")]
        public bool EnableDebugLog { get; set; } = false;
    }

    public sealed class OptionsService
    {
        private readonly AsyncPackage _package;
        private readonly IServiceProvider _sp;
        public OptionsService(AsyncPackage package) { _package = package ?? throw new ArgumentNullException(nameof(package)); _sp = package; }
        public SampleOptionsPage GetPage() => (SampleOptionsPage)_package.GetDialogPage(typeof(SampleOptionsPage));
        public string ServerUrl => GetPage().ServerUrl;
        public bool EnableDebugLog => GetPage().EnableDebugLog;
    }
}
