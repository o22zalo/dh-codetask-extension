# DH Codetask Extension — DevTask Tracker v3.0

Extension theo dõi task, time tracking và TODO trực tiếp trong Visual Studio 2017, tích hợp Gitea.

## Tính năng v3.0

| Tính năng | Mô tả |
|-----------|-------|
| **Gitea Integration** | Fetch task từ URL issue Gitea, auto-populate title/labels/description |
| **Task Timer** | Start / Pause / Resume / Stop với đồng hồ real-time |
| **TODO Time Tracking** | Mỗi TODO item có timer độc lập Start/Pause/Complete |
| **Completion Report** | Sinh JSON + Markdown tự động, lưu `history\YYYY\MM\` |
| **History Browser** | Hôm nay / Tuần / Tháng / Tùy chỉnh, phân trang, export CSV |
| **Git Integration** | Auto commit message generation, push & complete một thao tác |
| **Webhook** | POST event ra ngoài (Slack, CI/CD…), retry 3 lần |
| **Restore** | Khôi phục task dở khi VS khởi động lại |
| **Provider Pattern** | Thêm provider mới không cần sửa code hiện có |

## Cấu trúc thư mục

```
dh-codetask-extension/
├── DhCodetaskExtension.sln
├── LICENSE / README.md / CHANGELOG.md / user_changelog.md
├── .opushforce.message / .gitignore
├── docs/
│   ├── Instructions.md      ← Spec đầy đủ v3.0
│   ├── error-skill-devtasktracker.md
│   └── rule.md
└── src/
    ├── DhCodetaskExtension.csproj
    ├── DevTaskTrackerPackage.cs    ← Package entry point v3.0
    ├── PackageGuids.cs
    ├── CommandTable.vsct
    ├── source.extension.vsixmanifest
    ├── packages.config
    ├── Core/
    │   ├── Interfaces/             ← ITaskProvider, IEventBus, IGitService, …
    │   ├── Models/                 ← TaskItem, TodoItem, CompletionReport, …
    │   ├── Events/                 ← TaskStartedEvent, TodoCompletedEvent, …
    │   └── Services/               ← EventBus, TimeTrackingService, AtomicFile, …
    ├── Providers/
    │   ├── TaskProviders/          ← GiteaTaskProvider, ManualTaskProvider, Factory
    │   ├── StorageProviders/       ← JsonStorageService
    │   ├── GitProviders/           ← GitService
    │   ├── ReportProviders/        ← Json/Markdown/CompositeReportGenerator
    │   └── NotificationProviders/  ← WebhookNotificationProvider
    ├── ViewModels/
    │   ├── TrackerViewModel.cs
    │   ├── TodoItemViewModel.cs
    │   └── HistoryViewModel.cs
    ├── Commands/                   ← ShowTrackerWindow, ShowHistoryWindow, …
    ├── Services/                   ← OutputWindowService, StatusBarService, …
    └── ToolWindows/
        ├── TrackerControl.xaml/.cs
        ├── HistoryControl.xaml/.cs
        ├── ReportDetailDialog.xaml/.cs
        ├── TaskSettingsDialog.xaml/.cs
        └── DevTaskToolWindows.cs
```

## Cài đặt & Build

```bash
# 1. Mở VS2017, open DhCodetaskExtension.sln
# 2. Restore NuGet packages
# 3. F5 để debug trong Experimental Instance
# 4. View > DevTask Tracker để mở panel
```

## Cấu hình Gitea

1. Mở **DH Codetask Extension > DevTask Settings...**
2. Điền `Gitea Base URL`, `Personal Token`
3. Dán URL issue vào URL bar → **Fetch**

## Luồng cơ bản

```
URL issue → [Fetch] → [▶ Start] → work... → [⏸ Pause]... → [▶ Resume]
→ Thêm TODO items, start/pause từng item
→ Ghi Work Notes & Business Logic
→ [🚀 Push & Complete] → git commit+push → report JSON+MD → clear
```

## Mở rộng

```csharp
// Thêm provider task mới (Jira, Linear…)
public class JiraTaskProvider : ITaskProvider {
    public bool CanHandle(string url) => url.Contains("atlassian.net");
    public async Task<TaskFetchResult> FetchAsync(string url, CancellationToken ct) { … }
}
// Đăng ký trong Package.InitializeAsync():
_taskFactory.Register(new JiraTaskProvider(() => Settings));
// Xong. Không sửa gì khác.
```

## Versioning

Format: `dh-codetask-extension.<version>.<nội-dung-thay-đổi>.zip`

## Changelog

Xem `CHANGELOG.md` để biết lịch sử thay đổi chi tiết.

---

_DhCodetaskExtension v3.0 — Gitea-first · Provider Pattern · TODO Time Tracking · EventBus · Completion Report · History Browser_
