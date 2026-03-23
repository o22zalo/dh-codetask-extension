## 2026-03-23 - v3.9: Cập nhật tự động qua VSIX Gallery + GitHub Actions

**Nguyên nhân:** Yêu cầu từ `docs/tasks/task-2026-03-23-01.md` — dùng cơ chế `GalleryUrl` tích hợp sẵn của VS2017 (đúng chuẩn VSIX), không tự viết HTTP client.

### Thay đổi

**`src/source.extension.vsixmanifest`:**
- Thêm `<GalleryUrl>` trỏ về GitHub Pages feed
- VS đọc feed qua `Tools > Extensions and Updates > Updates`

**`docs/vsixfeed.xml`** — generated file, do CI cập nhật tự động

**`docs/vsixfeed.template.xml`** — template gốc với `{{PLACEHOLDER}}`, chỉ sửa file này khi cần thay cấu trúc feed

**`.github/workflows/release.yml`** — YAML gọn, chỉ gọi node script:
- Trigger: push tag `v*.*.*` hoặc `workflow_dispatch`
- Patch version → Build VSIX → Prepare assets → Update feed → Create Release → Commit feed

**`.github/workflows/release.js`** — toàn bộ logic xử lý:
- `patch-version`: sửa `AssemblyInfo.cs` + `vsixmanifest`
- `prepare-assets`: rename .vsix, tạo `release_notes.md` từ CHANGELOG.md
- `update-feed`: đọc template, replace `{{VERSION}}` / `{{DOWNLOAD_URL}}` / `{{REPO_URL}}` / `{{UPDATED}}`, ghi ra `vsixfeed.xml`

**Version bump:** 3.8 → 3.9

---

## 2026-03-22 - v3.7: Fix ripgrep logging; fix Checksum canonical form; fix History refresh; fix TODO button states; fix TodoTemplate UX

**Nguyên nhân:** Yêu cầu từ `docs/tasks/task-2026-03-22-04.md`

### Fix 1 — ripgrep: Log đầy đủ + fix RelativePath

**Files:** `src/ViewModels/ProjectHelperViewModel.cs`

- `SearchContentAsync()`: log command, path, thư mục, thời gian thực thi trước/sau search
- `ParseRipgrepOutput()`: đọc stderr trên thread riêng, log toàn bộ stderr ra Output Window, log exit code
- Thêm `--` separator trước pattern để pattern bắt đầu bằng `-` không bị parse là flag
- Fix `RelativePath` trong `RipgrepSearchResult`: tính đúng bằng cách strip root prefix thay vì gán `psi.Arguments`

### Fix 2 — Task History refresh: force cache invalidation

**Files:** `src/Core/Services/HistoryQueryService.cs`, `src/ViewModels/HistoryViewModel.cs`, `src/ToolWindows/HistoryControl.xaml.cs`, `src/DevTaskTrackerPackage.cs`

- `HistoryQueryService`: thêm `InvalidateCache()` method, thêm `_watcher.Changed` event
- `HistoryViewModel`: thêm `_invalidateCache` Action (injected từ package); `RefreshAsync(forceInvalidate)` gọi invalidate trước khi query
- `HistoryControl.BtnRefresh_Click`: gọi `RefreshAsync(forceInvalidate: true)` thay vì `RefreshAsync()`
- `DevTaskTrackerPackage`: truyền `() => _historyRepo.InvalidateCache()` vào HistoryViewModel constructor

### Fix 3 — Checksum: canonical JSON form

**Files:** `src/Providers/StorageProviders/JsonStorageService.cs`

Root cause: Archive dùng `JsonConvert.Serialize(report)` (output có thể chứa `"Checksum": null`) rồi compute hash; còn verify dùng `JObject.Remove("Checksum") → ToString()` → hai chuỗi khác nhau → hash khác nhau → báo tampered sai.

Fix: `ArchiveReportAsync` dùng cùng JObject round-trip:
```
Serialize(report) → JObject.Parse → Remove("Checksum") → ToString() → hash
```

### Fix 4 — TODO buttons: CanExecuteChanged

**File:** `src/ViewModels/TodoItemViewModel.cs`

`RaiseStateChanged()` nay gọi thêm `RaiseCanExecuteChanged()` cho Start/Pause/Stop/Complete commands.

### Fix 5 — TodoTemplate: set text + enable Add button

**File:** `src/ViewModels/TrackerViewModel.cs`

- `NewTodoText` setter: raise `AddTodoCommand.CanExecuteChanged()` → nút Add tự enable khi có text
- `AddTodoFromTemplateCommand`: đổi từ auto-add thành chỉ set `NewTodoText = t` → user review text trước khi click Add

**Version bump:** 3.6 → 3.7

---

## 2026-03-22 - Project Helper: panel chuyên biệt quản lý .sln và .csproj

**Nguyên nhân:** Yêu cầu từ `docs/tasks/task-2026-03-22-01.md` — tách nghiệp vụ list file ra khỏi Tracker panel, tạo tool window riêng `Project Helper` với filter, sort, open VS, copy path.

**Version bump:** 3.3 → 3.4

## 2026-03-22 - Solution File Browser: quét .sln và .csproj trong DirectoryRootDhHosCodePath với cache TTL

## 2026-03-22 - Report Checksum: SHA-256 để phát hiện can thiệp từ bên ngoài

## 2026-03-22 - URL Duplicate Check: kiểm tra lịch sử trước khi fetch Gitea

## 2026-03-22 - Redesign Task Timer: chỉ Bắt đầu và Tạm ngưng với lý do

## 2026-03-22 - Redesign TODO: Bắt đầu / Tạm ngưng / Kết thúc

## 2026-03-22 - TODO Templates: chọn nhanh task con từ cấu hình

**Version bump:** 3.2 → 3.3

# Changelog

## 2026-03-21 - Đổi tên project: Rename toàn bộ từ VS2017ExtensionTemplate sang dh-codetask-extension

## 2026-03-21 - Thêm top-level menu: Bổ sung menu "DH Codetask Extension" trên menu bar Visual Studio

## 2026-03-21 - Triển khai DevTaskTracker v3.0: Gitea Task Tracking, Time Tracking, TODO, Reports, History Browser

## 2026-03-21 - Sửa lỗi build CS8179/CS8137 ValueTuple không hỗ trợ .NET 4.6

## 2026-03-21 - Sửa lỗi build CS8314 pattern matching generic type C# 7.0

## 2026-03-21 - Sửa warning VSTHRD103 sync block ManualTaskProvider

## 2026-03-21 - Sửa warning VSTHRD101 async lambda trên void delegate

## 2026-03-21 - Sửa lỗi runtime tool window hiển thị "Working on it..."

## 2026-03-21 - Sửa lỗi runtime InvalidOperationException WPF binding TwoWay trên readonly property

## 2026-03-21 - Thêm AppLogger: ghi log đồng thời ra file và VS Output Window

## 2026-03-21 - Loại bỏ Settings form, chỉ giữ Settings JSON: AppSettingsJsonDialog + logging + Open Log/Config buttons

## 2026-03-21 - Sửa lỗi NullReferenceException trong HistoryControl Filter_Week

## 2026-03-21 - Thêm tính năng Resume task từ lịch sử

## 2026-03-21 - Thêm tính năng Open URL trong cả 2 panel và ReportDetailDialog

## 2026-03-21 - Bật Topmost cho các dialog cấu hình và chi tiết report

**Version bump:** 3.1 → 3.2
