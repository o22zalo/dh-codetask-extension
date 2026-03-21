# User Changelog

## 2026-03-21 10:00 — Đổi tên project

**Yêu cầu:** Đổi tên toàn bộ project từ `VS2017ExtensionTemplate` sang `dh-codetask-extension`.
- Đổi tên namespace, assembly, class, file, GUID string display
- Đổi tên các file để dễ quản lý
- Cập nhật tất cả references bên trong code
- Tạo zip theo quy tắc versioning

## 2026-03-21 11:00 — Thêm top-level menu "DH Codetask Extension"

**Yêu cầu:** Bổ sung menu cha là "DH Codetask Extension" trên menu bar Visual Studio, với menu con "Settings" và "Settings (JSON)" để mở form cấu hình.


## 2026-03-21 10:23 — Triển khai Dev Task Tracker theo Instructions.md

**Yêu cầu:** Đọc `docs/Instructions.md`, `docs/error-skill-devtasktracker.md`, `docs/rule.md` và triển khai tool window theo hướng task tracker thực tế, có timer task/TODO, commit message và completion report.
