using DhCodetaskExtension.Services;

namespace DhCodetaskExtension.ToolWindows
{
    public class MainToolWindowState
    {
        public EnvDTE80.DTE2        DTE          { get; set; }
        public OutputWindowService  OutputWindow { get; set; }
        public StatusBarService     StatusBar    { get; set; }
        public ConfigurationService Config       { get; set; }
        public JsonConfigService    JsonConfig   { get; set; }
    }
}
