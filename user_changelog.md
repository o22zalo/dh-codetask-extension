## 2026-03-23 10:30 — v3.9: Cập nhật tự động qua VSIX Gallery của Visual Studio

**Yêu cầu:** Dùng cơ chế cập nhật tích hợp sẵn của Visual Studio — không tự viết HTTP client.

**Cách hoạt động:**
- Extension khai báo `<GalleryUrl>` trong `source.extension.vsixmanifest`
- VS tự check feed qua **Tools > Extensions and Updates > Updates**
- VS tự tải và cài — không cần code gì thêm trong extension

**Triển khai:**

`src/source.extension.vsixmanifest` — thêm 1 dòng `<GalleryUrl>` (thay `YOUR_GITHUB_USERNAME`)

`docs/vsixfeed.template.xml` — template Atom feed với `{{PLACEHOLDER}}`, chỉ sửa khi cần đổi cấu trúc

`docs/vsixfeed.xml` — generated, do CI tự cập nhật sau mỗi release

`.github/workflows/release.yml` — YAML gọn, chỉ gọi `node release.js <command>`

`.github/workflows/release.js` — toàn bộ logic: patch-version, prepare-assets, update-feed

**Version bump:** 3.8 → 3.9

## 2026-03-23 10:30 — v3.9: Cập nhật tự động qua cơ chế VSIX Gallery của Visual Studio

**Yêu cầu:** Dùng cơ chế cập nhật tích hợp sẵn của Visual Studio thay vì tự viết HTTP client.

**Cách hoạt động:**

- Extension khai báo `<GalleryUrl>` trong `source.extension.vsixmanifest`
- VS tự kiểm tra feed này qua **Tools > Extensions and Updates > Updates**
- VS tự tải và cài VSIX mới — không cần code bổ sung trong extension

**Triển khai:**

**1. `src/source.extension.vsixmanifest`** — thêm `<GalleryUrl>`:

```xml
<GalleryUrl>https://YOUR_GITHUB_USERNAME.github.io/dh-codetask-extension/vsixfeed.xml</GalleryUrl>
```

**2. `docs/vsixfeed.xml`** — Atom feed chuẩn VSIX Gallery Schema, host trên GitHub Pages:

- VS đọc format Atom feed với namespace `http://schemas.microsoft.com/developer/vsx-syndication-schema/2010`
- Mỗi entry chứa `<Vsix>` với Id, Version, và `<content src="URL_VSIX_FILE"/>`

**3. `.github/workflows/release.yml`** — GitHub Actions tự động:

- Trigger: push tag `v*.*.*`
- Patch version vào `AssemblyInfo.cs` và `vsixmanifest`
- Build VSIX với MSBuild + NuGet
- Tạo GitHub Release + upload .vsix
- Cập nhật `docs/vsixfeed.xml` với version và download URL mới
- Commit feed vào repo (VS Pages phục vụ tự động)

**4. `src/DevTaskTrackerPackage.cs`** — log hướng dẫn khi khởi động:

- "Kiểm tra bản mới: Tools > Extensions and Updates > Updates"

## 2026-03-22 18:30 — v3.8: Fix TodoTemplates hiển thị + TODO chỉ chạy khi task cha Running

**Yêu cầu:**

1. TodoTemplates chưa hiển thị — cần triển khai chọn từ danh sách, button Add enabled
2. TODO con phải chỉ start được khi task cha đang Running

**1. Fix TodoTemplates Expander không hiển thị**

Nguyên nhân: `BooleanToVisibilityConverter` chỉ nhận `bool`, nhưng XAML bind Visibility vào `TodoTemplates.Count` (kiểu `int`) → WPF silent-fail → Expander luôn ẩn.

Fix: Thêm `HasTodoTemplates { get => TodoTemplates.Count > 0; }` vào `TrackerViewModel`. XAML bind `Visibility="{Binding HasTodoTemplates, Converter={StaticResource BoolVis}}"`. Expander có `IsExpanded="True"` để tự mở, thêm hint text hướng dẫn user.

Files: `src/ViewModels/TrackerViewModel.cs`, `src/ToolWindows/TrackerControl.xaml`

**2. TODO ▶ chỉ enable khi task cha đang Running**

Nguyên nhân: `TodoItemViewModel.CanStart` chỉ kiểm tra state của chính item, không kiểm tra task cha.

Fix:

- `TodoItemViewModel`: thêm `Func<bool> isParentRunning` param. `CanStart` kiểm tra cả hai điều kiện. Thêm `RefreshParentStateCommands()` để re-evaluate khi task cha đổi state.
- `TrackerViewModel.CreateTodoVm()`: truyền `() => TimerState == "Running"`.
- `TrackerViewModel.TimerState` setter: gọi `RefreshTodoParentStateCommands()` → mỗi todo vm gọi `RefreshParentStateCommands()`.

Kết quả: Task Running → ▶ enable; Task Paused/Stopped/Idle → ▶ disable tức thì.

Files: `src/ViewModels/TodoItemViewModel.cs`, `src/ViewModels/TrackerViewModel.cs`

**Version bump:** 3.7 → 3.8

