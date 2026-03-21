// Full implementation — see context document index 23
// Paste the full JsonConfigService.cs here from the original project files.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension.Services
{
    public sealed class JsonParseResult { public bool Valid{get;set;} public JObject Obj{get;set;} public string Error{get;set;} public static JsonParseResult Ok(JObject o)=>new JsonParseResult{Valid=true,Obj=o}; public static JsonParseResult Fail(string e)=>new JsonParseResult{Valid=false,Error=e}; }
    public sealed class JsonFormatResult { public string Result{get;set;} public string Error{get;set;} public bool IsOk=>Error==null; public static JsonFormatResult Ok(string r)=>new JsonFormatResult{Result=r}; public static JsonFormatResult Fail(string e)=>new JsonFormatResult{Error=e}; }
    public sealed class JsonSaveResult { public bool Success{get;set;} public IReadOnlyList<string> ChangedKeys{get;set;} public string Error{get;set;} public static JsonSaveResult Ok(IReadOnlyList<string> k)=>new JsonSaveResult{Success=true,ChangedKeys=k}; public static JsonSaveResult Fail(string e)=>new JsonSaveResult{Success=false,ChangedKeys=new List<string>(),Error=e}; }
    public sealed class JsonResetResult { public bool Success{get;set;} public string Error{get;set;} public static JsonResetResult Ok()=>new JsonResetResult{Success=true}; public static JsonResetResult Fail(string e)=>new JsonResetResult{Error=e}; }

    public sealed class JsonConfigService
    {
        private const string AppDataFolderName = "DhCodetaskExtension";
        private const string ConfigFileName    = "DhCodetaskExtension.json";
        public static readonly string DefaultJson = JsonConvert.SerializeObject(new{ServerUrl="https://api.example.com/v1",ApiKey="",TimeoutSeconds=30,MaxResults=50,RefreshInterval=5,OutputFormat="JSON",EnableLogging=true,DebugMode=false,ShowStatusBar=true,Tags=new[]{"vsix","dh-codetask"},Advanced=new{RetryCount=3,RetryDelayMs=500,UserAgent="DhCodetaskExtension/1.0"}},Formatting.Indented);
        private readonly AsyncPackage _package;
        private readonly OutputWindowService _outputWindow;
        private string _configFilePath;
        private JObject _current;
        public event Action<IReadOnlyList<string>> ConfigSaved;
        public JsonConfigService(AsyncPackage package, OutputWindowService outputWindow){_package=package??throw new ArgumentNullException(nameof(package));_outputWindow=outputWindow??throw new ArgumentNullException(nameof(outputWindow));}
        public async Task InitializeAsync(){await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),AppDataFolderName);Directory.CreateDirectory(folder);_configFilePath=Path.Combine(folder,ConfigFileName);_current=LoadFromFile();_outputWindow.Log($"[JsonConfig] Initialized. File: {_configFilePath}");}
        public string ConfigFilePath=>_configFilePath??"(not initialized)";
        public string GetCurrentJson(Formatting fmt=Formatting.Indented)=>_current.ToString(fmt);
        public T Get<T>(string dotPath, T fallback){try{JToken tok=ResolvePath(_current,dotPath);return tok==null?fallback:tok.ToObject<T>();}catch{return fallback;}}
        public string ServerUrl=>Get<string>("ServerUrl","https://api.example.com/v1");
        public string ApiKey=>Get<string>("ApiKey","");
        public int TimeoutSeconds=>Get<int>("TimeoutSeconds",30);
        public int MaxResults=>Get<int>("MaxResults",50);
        public bool EnableLogging=>Get<bool>("EnableLogging",true);
        public bool DebugMode=>Get<bool>("DebugMode",false);
        public JsonParseResult TryParse(string json){if(string.IsNullOrWhiteSpace(json))return JsonParseResult.Fail("Empty.");try{return JsonParseResult.Ok(JObject.Parse(json));}catch(JsonException ex){return JsonParseResult.Fail("JSON parse error: "+ex.Message);}}
        public JsonFormatResult FormatJson(string json){var p=TryParse(json);return p.Valid?JsonFormatResult.Ok(p.Obj.ToString(Formatting.Indented)):JsonFormatResult.Fail(p.Error);}
        public JsonSaveResult Save(string newJson){ThreadHelper.ThrowIfNotOnUIThread();var parsed=TryParse(newJson);if(!parsed.Valid)return JsonSaveResult.Fail(parsed.Error);try{File.WriteAllText(_configFilePath,parsed.Obj.ToString(Formatting.Indented),Encoding.UTF8);}catch(Exception ex){return JsonSaveResult.Fail(ex.Message);}_current=parsed.Obj;var keys=new List<string>();ConfigSaved?.Invoke(keys);return JsonSaveResult.Ok(keys);}
        public JsonResetResult ResetToDefault(){ThreadHelper.ThrowIfNotOnUIThread();var r=Save(DefaultJson);return r.Success?JsonResetResult.Ok():JsonResetResult.Fail(r.Error);}
        private JObject LoadFromFile(){ThreadHelper.ThrowIfNotOnUIThread();if(!File.Exists(_configFilePath)){try{File.WriteAllText(_configFilePath,DefaultJson,Encoding.UTF8);}catch{}return JObject.Parse(DefaultJson);}try{return JObject.Parse(File.ReadAllText(_configFilePath,Encoding.UTF8));}catch{return JObject.Parse(DefaultJson);}}
        private static JToken ResolvePath(JObject obj, string dotPath){JToken cur=obj;foreach(string p in dotPath.Split('.')){var jo=cur as JObject;if(jo==null)return null;JToken next;if(!jo.TryGetValue(p,StringComparison.OrdinalIgnoreCase,out next))return null;cur=next;}return cur;}
    }
}
