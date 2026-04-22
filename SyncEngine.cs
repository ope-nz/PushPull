using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PushPull
{
    public enum SyncStatus
    {
        Same,
        LocalNewer,
        RemoteNewer,
        LocalOnly,
        RemoteOnly
    }

    public class FileEntry
    {
        public string RelativePath { get; set; }
        public bool ExistsLocally { get; set; }
        public bool ExistsRemotely { get; set; }
        public DateTime LocalModified { get; set; }
        public long LocalSize { get; set; }
        public string LocalSha { get; set; }
        public string RemoteSha { get; set; }
        public int RemoteSize { get; set; }
        public SyncStatus Status { get; set; }

        public string DisplayName
        {
            get
            {
                int slash = RelativePath.LastIndexOfAny(new[] { '/', '\\' });
                return slash >= 0 ? RelativePath.Substring(slash + 1) : RelativePath;
            }
        }
    }

    public static class SyncEngine
    {
        public static List<FileEntry> Compare(
            GfdProject project,
            Dictionary<string, GitHub.RemoteFile> remoteIndex)
        {
            var entries = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);

            // Walk local files
            if (Directory.Exists(project.LocalFolder))
            {
                foreach (string abs in GetFilesRecursively(project.LocalFolder))
                {
                    string rel = abs.Substring(project.LocalFolder.Length)
                        .TrimStart('\\', '/').Replace('\\', '/');

                    if (ShouldIgnore(rel, project.IgnorePatterns)) continue;

                    var info = new FileInfo(abs);
                    string sha = GitHub.CalcLocalSha(abs);

                    entries[rel] = new FileEntry
                    {
                        RelativePath = rel,
                        ExistsLocally = true,
                        LocalModified = info.LastWriteTime,
                        LocalSize = info.Length,
                        LocalSha = sha
                    };
                }
            }

            // Merge remote
            foreach (var kv in remoteIndex)
            {
                string rel = kv.Key.Replace('\\', '/');
                if (ShouldIgnore(rel, project.IgnorePatterns)) continue;

                if (entries.ContainsKey(rel))
                {
                    entries[rel].ExistsRemotely = true;
                    entries[rel].RemoteSha = kv.Value.Sha;
                    entries[rel].RemoteSize = kv.Value.Size;
                }
                else
                {
                    entries[rel] = new FileEntry
                    {
                        RelativePath = rel,
                        ExistsRemotely = true,
                        RemoteSha = kv.Value.Sha,
                        RemoteSize = kv.Value.Size
                    };
                }
            }

            // Assign status
            foreach (var e in entries.Values)
            {
                if (e.ExistsLocally && !e.ExistsRemotely)
                    e.Status = SyncStatus.LocalOnly;
                else if (!e.ExistsLocally && e.ExistsRemotely)
                    e.Status = SyncStatus.RemoteOnly;
                else if (e.LocalSha == e.RemoteSha)
                    e.Status = SyncStatus.Same;
                else
                    e.Status = SyncStatus.LocalNewer; // SHA differs; can't determine direction without dates so default local
            }

            var list = new List<FileEntry>(entries.Values);
            list.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        static IEnumerable<string> GetFilesRecursively(string folder)
        {
            foreach (string f in Directory.GetFiles(folder))
                yield return f;
            foreach (string d in Directory.GetDirectories(folder))
                foreach (string f in GetFilesRecursively(d))
                    yield return f;
        }

        static bool ShouldIgnore(string relativePath, List<string> patterns)
        {
            if (patterns == null) return false;
            foreach (string pattern in patterns)
            {
                string p = pattern.TrimEnd('/');
                // Directory prefix match
                if (pattern.EndsWith("/") && (relativePath.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase)
                    || relativePath.Equals(p, StringComparison.OrdinalIgnoreCase)))
                    return true;
                // Wildcard
                if (pattern.Contains("*") || pattern.Contains("?"))
                {
                    string regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    string fileName = Path.GetFileName(relativePath);
                    if (Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase)) return true;
                    if (Regex.IsMatch(relativePath, regex, RegexOptions.IgnoreCase)) return true;
                }
                // Exact suffix match
                if (relativePath.EndsWith(pattern, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
