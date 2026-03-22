using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DhCodetaskExtension.Core.Models;
using DhCodetaskExtension.Core.Services;
using Newtonsoft.Json.Linq;

namespace DhCodetaskExtension.ViewModels
{
    public enum ProjectSortMode
    {
        NameAsc,
        NameDesc,
        DateDesc,
        DateAsc
    }

    public enum ProjectFileType
    {
        All,
        SlnOnly,
        CsprojOnly
    }

    public sealed class ProjectHelperViewModel : INotifyPropertyChanged
    {
        private readonly SolutionFileService _service;
        private readonly Func<AppSettings>   _getSettings;
        private readonly Action<string>      _log;

        private List<SolutionFileEntry> _allFiles = new List<SolutionFileEntry>();

        // ── File list state ───────────────────────────────────────────────

        // FilterKeyword does NOT auto-trigger ApplyFilter — use FilterCommand / Enter
        private string          _filterKeyword  = string.Empty;
        private ProjectSortMode _sortMode        = ProjectSortMode.NameAsc;
        private ProjectFileType _fileType        = ProjectFileType.All;
        private bool            _isLoading;
        private string          _statusMessage   = "Nhấn 🔄 để quét thư mục.";

        // ── Panel mode (v3.6) ─────────────────────────────────────────────

        private bool _isFileMode = true;

        public bool IsFileMode
        {
            get => _isFileMode;
            set { _isFileMode = value; OnProp(nameof(IsFileMode)); OnProp(nameof(IsSearchMode)); }
        }

        public bool IsSearchMode => !_isFileMode;

        // ── Properties ────────────────────────────────────────────────────

        public string FilterKeyword
        {
            get => _filterKeyword;
            set { _filterKeyword = value; OnProp(nameof(FilterKeyword)); }
            // NOTE: intentionally does NOT call ApplyFilter() on set.
            // User must press the Filter button or Enter to apply.
        }

        public ProjectSortMode SortMode
        {
            get => _sortMode;
            set { _sortMode = value; OnProp(nameof(SortMode)); ApplyFilter(); RaiseSortFlags(); }
        }

        public ProjectFileType FileType
        {
            get => _fileType;
            set { _fileType = value; OnProp(nameof(FileType)); ApplyFilter(); RaiseTypeFlags(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnProp(nameof(IsLoading)); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnProp(nameof(StatusMessage)); }
        }

        // Sort flag helpers (for RadioButton TwoWay binding)
        public bool SortNameAsc  { get => SortMode == ProjectSortMode.NameAsc;  set { if (value) SortMode = ProjectSortMode.NameAsc; } }
        public bool SortNameDesc { get => SortMode == ProjectSortMode.NameDesc; set { if (value) SortMode = ProjectSortMode.NameDesc; } }
        public bool SortDateDesc { get => SortMode == ProjectSortMode.DateDesc; set { if (value) SortMode = ProjectSortMode.DateDesc; } }
        public bool SortDateAsc  { get => SortMode == ProjectSortMode.DateAsc;  set { if (value) SortMode = ProjectSortMode.DateAsc; } }

        // Type flag helpers
        public bool TypeAll     { get => FileType == ProjectFileType.All;        set { if (value) FileType = ProjectFileType.All; } }
        public bool TypeSln     { get => FileType == ProjectFileType.SlnOnly;    set { if (value) FileType = ProjectFileType.SlnOnly; } }
        public bool TypeCsproj  { get => FileType == ProjectFileType.CsprojOnly; set { if (value) FileType = ProjectFileType.CsprojOnly; } }

        public ObservableCollection<SolutionFileEntry> Files { get; }
            = new ObservableCollection<SolutionFileEntry>();

        // ── Ripgrep content search ─────────────────────────────────────────

        private string _contentQuery   = string.Empty;
        private bool   _isSearching;
        private string _searchStatus   = string.Empty;
        private CancellationTokenSource _searchCts;

        public string ContentQuery
        {
            get => _contentQuery;
            set { _contentQuery = value; OnProp(nameof(ContentQuery)); }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set { _isSearching = value; OnProp(nameof(IsSearching)); }
        }

        public string SearchStatus
        {
            get => _searchStatus;
            set { _searchStatus = value; OnProp(nameof(SearchStatus)); }
        }

        public ObservableCollection<RipgrepSearchResult> SearchResults { get; }
            = new ObservableCollection<RipgrepSearchResult>();

        // ── Commands ──────────────────────────────────────────────────────
        public ICommand RefreshCommand       { get; }
        public ICommand FilterCommand        { get; }   // v3.6: button-triggered filter
        public ICommand ClearFilterCommand   { get; }
        public ICommand SearchContentCommand { get; }
        public ICommand CancelSearchCommand  { get; }
        public ICommand ClearSearchCommand   { get; }
        public RelayCommand<SolutionFileEntry>   OpenInVsCommand   { get; }
        public RelayCommand<SolutionFileEntry>   CopyPathCommand   { get; }
        public RelayCommand<SolutionFileEntry>   OpenFolderCommand { get; }
        public RelayCommand<RipgrepSearchResult> OpenSearchResultCommand { get; }

        // ── Injected actions ──────────────────────────────────────────────
        public Action<string> OpenFileAction   { get; set; }
        public Action<string> CopyToClipboard  { get; set; }
        public Action<string> OpenFolderAction { get; set; }

        public ProjectHelperViewModel(SolutionFileService service, Func<AppSettings> getSettings, Action<string> log)
        {
            _service     = service     ?? throw new ArgumentNullException(nameof(service));
            _getSettings = getSettings ?? throw new ArgumentNullException(nameof(getSettings));
            _log         = log         ?? (_ => { });

            RefreshCommand     = new RelayCommand(RefreshFireAndForget);
            FilterCommand      = new RelayCommand(ApplyFilter);
            ClearFilterCommand = new RelayCommand(() => { FilterKeyword = string.Empty; ApplyFilter(); });

            SearchContentCommand = new RelayCommand(
                () => { var _ = SearchContentAsync(); },
                () => !IsSearching && !string.IsNullOrWhiteSpace(ContentQuery));

            CancelSearchCommand = new RelayCommand(
                CancelSearch,
                () => IsSearching);

            ClearSearchCommand = new RelayCommand(() =>
            {
                ContentQuery = string.Empty;
                SearchResults.Clear();
                SearchStatus = string.Empty;
            });

            OpenInVsCommand = new RelayCommand<SolutionFileEntry>(entry =>
            {
                if (entry == null) return;
                _log(string.Format("[ProjectHelper] 📂 Open: {0}", entry.FullPath));
                OpenFileAction?.Invoke(entry.FullPath);
            });

            CopyPathCommand = new RelayCommand<SolutionFileEntry>(entry =>
            {
                if (entry == null) return;
                _log(string.Format("[ProjectHelper] 📋 Copy path: {0}", entry.FullPath));
                CopyToClipboard?.Invoke(entry.FullPath);
            });

            OpenFolderCommand = new RelayCommand<SolutionFileEntry>(entry =>
            {
                if (entry == null) return;
                var dir = System.IO.Path.GetDirectoryName(entry.FullPath) ?? string.Empty;
                _log(string.Format("[ProjectHelper] 📁 Open folder: {0}", dir));
                OpenFolderAction?.Invoke(dir);
            });

            OpenSearchResultCommand = new RelayCommand<RipgrepSearchResult>(r =>
            {
                if (r == null || string.IsNullOrEmpty(r.FilePath)) return;
                _log(string.Format("[ProjectHelper] 🔍 Open search result: {0}:{1}", r.FilePath, r.LineNumber));
                OpenFileAction?.Invoke(r.FilePath);
            });
        }

        // ── Refresh ───────────────────────────────────────────────────────

        private void RefreshFireAndForget() { var _ = RefreshAsync(); }

        public async Task RefreshAsync(bool forceRescan = false)
        {
            IsLoading     = true;
            StatusMessage = "⏳ Đang quét...";
            _log("[ProjectHelper] 🔍 Refreshing file list...");
            try
            {
                if (forceRescan || _service.IsCacheExpired())
                    await _service.RefreshAsync();

                _allFiles = await _service.GetFilesAsync();
                var age = _service.GetCacheAgeDisplay();
                _log(string.Format("[ProjectHelper] ✅ Found {0} file(s). Cache age: {1}",
                    _allFiles.Count, age ?? "fresh"));
                ApplyFilter();
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ " + ex.Message;
                _log("[ProjectHelper] ❌ " + ex.Message);
            }
            finally { IsLoading = false; }
        }

        // ── Filter + Sort ─────────────────────────────────────────────────

        public void ApplyFilter()
        {
            var src = _allFiles.AsEnumerable();

            switch (FileType)
            {
                case ProjectFileType.SlnOnly:    src = src.Where(f => f.Extension == ".sln");    break;
                case ProjectFileType.CsprojOnly: src = src.Where(f => f.Extension == ".csproj"); break;
            }

            if (!string.IsNullOrWhiteSpace(FilterKeyword))
            {
                var kw = FilterKeyword.ToLower();
                src = src.Where(f =>
                    f.FileName.ToLower().Contains(kw) ||
                    f.RelativePath.ToLower().Contains(kw));
            }

            switch (SortMode)
            {
                case ProjectSortMode.NameAsc:  src = src.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase); break;
                case ProjectSortMode.NameDesc: src = src.OrderByDescending(f => f.FileName, StringComparer.OrdinalIgnoreCase); break;
                case ProjectSortMode.DateDesc: src = src.OrderByDescending(f => f.LastModified); break;
                case ProjectSortMode.DateAsc:  src = src.OrderBy(f => f.LastModified); break;
            }

            var result = src.ToList();
            Files.Clear();
            foreach (var f in result) Files.Add(f);

            var age = _service.GetCacheAgeDisplay();
            var ageInfo = age != null ? string.Format(" (cache: {0} trước)", age) : string.Empty;
            StatusMessage = result.Count == 0
                ? (_allFiles.Count == 0
                    ? "Chưa quét. Cấu hình DirectoryRootDhHosCodePath và nhấn 🔄."
                    : "Không tìm thấy file nào khớp.")
                : string.Format("{0} file{1} — {2:HH:mm:ss}", result.Count, ageInfo, DateTime.Now);
        }

        private void RaiseSortFlags()
        {
            OnProp(nameof(SortNameAsc)); OnProp(nameof(SortNameDesc));
            OnProp(nameof(SortDateDesc)); OnProp(nameof(SortDateAsc));
        }

        private void RaiseTypeFlags()
        {
            OnProp(nameof(TypeAll)); OnProp(nameof(TypeSln)); OnProp(nameof(TypeCsproj));
        }

        // ── Ripgrep content search ────────────────────────────────────────

        private void CancelSearch()
        {
            try { _searchCts?.Cancel(); }
            catch { }
            _log("[ProjectHelper] 🔍 Search cancelled.");
        }

        private async Task SearchContentAsync()
        {
            if (string.IsNullOrWhiteSpace(ContentQuery)) return;

            var settings = _getSettings();
            var rgPath   = settings.RipgrepPath?.Trim() ?? string.Empty;
            var root     = settings.DirectoryRootDhHosCodePath?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(rgPath) || !File.Exists(rgPath))
            {
                SearchStatus = "❌ ripgrep không tìm thấy. Cấu hình RipgrepPath trong Settings (JSON).";
                _log("[ProjectHelper] ❌ RipgrepPath not set or file not found: " + rgPath);
                return;
            }

            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                SearchStatus = "❌ DirectoryRootDhHosCodePath không hợp lệ.";
                return;
            }

            try { _searchCts?.Cancel(); _searchCts?.Dispose(); }
            catch { }
            _searchCts = new CancellationTokenSource();
            var ct = _searchCts.Token;

            IsSearching  = true;
            SearchStatus = string.Format("⏳ Đang tìm: \"{0}\"...", ContentQuery);
            SearchResults.Clear();
            (SearchContentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelSearchCommand  as RelayCommand)?.RaiseCanExecuteChanged();

            _log(string.Format("[ProjectHelper] 🔍 ripgrep: \"{0}\" in {1}", ContentQuery, root));

            try
            {
                var results = await Task.Run(() => RunRipgrep(rgPath, ContentQuery, root, ct), ct);

                if (!ct.IsCancellationRequested)
                {
                    foreach (var r in results) SearchResults.Add(r);
                    SearchStatus = results.Count == 0
                        ? string.Format("🔍 Không tìm thấy kết quả cho \"{0}\"", ContentQuery)
                        : string.Format("✅ {0} kết quả cho \"{1}\"", results.Count, ContentQuery);
                    _log(string.Format("[ProjectHelper] ✅ Search done: {0} result(s)", results.Count));
                }
                else
                {
                    SearchStatus = "⏸ Đã hủy tìm kiếm.";
                }
            }
            catch (OperationCanceledException)
            {
                SearchStatus = "⏸ Đã hủy tìm kiếm.";
            }
            catch (Exception ex)
            {
                SearchStatus = "❌ Lỗi: " + ex.Message;
                _log("[ProjectHelper] ❌ Search error: " + ex.Message);
            }
            finally
            {
                IsSearching = false;
                (SearchContentCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (CancelSearchCommand  as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Runs ripgrep with --json output.
        /// Strategy: try direct first; if Win32Exception (architecture mismatch — VS2017
        /// devenv.exe is 32-bit but rg.exe may be 64-bit), retry via cmd.exe which is
        /// 64-bit on x64 Windows and can launch 64-bit executables.
        /// </summary>
        private static List<RipgrepSearchResult> RunRipgrep(
            string rgPath, string pattern, string root, CancellationToken ct)
        {
            // Build inner arguments (used by both direct and cmd approaches)
            var safePattern = pattern.Replace("\"", "\\\"");
            var safeRoot    = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var rgArgs = string.Format(
                "--json --line-number --max-count 500 \"{0}\" \"{1}\"",
                safePattern, safeRoot);

            // 1. Try direct execution
            try
            {
                var psi = new ProcessStartInfo(rgPath, rgArgs)
                {
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                return ParseRipgrepOutput(psi, ct);
            }
            catch (System.ComponentModel.Win32Exception ex)
                when (ex.NativeErrorCode == 193 || ex.NativeErrorCode == 216 || ex.NativeErrorCode == 14001)
            {
                // 193 = ERROR_BAD_EXE_FORMAT  (e.g. 64-bit exe launched from 32-bit host)
                // 216 = ERROR_EXE_MACHINE_TYPE_MISMATCH
                // 14001 = side-by-side configuration error
                // Fall through to cmd.exe fallback below
            }

            // 2. Fallback: launch via cmd.exe (64-bit on x64 Windows) to bypass
            //    architecture restriction of 32-bit VS2017 host process.
            //    cmd /c  ""path\rg.exe"  args"
            //    Outer quotes are required by cmd /c for commands containing spaces.
            var cmdInner = string.Format("\"{0}\" {1}", rgPath, rgArgs);
            var cmdArgs  = string.Format("/c \"{0}\"", cmdInner);

            var cmdPsi = new ProcessStartInfo("cmd.exe", cmdArgs)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            return ParseRipgrepOutput(cmdPsi, ct);
        }

        /// <summary>Starts the process and parses ripgrep --json output line by line.</summary>
        private static List<RipgrepSearchResult> ParseRipgrepOutput(
            ProcessStartInfo psi, CancellationToken ct)
        {
            var results = new List<RipgrepSearchResult>();

            using (var proc = Process.Start(psi))
            {
                if (proc == null) return results;

                ct.Register(() =>
                {
                    try { if (!proc.HasExited) proc.Kill(); }
                    catch { }
                });

                string line;
                while ((line = proc.StandardOutput.ReadLine()) != null)
                {
                    if (ct.IsCancellationRequested) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var obj  = JObject.Parse(line);
                        var type = obj["type"]?.ToString();
                        if (type != "match") continue;

                        var data = obj["data"] as JObject;
                        if (data == null) continue;

                        var filePath = data["path"]?["text"]?.ToString() ?? string.Empty;
                        var lineNum  = data["line_number"]?.ToObject<int>() ?? 0;
                        var lineText = data["lines"]?["text"]?.ToString() ?? string.Empty;

                        lineText = lineText.TrimEnd('\r', '\n');
                        if (lineText.Length > 200) lineText = lineText.Substring(0, 197) + "...";

                        // Compute relative path (best-effort)
                        var root = psi.Arguments; // not ideal but fallback is full path
                        results.Add(new RipgrepSearchResult
                        {
                            FilePath     = filePath,
                            RelativePath = filePath,   // enriched by caller if needed
                            LineNumber   = lineNum,
                            LineText     = lineText
                        });
                    }
                    catch { /* skip malformed JSON */ }
                }

                proc.WaitForExit(10000);
            }

            return results;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnProp(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
