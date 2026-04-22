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

        public string DisplayName { get { return RelativePath; } }

        public string FileName
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

            // Walk local files, skipping ignored directories early
            if (Directory.Exists(project.LocalFolder))
            {
                foreach (string abs in GetFilesRecursively(project.LocalFolder, project.LocalFolder, project.IgnorePatterns))
                {
                    string rel = abs.Substring(project.LocalFolder.Length)
                        .TrimStart('\\', '/').Replace('\\', '/');

                    if (ShouldIgnore(rel, project.IgnorePatterns)) continue;

                    var info = new FileInfo(abs);
                    entries[rel] = new FileEntry
                    {
                        RelativePath = rel,
                        ExistsLocally = true,
                        LocalModified = info.LastWriteTime,
                        LocalSize = info.Length,
                        LocalSha = GitHub.CalcLocalSha(abs)
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

        static IEnumerable<string> GetFilesRecursively(string folder, string baseFolder, List<string> ignorePatterns)
        {
            foreach (string f in Directory.GetFiles(folder))
                yield return f;
            foreach (string d in Directory.GetDirectories(folder))
            {
                string relDir = d.Substring(baseFolder.Length).TrimStart('\\', '/').Replace('\\', '/');
                if (ShouldIgnoreFolder(relDir, ignorePatterns)) continue;
                foreach (string f in GetFilesRecursively(d, baseFolder, ignorePatterns))
                    yield return f;
            }
        }

        // Checks whether an entire directory should be skipped during the walk.
        static bool ShouldIgnoreFolder(string relDir, List<string> patterns)
        {
            if (patterns == null) return false;
            string dirName = relDir;
            int slash = relDir.LastIndexOf('/');
            if (slash >= 0) dirName = relDir.Substring(slash + 1);

            foreach (string pattern in patterns)
            {
                // Explicit folder pattern: bin/
                if (pattern.EndsWith("/"))
                {
                    string p = pattern.TrimEnd('/');
                    if (relDir.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                        relDir.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                // Bare name with no wildcards matches any folder segment of that name
                else if (!pattern.Contains("*") && !pattern.Contains("?") && !pattern.Contains("/"))
                {
                    if (string.Equals(dirName, pattern, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        static bool ShouldIgnore(string relativePath, List<string> patterns)
        {
            if (patterns == null) return false;
            foreach (string pattern in patterns)
            {
                string p = pattern.TrimEnd('/');
                // Explicit folder pattern: bin/
                if (pattern.EndsWith("/") && (relativePath.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase)
                    || relativePath.Equals(p, StringComparison.OrdinalIgnoreCase)))
                    return true;
                // Wildcard matches filename or full path
                if (pattern.Contains("*") || pattern.Contains("?"))
                {
                    string regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    string fileName = Path.GetFileName(relativePath);
                    if (Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase)) return true;
                    if (Regex.IsMatch(relativePath, regex, RegexOptions.IgnoreCase)) return true;
                }
                // Bare name with no wildcards: match any folder segment or exact filename
                else if (!pattern.Contains("/"))
                {
                    string[] segments = relativePath.Split('/');
                    foreach (string seg in segments)
                        if (string.Equals(seg, pattern, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }
    }
}
