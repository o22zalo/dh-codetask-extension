# DH Codetask Extension — DevTask Tracker v3.9

Extension theo dõi task, time tracking và TODO trực tiếp trong Visual Studio 2017, tích hợp Gitea.

## Tính năng v3.9 (mới) — Cập nhật tự động

VS2017 có cơ chế **VSIX Gallery** tích hợp sẵn. Extension khai báo một feed URL, VS tự kiểm tra và thông báo khi có bản mới qua **Tools > Extensions and Updates > Updates**.

| Thành phần | Mô tả |
|-----------|-------|
| `GalleryUrl` trong vsixmanifest | VS đọc Atom feed từ URL này để phát hiện bản mới |
| `docs/vsixfeed.xml` | Feed chuẩn VSIX Gallery Schema, host GitHub Pages |
| `.github/workflows/release.yml` | GitHub Actions tự build + release + cập nhật feed |

## Hướng dẫn thiết lập (một lần duy nhất)

### Bước 1 — Push repo lên GitHub

```bash
git remote add origin https://github.com/YOUR_USERNAME/dh-codetask-extension.git
git push -u origin main
```

### Bước 2 — Bật GitHub Pages

Vào **Settings > Pages > Source** → chọn `Deploy from branch: main / docs`

Sau vài phút, feed có tại:
```
https://YOUR_USERNAME.github.io/dh-codetask-extension/vsixfeed.xml
```

### Bước 3 — Cập nhật GalleryUrl trong vsixmanifest

Mở `src/source.extension.vsixmanifest`, thay `YOUR_GITHUB_USERNAME`:

```xml
<GalleryUrl>https://YOUR_USERNAME.github.io/dh-codetask-extension/vsixfeed.xml</GalleryUrl>
```

Cũng cập nhật URL tương tự trong `docs/vsixfeed.xml` (phần placeholder).

### Bước 4 — Commit và push lần đầu

```bash
git add src/source.extension.vsixmanifest docs/vsixfeed.xml
git commit -m "chore: configure GalleryUrl for auto-update"
git push
```

## Tạo release mới

```bash
git tag v3.9.1
git push origin v3.9.1
```

GitHub Actions sẽ tự động:
1. Patch version vào `AssemblyInfo.cs` và `vsixmanifest`
2. Build VSIX với MSBuild
3. Tạo GitHub Release + upload `.vsix`
4. Cập nhật `docs/vsixfeed.xml` với version mới
5. Commit feed vào repo → GitHub Pages phục vụ tự động

## Cách user nhận cập nhật

Trong Visual Studio: **Tools > Extensions and Updates > Updates**

VS tự kiểm tra `GalleryUrl` và hiển thị nút **Update** nếu có bản mới:

```
DH Codetask Extension    v3.9 → v3.9.1    [Update]
```

User click **Update** → VS tải .vsix và yêu cầu restart → cập nhật hoàn tất.

> VS2017 kiểm tra cập nhật định kỳ tự động (mặc định khi mở VS). Không cần thêm code trong extension.

## Log khi khởi động

Mở Output Window > DH Codetask Extension để thấy:
```
[Update] VS tự quản lý cập nhật extension qua GalleryUrl trong vsixmanifest.
[Update] Kiểm tra bản mới: Tools > Extensions and Updates > Updates
```

## Cấu trúc thư mục

```
dh-codetask-extension/
├── .github/
│   └── workflows/
│       └── release.yml          ← v3.9: GitHub Actions build + release + feed update
├── docs/
│   └── vsixfeed.xml             ← v3.9: Atom feed cho VSIX Gallery (GitHub Pages)
└── src/
    ├── source.extension.vsixmanifest  ← v3.9: thêm <GalleryUrl>
    └── DevTaskTrackerPackage.cs       ← v3.9: version bump, log update hint
```

## Tính năng v3.8

| Tính năng | Mô tả |
|-----------|-------|
| **Fix: TodoTemplates** | Sửa lỗi Expander không hiện do bind int vào BooleanToVisibilityConverter |
| **Fix: TODO Start gated** | TODO chỉ Start được khi task cha đang Running |

## Cài đặt & Build

```bash
# 1. Mở VS2017, open DhCodetaskExtension.sln
# 2. Restore NuGet packages
# 3. F5 để debug trong Experimental Instance
# 4. View > DevTask Tracker để mở panel
```

---

_DhCodetaskExtension v3.9 — VSIX Gallery Auto-Update · GitHub Actions Release · GitHub Pages Feed_
