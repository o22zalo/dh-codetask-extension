# DH Codetask Extension

DH Codetask Extension hiện được nâng cấp thành **Dev Task Tracker** cho Visual Studio 2017: theo dõi task, TODO, bộ đếm thời gian, ghi chú triển khai và sinh báo cáo hoàn thành ngay trong tool window.

## Tính năng chính

- **Task tracker panel** với các trường Issue URL, Task ID, Title, Labels, Description.
- **Time tracking** cho task với các trạng thái Start / Pause / Resume / Stop.
- **TODO list** có timer độc lập cho từng item, cho phép Start / Pause / Toggle Done.
- **Work Notes** và **Business Logic** để lưu lại quá trình làm việc.
- **Commit message generator** theo convention `feat(scope): #task slug-title`.
- **Snapshot storage** vào `%APPDATA%\DhCodetaskExtension\current-task.json`.
- **Completion report** sinh đồng thời file JSON và Markdown vào `%APPDATA%\DhCodetaskExtension\history\YYYY\MM`.
- Giữ lại các thành phần nền sẵn có: **Output Window**, **Status Bar**, **Settings XML/JSON**, **Options Page**.

## Cấu trúc liên quan

```
src/
├── Models/TaskTrackerModels.cs
├── Services/TaskTrackerService.cs
└── ToolWindows/MainToolWindowControl.xaml(.cs)
```

## Cách dùng nhanh

1. Mở `View > Other Windows > DH Codetask`.
2. Nhập thông tin task hoặc issue thủ công.
3. Bấm **Start** để bắt đầu timer task.
4. Thêm TODO và dùng **Start TODO** khi cần track từng đầu việc.
5. Ghi Work Notes, Business Logic, rồi bấm **Regenerate Commit Message** nếu muốn sinh commit message gợi ý.
6. Bấm **Save Snapshot** để lưu trạng thái làm việc hiện tại.
7. Khi xong, bấm **Complete & Archive** để tạo báo cáo JSON + Markdown.

## Versioning

- Phiên bản hiện tại: **3.0.0**
- Gói zip theo rule: `dh-codetask-extension.<version>.<noi-dung-thay-doi>.zip`
