using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DhCodetaskExtension.Core.Models;
using DhCodetaskExtension.Core.Services;

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
        private readonly Action<string>      _log;

        private List<SolutionFileEntry> _allFiles = new List<SolutionFileEntry>();

        private string          _filterKeyword  = string.Empty;
        private ProjectSortMode _sortMode        = ProjectSortMode.NameAsc;
        private ProjectFileType _fileType        = ProjectFileType.All;
        private bool            _isLoading;
        private string          _statusMessage   = "Nhấn 🔄 để quét thư mục.";

        // ── Properties ────────────────────────────────────────────────────

        public string FilterKeyword
        {
            get => _filterKeyword;
            set { _filterKeyword = value; OnProp(nameof(FilterKeyword)); ApplyFilter(); }
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

        // Sort flag helpers (for RadioButton binding)
        public bool SortNameAsc  { get => SortMode == ProjectSortMode.NameAsc;  set { if (value) SortMode = ProjectSortMode.NameAsc; } }
        public bool SortNameDesc { get => SortMode == ProjectSortMode.NameDesc; set { if (value) SortMode = ProjectSortMode.NameDesc; } }
        public bool SortDateDesc { get => SortMode == ProjectSortMode.DateDesc; set { if (value) SortMode = ProjectSortMode.DateDesc; } }
        public bool SortDateAsc  { get => SortMode == ProjectSortMode.DateAsc;  set { if (value) SortMode = ProjectSortMode.DateAsc; } }

        // Type flag helpers (for RadioButton binding)
        public bool TypeAll     { get => FileType == ProjectFileType.All;       set { if (value) FileType = ProjectFileType.All; } }
        public bool TypeSln     { get => FileType == ProjectFileType.SlnOnly;   set { if (value) FileType = ProjectFileType.SlnOnly; } }
        public bool TypeCsproj  { get => FileType == ProjectFileType.CsprojOnly;set { if (value) FileType = ProjectFileType.CsprojOnly; } }

        public ObservableCollection<SolutionFileEntry> Files { get; }
            = new ObservableCollection<SolutionFileEntry>();

        // ── Commands ──────────────────────────────────────────────────────
        public ICommand RefreshCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public RelayCommand<SolutionFileEntry> OpenInVsCommand   { get; }
        public RelayCommand<SolutionFileEntry> CopyPathCommand   { get; }
        public RelayCommand<SolutionFileEntry> OpenFolderCommand { get; }

        // ── Injected actions ──────────────────────────────────────────────
        /// <summary>Called to open a file in Visual Studio (or OS default).</summary>
        public Action<string> OpenFileAction   { get; set; }
        /// <summary>Called to copy text to clipboard.</summary>
        public Action<string> CopyToClipboard  { get; set; }
        /// <summary>Called to open a folder in Explorer.</summary>
        public Action<string> OpenFolderAction { get; set; }

        public ProjectHelperViewModel(SolutionFileService service, Action<string> log)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _log     = log     ?? (_ => { });

            RefreshCommand    = new RelayCommand(RefreshFireAndForget);
            ClearFilterCommand= new RelayCommand(() => FilterKeyword = string.Empty);

            OpenInVsCommand   = new RelayCommand<SolutionFileEntry>(entry =>
            {
                if (entry == null) return;
                _log(string.Format("[ProjectHelper] 📂 Open: {0}", entry.FullPath));
                OpenFileAction?.Invoke(entry.FullPath);
            });

            CopyPathCommand   = new RelayCommand<SolutionFileEntry>(entry =>
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
                _log(string.Format("[ProjectHelper] ✅ Found {0} file(s).", _allFiles.Count));
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

        private void ApplyFilter()
        {
            var src = _allFiles.AsEnumerable();

            // 1. Type filter
            switch (FileType)
            {
                case ProjectFileType.SlnOnly:
                    src = src.Where(f => f.Extension == ".sln");
                    break;
                case ProjectFileType.CsprojOnly:
                    src = src.Where(f => f.Extension == ".csproj");
                    break;
            }

            // 2. Keyword filter
            if (!string.IsNullOrWhiteSpace(FilterKeyword))
            {
                var kw = FilterKeyword.ToLower();
                src = src.Where(f =>
                    f.FileName.ToLower().Contains(kw) ||
                    f.RelativePath.ToLower().Contains(kw));
            }

            // 3. Sort
            switch (SortMode)
            {
                case ProjectSortMode.NameAsc:
                    src = src.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase);
                    break;
                case ProjectSortMode.NameDesc:
                    src = src.OrderByDescending(f => f.FileName, StringComparer.OrdinalIgnoreCase);
                    break;
                case ProjectSortMode.DateDesc:
                    src = src.OrderByDescending(f => f.LastModified);
                    break;
                case ProjectSortMode.DateAsc:
                    src = src.OrderBy(f => f.LastModified);
                    break;
            }

            var result = src.ToList();
            Files.Clear();
            foreach (var f in result) Files.Add(f);

            StatusMessage = result.Count == 0
                ? (_allFiles.Count == 0 ? "Chưa quét. Cấu hình DirectoryRootDhHosCodePath và nhấn 🔄." : "Không tìm thấy file nào khớp.")
                : string.Format("{0} file — cập nhật {1:HH:mm:ss}", result.Count, DateTime.Now);
        }

        private void RaiseSortFlags()
        {
            OnProp(nameof(SortNameAsc));
            OnProp(nameof(SortNameDesc));
            OnProp(nameof(SortDateDesc));
            OnProp(nameof(SortDateAsc));
        }

        private void RaiseTypeFlags()
        {
            OnProp(nameof(TypeAll));
            OnProp(nameof(TypeSln));
            OnProp(nameof(TypeCsproj));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnProp(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
