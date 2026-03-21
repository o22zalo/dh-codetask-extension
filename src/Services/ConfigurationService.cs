// Full source: see context document index 1
// This is a stub — copy the full ConfigurationService.cs from the original project
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension.Services
{
    public sealed class ConfigurationService
    {
        private const string AppDataFolderName = "DhCodetaskExtension";
        private const string ConfigFileName    = "DhCodetaskExtension.config.xml";
        private const string RootElement       = "DhCodetaskExtensionConfig";
        public const string KeyServerUrl       = "ServerUrl";
        public const string KeyApiKey          = "ApiKey";
        public const string KeyTimeoutSeconds  = "TimeoutSeconds";
        public const string KeyMaxResults      = "MaxResults";
        public const string KeyEnableLogging   = "EnableLogging";
        public const string KeyOutputFormat    = "OutputFormat";
        public const string KeyRefreshInterval = "RefreshInterval";
        public const string KeyDebugMode       = "DebugMode";

        private static readonly Dictionary<string, object> Defaults = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            [KeyServerUrl]="https://api.example.com/v1",[KeyApiKey]="",[KeyTimeoutSeconds]=30,
            [KeyMaxResults]=50,[KeyEnableLogging]=true,[KeyOutputFormat]="JSON",[KeyRefreshInterval]=5,[KeyDebugMode]=false,
        };

        private readonly AsyncPackage _package;
        private readonly OutputWindowService _outputWindow;
        private readonly Dictionary<string, string> _store = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string _configFilePath;
        public event Action<string, string> ConfigChanged;

        public ConfigurationService(AsyncPackage package, OutputWindowService outputWindow)
        { _package=package??throw new ArgumentNullException(nameof(package)); _outputWindow=outputWindow??throw new ArgumentNullException(nameof(outputWindow)); }

        public async Task InitializeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolderName);
            Directory.CreateDirectory(folder);
            _configFilePath = Path.Combine(folder, ConfigFileName);
            ResetToDefaults(log: false);
            LoadFromFile();
            _outputWindow.Log($"[Config] Initialized. File: {_configFilePath}");
        }

        public string GetString(string key, string fallback="") { return _store.TryGetValue(key,out string v)?v:fallback; }
        public int GetInt(string key, int fallback=0) { return _store.TryGetValue(key,out string v)&&int.TryParse(v,out int r)?r:fallback; }
        public bool GetBool(string key, bool fallback=false) { return _store.TryGetValue(key,out string v)&&bool.TryParse(v,out bool r)?r:fallback; }
        public IReadOnlyDictionary<string,string> GetAll() => new Dictionary<string,string>(_store,StringComparer.OrdinalIgnoreCase);

        public void Set(string key, string value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _store[key]=value??string.Empty;
            ConfigChanged?.Invoke(key,value);
            SaveToFile();
        }
        public void Set(string key, int v) { ThreadHelper.ThrowIfNotOnUIThread(); Set(key,v.ToString()); }
        public void Set(string key, bool v) { ThreadHelper.ThrowIfNotOnUIThread(); Set(key,v.ToString()); }

        public void ResetToDefaults(bool log=true)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _store.Clear();
            foreach(var kv in Defaults) _store[kv.Key]=kv.Value?.ToString()??string.Empty;
            if(log){_outputWindow.Log("[Config] Reset to defaults.");SaveToFile();}
        }

        public void SaveToFile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(string.IsNullOrEmpty(_configFilePath))return;
            try{var root=new XElement(RootElement,new XAttribute("saved",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));foreach(var kv in _store)root.Add(new XElement("entry",new XAttribute("key",kv.Key),new XAttribute("value",kv.Value)));new XDocument(new XDeclaration("1.0","utf-8","yes"),root).Save(_configFilePath);}
            catch(Exception ex){_outputWindow.Log($"[Config] ERROR saving: {ex.Message}");}
        }

        private void LoadFromFile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(!File.Exists(_configFilePath)){_outputWindow.Log("[Config] File not found – using defaults.");return;}
            try{var doc=XDocument.Load(_configFilePath);int count=0;foreach(var el in doc.Root.Elements("entry")){string key=(string)el.Attribute("key");string val=(string)el.Attribute("value");if(!string.IsNullOrEmpty(key)){_store[key]=val??string.Empty;count++;}}
            _outputWindow.Log($"[Config] Loaded {count} entries.");}
            catch(Exception ex){_outputWindow.Log($"[Config] ERROR loading: {ex.Message}");}
        }

        public void DumpToOutput()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _outputWindow.Log($"[Config] ═══ Current config ({_store.Count} entries) ═══");
            foreach(var kv in _store){string display=kv.Key.Equals(KeyApiKey,StringComparison.OrdinalIgnoreCase)&&kv.Value.Length>4?"****"+kv.Value.Substring(kv.Value.Length-4):kv.Value;_outputWindow.Log($"[Config]   {kv.Key,-20} = {display}");}
        }

        public string ServerUrl       => GetString(KeyServerUrl,"https://api.example.com/v1");
        public string ApiKey          => GetString(KeyApiKey,"");
        public int    TimeoutSeconds  => GetInt(KeyTimeoutSeconds,30);
        public int    MaxResults      => GetInt(KeyMaxResults,50);
        public bool   EnableLogging   => GetBool(KeyEnableLogging,true);
        public string OutputFormat    => GetString(KeyOutputFormat,"JSON");
        public int    RefreshInterval => GetInt(KeyRefreshInterval,5);
        public bool   DebugMode       => GetBool(KeyDebugMode,false);
    }
}
