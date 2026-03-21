using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DhCodetaskExtension.Core.Services
{
    /// <summary>
    /// Atomic file write: write to .tmp, then rename/replace.
    /// Prevents data corruption on crash.
    /// </summary>
    public static class AtomicFile
    {
        public static async Task WriteAllTextAsync(string path, string content)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var tmp = path + ".tmp";
            await Task.Run(() => File.WriteAllText(tmp, content, Encoding.UTF8));

            await Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    var bak = path + ".bak";
                    try { File.Replace(tmp, path, bak); }
                    catch
                    {
                        // Fallback if File.Replace fails (different volumes, etc.)
                        File.Copy(tmp, path, overwrite: true);
                        try { File.Delete(tmp); } catch { }
                    }
                }
                else
                {
                    File.Move(tmp, path);
                }
            });
        }
    }
}
