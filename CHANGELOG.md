# Changelog

## 2026-03-21 - Đổi tên project: Rename toàn bộ từ VS2017ExtensionTemplate sang dh-codetask-extension

**Nguyên nhân:** Code gốc lấy từ template chưa được đặt tên phù hợp với dự án thực tế.

**Thay đổi:**
- Namespace `VS2017ExtensionTemplate` → `DhCodetaskExtension`
- Class `MyPackage` → `DhCodetaskPackage`
- File `MyPackage.cs` → `DhCodetaskPackage.cs`
- Output pane title, tool window title, menu items cập nhật đồng bộ

## 2026-03-21 - Thêm top-level menu: Bổ sung menu "DH Codetask Extension" trên menu bar Visual Studio

**Nguyên nhân:** Người dùng yêu cầu có menu cha riêng trên menu bar VS.

**Thay đổi:**
- `CommandTable.vsct`: Thêm `<Menu>` TopLevelMenu, group TopLevelMenuGroup
- Di chuyển Settings và JsonSettings vào TopLevelMenuGroup

## 2026-03-21 - Triển khai DevTaskTracker v3.0: Gitea Task Tracking, Time Tracking, TODO, Reports, History Browser

**Nguyên nhân:** Theo yêu cầu tại docs/Instructions.md — xây dựng extension theo kiến trúc Provider Pattern đầy đủ.

**Thay đổi:**

### Core (không phụ thuộc VS SDK)
- Thêm `Core/Interfaces/`: `ITaskProvider`, `IEventBus`, `IGitService`, `IStorageService`, `IHistoryRepository`, `IReportGenerator`, `INotificationProvider`
- Thêm `Core/Models/`: `TaskItem`, `TodoItem`, `TimeSession`, `WorkLog`, `CompletionReport` (immutable Builder pattern), `AppSettings`
- Thêm `Core/Events/`: `TaskFetchedEvent`, `TaskStartedEvent`, `TaskPausedEvent`, `TaskResumedEvent`, `TaskCompletedEvent`, `CommitPushedEvent`, `ReportSavedEvent`, `TodoStartedEvent`, `TodoPausedEvent`, `TodoCompletedEvent`
- Thêm `Core/Services/`: `EventBus` (non-blocking, per-subscriber Task.Run), `TimeTrackingService` (state machine IDLE→RUNNING→PAUSED→COMPLETED), `HistoryQueryService` (lazy scan + FileSystemWatcher cache), `CommitMessageGenerator`, `AtomicFile` (OS-level File.Replace)

### Providers
- `GiteaTaskProvider`: parse URL, call `/api/v1/repos/{owner}/{repo}/issues/{number}`, Authorization: token header, 10s timeout, KHÔNG throw
- `ManualTaskProvider`: fallback HTML title scrape
- `TaskProviderFactory`: Open/Closed, newest provider first
- `JsonStorageService`: settings.json, current-task.json, history archive atomic write
- `GitService`: FindRepoRoot, GetCurrentBranch, PushAndCompleteAsync (add→commit→push→rev-parse HEAD)
- `JsonReportGenerator`, `MarkdownReportGenerator` (full template với sessions table, TODO table, commit section), `CompositeReportGenerator`
- `WebhookNotificationProvider`: POST JSON, retry 3 lần 1s/2s/4s backoff, fail silently

### ViewModels
- `TodoItemViewModel`: Start/Pause/Complete/Delete per-item, DispatcherTimer 1s display update, AutoPause()
- `TrackerViewModel`: fetch, timer state machine, TODO collection, commit generation, push & complete flow, auto-save 30s, restore
- `HistoryViewModel`: Today/ThisWeek/ThisMonth/Custom, search, pagination 20 items, summary bar, delete, export CSV

### Views (XAML + code-behind)
- `TrackerControl.xaml/.cs`: URL bar, task info, timer với 4 nút, TODO list với per-item controls, work notes, business logic, commit message
- `HistoryControl.xaml/.cs`: filter radio buttons, custom date range, search, summary UniformGrid, grouped list, pagination, export CSV
- `ReportDetailDialog.xaml/.cs`: sessions DataGrid, todos DataGrid, commit/git section, open .md/.json buttons
- `TaskSettingsDialog.xaml/.cs`: 12 trường Gitea/Git/Storage/Report/Webhook, validation

### Tool Windows
- `TrackerToolWindow`, `HistoryToolWindow`: VS dockable ToolWindowPane

### Package
- `DevTaskTrackerPackage.cs`: wiring tất cả services, khởi tạo đúng thứ tự, restore in-progress task, commands ShowTrackerWindow/ShowHistoryWindow/ShowTaskSettings
- `CommandTable.vsct`: bổ sung ShowTrackerWindowId (0x0200), ShowHistoryWindowId (0x0300), ShowTaskSettingsId (0x0600), TrackerMenuGroup (0x1600)
- `PackageGuids.cs`: bổ sung 4 ID mới
- `DhCodetaskExtension.csproj`: đăng ký toàn bộ 50+ file mới
- `AssemblyInfo.cs`: version 3.0.0.0
- `source.extension.vsixmanifest`: version 3.0, mô tả cập nhật

### Tuân thủ error-skill-devtasktracker.md
- Wiring: constructor injection, không register trùng, không sửa class cũ khi thêm provider
- Compile: chỉ dùng API có trong .NET 4.6.1 / VS2017
- Time Tracking: state machine ngăn transition sai, AutoPause TODO khi Task pause
- Report: một entry point duy nhất (CompleteFlowAsync), CompletionReport immutable sau Build()
- File I/O: AtomicFile.WriteAllTextAsync cho tất cả ghi file
- EventBus: non-blocking, subscriber exception không crash publisher
- Async: không dùng async void ngoài event handler, try-catch tại boundary
