/**
 * release.js — Build helper cho GitHub Actions
 *
 * Commands:
 *   node release.js patch-version  <version>
 *   node release.js prepare-assets <version>
 *   node release.js update-feed    <version> <owner/repo>
 *
 * Thiết kế: logic nằm ở đây, YAML chỉ gọi node.
 * Template feed đặt tại docs/vsixfeed.template.xml — chỉ replace {{PLACEHOLDER}}.
 */

'use strict';

const fs   = require('fs');
const path = require('path');

// ── Helpers ──────────────────────────────────────────────────────────────────

function readFile(p)        { return fs.readFileSync(p, 'utf8'); }
function writeFile(p, data) { fs.writeFileSync(p, data, 'utf8'); console.log('  wrote:', p); }

function replaceInFile(filePath, pairs) {
  let content = readFile(filePath);
  for (const [from, to] of pairs) {
    content = content.replace(from, to);
  }
  writeFile(filePath, content);
}

function findFirst(dir, ext) {
  const files = fs.readdirSync(dir).filter(f => f.endsWith(ext));
  if (!files.length) throw new Error(`No ${ext} found in ${dir}`);
  return path.join(dir, files[0]);
}

function nowIso() { return new Date().toISOString().replace(/\.\d+Z$/, 'Z'); }

// ── Command: patch-version ────────────────────────────────────────────────────
// Ghi version vào AssemblyInfo.cs và vsixmanifest.
// Không đụng vào file nào khác.

function patchVersion(version) {
  console.log(`[patch-version] Patching to ${version}...`);

  // AssemblyInfo.cs
  const asmPath = 'src/Properties/AssemblyInfo.cs';
  replaceInFile(asmPath, [
    [/AssemblyVersion\(".*?"\)/,     `AssemblyVersion("${version}.0")`],
    [/AssemblyFileVersion\(".*?"\)/, `AssemblyFileVersion("${version}.0")`],
  ]);

  // source.extension.vsixmanifest — chỉ patch attribute Version trên dòng Identity
  const mfPath = 'src/source.extension.vsixmanifest';
  replaceInFile(mfPath, [
    [/(<Identity[^>]+Version=")[^"]*(")/,  `$1${version}$2`],
  ]);

  console.log('[patch-version] Done.');
}

// ── Command: prepare-assets ───────────────────────────────────────────────────
// Tìm file .vsix trong bin/Release, đổi tên, tạo release_notes.md.

function prepareAssets(version) {
  console.log(`[prepare-assets] Preparing for version ${version}...`);

  // Rename VSIX
  const vsixSrc  = findFirst('src/bin/Release', '.vsix');
  const vsixDest = `DhCodetaskExtension.${version}.vsix`;
  fs.copyFileSync(vsixSrc, vsixDest);
  console.log(`  VSIX: ${vsixDest}`);

  // Build release notes từ CHANGELOG.md (lấy block đầu tiên)
  let notes = `## DH Codetask Extension v${version}\n\n`;
  if (fs.existsSync('CHANGELOG.md')) {
    const cl       = readFile('CHANGELOG.md');
    const blocks   = cl.split(/\n## /);           // tách theo heading ##
    const firstBlock = blocks[1] ? '## ' + blocks[1].trim() : cl.substring(0, 1500);
    notes += firstBlock;
  }
  writeFile('release_notes.md', notes);

  console.log('[prepare-assets] Done.');
}

// ── Command: update-feed ──────────────────────────────────────────────────────
// Đọc template docs/vsixfeed.template.xml, replace placeholder, ghi ra docs/vsixfeed.xml.

function updateFeed(version, ownerRepo) {
  console.log(`[update-feed] Updating feed for ${version} (${ownerRepo})...`);

  const templatePath = 'docs/vsixfeed.template.xml';
  const feedPath     = 'docs/vsixfeed.xml';

  if (!fs.existsSync(templatePath)) {
    throw new Error(`Template not found: ${templatePath}\nTạo file này trước khi chạy workflow.`);
  }

  const vsixFile   = `DhCodetaskExtension.${version}.vsix`;
  const repoUrl    = `https://github.com/${ownerRepo}`;
  const downloadUrl = `${repoUrl}/releases/download/v${version}/${vsixFile}`;
  const now        = nowIso();

  const feed = readFile(templatePath)
    .replace(/\{\{VERSION\}\}/g,      version)
    .replace(/\{\{DOWNLOAD_URL\}\}/g, downloadUrl)
    .replace(/\{\{REPO_URL\}\}/g,     repoUrl)
    .replace(/\{\{UPDATED\}\}/g,      now);

  writeFile(feedPath, feed);
  console.log('[update-feed] Done.');
}

// ── Entry point ───────────────────────────────────────────────────────────────

const [,, cmd, ...args] = process.argv;

switch (cmd) {
  case 'patch-version':  patchVersion(args[0]);           break;
  case 'prepare-assets': prepareAssets(args[0]);          break;
  case 'update-feed':    updateFeed(args[0], args[1]);    break;
  default:
    console.error(`Unknown command: ${cmd}`);
    console.error('Usage: node release.js <patch-version|prepare-assets|update-feed> [args]');
    process.exit(1);
}
