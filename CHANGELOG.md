# Changelog

## 2026-03-21 - Đổi tên project: Rename toàn bộ từ VS2017ExtensionTemplate sang dh-codetask-extension

**Nguyên nhân:** Code gốc lấy từ template chưa được đặt tên phù hợp với dự án thực tế.

**Thay đổi:**
- Namespace `VS2017ExtensionTemplate` → `DhCodetaskExtension`
- Class `MyPackage` → `DhCodetaskPackage`
- File `MyPackage.cs` → `DhCodetaskPackage.cs`
- File `VSCommandTable.vsct` → `CommandTable.vsct`
- File `VSPackage.resx` → `DhCodetaskPackage.resx`
- File `VS2017ExtensionTemplate.csproj` → `DhCodetaskExtension.csproj`
- File `VS2017ExtensionTemplate.sln` → `DhCodetaskExtension.sln`
- Output pane title: `"VS2017 Extension Template"` → `"DH Codetask Extension"`
- Tool window title: `"Extension Template"` → `"DH Codetask"`
- Menu items: `"VS2017 Extension Template Settings"` → `"DH Codetask Extension Settings"`
- AppData folder: `"VS2017ExtensionTemplate"` → `"DhCodetaskExtension"`
- Config files: `DhCodetaskExtension.config.xml`, `DhCodetaskExtension.json`
- VSCT symbols: `guidMyPackage` → `guidDhCodetaskPackage`, `guidMyPackageCmdSet` → `guidDhCodetaskCmdSet`
- Cập nhật `README.md` với tài liệu mới
- Cập nhật `AssemblyInfo.cs` với thông tin mới
- Cập nhật `source.extension.vsixmanifest` với tên và mô tả mới

## 2026-03-21 - Thêm top-level menu: Bổ sung menu "DH Codetask Extension" trên menu bar Visual Studio

**Nguyên nhân:** Người dùng yêu cầu có menu cha riêng trên menu bar VS thay vì chỉ nằm trong Tools menu, để dễ truy cập hơn.

**Thay đổi:**
- `CommandTable.vsct`: Thêm `<Menu>` với id `TopLevelMenu` type="Menu", parent là `IDG_VS_MM_TOOLSADDINS` (vùng Add-ins trên menu bar)
- `CommandTable.vsct`: Thêm `<Group>` với id `TopLevelMenuGroup` thuộc `TopLevelMenu`
- `CommandTable.vsct`: Chuyển `CmdIdSettings` và `CmdIdJsonSettings` sang parent mới là `TopLevelMenuGroup`
- `CommandTable.vsct`: Thêm IDSymbol `TopLevelMenu` (0x1400) và `TopLevelMenuGroup` (0x1500)
- Các menu con: "Settings..." và "Settings (JSON)..." hiển thị ngắn gọn vì đã nằm trong menu cha
- Xóa group `SettingsCmdGroup` khỏi Tools menu (không cần nữa, nhưng giữ symbol để không breaking)
