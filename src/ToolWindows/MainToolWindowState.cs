using VS2017ExtensionTemplate.Services;

namespace VS2017ExtensionTemplate.ToolWindows
{
    /// <summary>
    /// State object passed from MyPackage.InitializeToolWindowAsync
    /// into MainToolWindow constructor. Carries DTE and all services.
    ///
    /// HOW TO CUSTOMIZE:
    ///   Add properties here for any new services you create,
    ///   then populate them in MyPackage.InitializeToolWindowAsync.
    /// </summary>
    public class MainToolWindowState
    {
        public EnvDTE80.DTE2        DTE          { get; set; }
        public OutputWindowService  OutputWindow { get; set; }
        public StatusBarService     StatusBar    { get; set; }
        public ConfigurationService Config       { get; set; }
        public JsonConfigService    JsonConfig   { get; set; }
    }
}
