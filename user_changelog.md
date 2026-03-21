# User Changelog

## 2026-03-21 10:00 — Đổi tên project

**Yêu cầu:** Đổi tên toàn bộ project từ `VS2017ExtensionTemplate` sang `dh-codetask-extension`.
- Đổi tên namespace, assembly, class, file, GUID string display
- Cập nhật tất cả references bên trong code
- Tạo zip theo quy tắc versioning

## 2026-03-21 11:00 — Thêm top-level menu "DH Codetask Extension"

**Yêu cầu:** Bổ sung menu cha là "DH Codetask Extension" trên menu bar Visual Studio, với menu con "Settings" và "Settings (JSON)".

## 2026-03-21 14:00 — Triển khai DevTaskTracker v3.0

**Yêu cầu:** Đọc docs/Instructions.md, docs/error-skill-devtasktracker.md, docs/rule.md và triển khai toàn bộ theo Instructions.md — DevTaskTracker v3.0 với:
- Fetch task từ Gitea (Provider Pattern)
- Time tracking cấp task và TODO item độc lập
- Completion Report JSON + Markdown (immutable, atomic write)
- History Browser (ngày/tuần/tháng, phân trang, export CSV)
- EventBus non-blocking, WebhookNotificationProvider
- Settings dialog 12 trường
- Restore task dở khi khởi động lại VS
