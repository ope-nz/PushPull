using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace PushPull
{
    public static class GitHub
    {
        const string UserAgent = "PushPull-app";

        static HttpWebRequest MakeRequest(string url, string method, string token)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            req.Headers.Add("Authorization", "token " + token);
            req.UserAgent = UserAgent;
            req.Timeout = 30000;
            return req;
        }

        static string ReadResponse(HttpWebResponse resp)
        {
            using (var sr = new StreamReader(resp.GetResponseStream()))
                return sr.ReadToEnd();
        }

        public static bool CheckRepoExists(string token, string owner, string repo)
        {
            try
            {
                var req = MakeRequest("https://api.github.com/repos/" + owner + "/" + repo, "GET", token);
                var resp = (HttpWebResponse)req.GetResponse();
                resp.Close();
                return (int)resp.StatusCode == 200;
            }
            catch { return false; }
        }

        // Returns flat dict of relativePath -> sha using recursive tree API (single request)
        public static Dictionary<string, RemoteFile> GetRepoTree(string token, string owner, string repo, string branch)
        {
            var result = new Dictionary<string, RemoteFile>(StringComparer.OrdinalIgnoreCase);
            try
            {
                // First get the branch to find its tree SHA
                var req = MakeRequest(
                    "https://api.github.com/repos/" + owner + "/" + repo + "/branches/" + branch,
                    "GET", token);
                HttpWebResponse resp;
                try
                {
                    resp = (HttpWebResponse)req.GetResponse();
                }
                catch (WebException ex)
                {
                    // 404 = branch doesn't exist yet (empty repo with no commits)
                    var r = ex.Response as HttpWebResponse;
                    if (r != null && (int)r.StatusCode == 404)
                        return result;
                    throw;
                }
                string json = ReadResponse(resp);
                resp.Close();

                var ser = new JavaScriptSerializer();
                var branchObj = ser.Deserialize<Dictionary<string, object>>(json);
                var commit = (Dictionary<string, object>)branchObj["commit"];
                var commitObj = (Dictionary<string, object>)commit["commit"];
                var tree = (Dictionary<string, object>)commitObj["tree"];
                string treeSha = (string)tree["sha"];

                // Now get recursive tree
                var req2 = MakeRequest(
                    "https://api.github.com/repos/" + owner + "/" + repo + "/git/trees/" + treeSha + "?recursive=1",
                    "GET", token);
                var resp2 = (HttpWebResponse)req2.GetResponse();
                string json2 = ReadResponse(resp2);
                resp2.Close();

                var treeObj = ser.Deserialize<Dictionary<string, object>>(json2);
                var nodes = (System.Collections.ArrayList)treeObj["tree"];

                foreach (Dictionary<string, object> node in nodes)
                {
                    string type = (string)node["type"];
                    if (type != "blob") continue;
                    string path = (string)node["path"];
                    string sha = (string)node["sha"];
                    int size = node.ContainsKey("size") && node["size"] != null ? Convert.ToInt32(node["size"]) : 0;
                    result[path] = new RemoteFile { Path = path, Sha = sha, Size = size };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetRepoTree failed: " + ex.Message, ex);
            }
            return result;
        }

        public static byte[] DownloadFile(string token, string owner, string repo, string branch, string path)
        {
            var req = MakeRequest(
                "https://api.github.com/repos/" + owner + "/" + repo + "/contents/" + Uri.EscapeUriString(path) + "?ref=" + branch,
                "GET", token);
            req.Accept = "application/vnd.github.v3.raw";
            var resp = (HttpWebResponse)req.GetResponse();
            using (var ms = new MemoryStream())
            {
                resp.GetResponseStream().CopyTo(ms);
                resp.Close();
                return ms.ToArray();
            }
        }

        public static bool UploadFile(string token, string owner, string repo, string branch,
            string relativePath, string localFullPath, string existingSha, string commitMessage = "PushPull update")
        {
            byte[] content = File.ReadAllBytes(localFullPath);
            string encoded = Convert.ToBase64String(content);
            string url = "https://api.github.com/repos/" + owner + "/" + repo + "/contents/" + relativePath.Replace("\\", "/");

            var req = MakeRequest(url, "PUT", token);
            req.ContentType = "application/json";

            string safeMsg = commitMessage.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
            var sb = new StringBuilder("{");
            sb.Append("\"message\":\"" + safeMsg + "\"");
            sb.Append(",\"branch\":\"" + branch + "\"");
            sb.Append(",\"content\":\"" + encoded + "\"");
            if (existingSha != null) sb.Append(",\"sha\":\"" + existingSha + "\"");
            sb.Append("}");

            using (var sw = new StreamWriter(req.GetRequestStream()))
                sw.Write(sb.ToString());

            try
            {
                var resp = (HttpWebResponse)req.GetResponse();
                int code = (int)resp.StatusCode;
                resp.Close();
                return code == 200 || code == 201;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var sr = new StreamReader(ex.Response.GetResponseStream()))
                        throw new Exception("UploadFile error: " + sr.ReadToEnd(), ex);
                }
                throw;
            }
        }

        public static bool DeleteFile(string token, string owner, string repo, string branch,
            string remotePath, string sha)
        {
            string url = "https://api.github.com/repos/" + owner + "/" + repo + "/contents/" + remotePath.Replace("\\", "/");
            var req = MakeRequest(url, "DELETE", token);
            req.ContentType = "application/json";

            var body = "{\"message\":\"PushPull delete\",\"branch\":\"" + branch + "\",\"sha\":\"" + sha + "\"}";
            using (var sw = new StreamWriter(req.GetRequestStream()))
                sw.Write(body);

            try
            {
                var resp = (HttpWebResponse)req.GetResponse();
                int code = (int)resp.StatusCode;
                resp.Close();
                return code == 200;
            }
            catch { return false; }
        }

        public class BatchChange
        {
            public string RelativePath { get; set; }
            public string LocalFullPath { get; set; } // null = delete the remote file
        }

        // Phase 4: one commit for the whole set via the Git Data API:
        // blobs -> tree (on the tip's base tree) -> commit -> ref update.
        // Throws on any failure; callers fall back to the per-file Contents loop
        // (which also handles the empty-repo case this API cannot).
        public static void PushBatch(string token, string owner, string repo, string branch,
            List<BatchChange> items, string commitMessage = "PushPull update")
        {
            if (items.Count == 0) return;
            string baseUrl = "https://api.github.com/repos/" + owner + "/" + repo;
            var ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

            // Branch tip and its tree
            var refObj = ser.Deserialize<Dictionary<string, object>>(
                GetJson(baseUrl + "/git/ref/heads/" + branch, token));
            string tip = (string)((Dictionary<string, object>)refObj["object"])["sha"];

            var commitObj = ser.Deserialize<Dictionary<string, object>>(
                GetJson(baseUrl + "/git/commits/" + tip, token));
            string baseTree = (string)((Dictionary<string, object>)commitObj["tree"])["sha"];

            // One blob per upload; a null sha in a tree entry deletes that path
            var treeEntries = new List<Dictionary<string, object>>();
            foreach (var it in items)
            {
                string path = it.RelativePath.Replace('\\', '/');
                if (it.LocalFullPath == null)
                {
                    treeEntries.Add(new Dictionary<string, object>
                    { { "path", path }, { "mode", "100644" }, { "type", "blob" }, { "sha", null } });
                    continue;
                }
                byte[] content = File.ReadAllBytes(it.LocalFullPath);
                var blobResp = ser.Deserialize<Dictionary<string, object>>(
                    SendJson(baseUrl + "/git/blobs", "POST", token, ser.Serialize(new Dictionary<string, object>
                    { { "content", Convert.ToBase64String(content) }, { "encoding", "base64" } })));
                treeEntries.Add(new Dictionary<string, object>
                { { "path", path }, { "mode", "100644" }, { "type", "blob" }, { "sha", (string)blobResp["sha"] } });
            }

            var treeResp = ser.Deserialize<Dictionary<string, object>>(
                SendJson(baseUrl + "/git/trees", "POST", token, ser.Serialize(new Dictionary<string, object>
                { { "base_tree", baseTree }, { "tree", treeEntries } })));

            var newCommitResp = ser.Deserialize<Dictionary<string, object>>(
                SendJson(baseUrl + "/git/commits", "POST", token, ser.Serialize(new Dictionary<string, object>
                { { "message", commitMessage }, { "tree", (string)treeResp["sha"] }, { "parents", new object[] { tip } } })));

            // Fast-forward the branch; fails if someone pushed meanwhile (caller falls back)
            SendJson(baseUrl + "/git/refs/heads/" + branch, "PATCH", token, ser.Serialize(new Dictionary<string, object>
            { { "sha", (string)newCommitResp["sha"] } }));
        }

        static string GetJson(string url, string token)
        {
            var req = MakeRequest(url, "GET", token);
            var resp = (HttpWebResponse)req.GetResponse();
            string json = ReadResponse(resp);
            resp.Close();
            return json;
        }

        static string SendJson(string url, string method, string token, string body)
        {
            var req = MakeRequest(url, method, token);
            req.ContentType = "application/json";
            using (var sw = new StreamWriter(req.GetRequestStream()))
                sw.Write(body);
            try
            {
                var resp = (HttpWebResponse)req.GetResponse();
                string json = ReadResponse(resp);
                resp.Close();
                return json;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var sr = new StreamReader(ex.Response.GetResponseStream()))
                        throw new Exception("PushBatch error: " + sr.ReadToEnd(), ex);
                }
                throw;
            }
        }

        public static List<string> GetRepos(string token, string owner)
        {
            var result = new List<string>();
            try
            {
                // /user/repos includes private repos; /users/{owner}/repos is public-only
                var req = MakeRequest(
                    "https://api.github.com/user/repos?per_page=100&sort=updated",
                    "GET", token);
                var resp = (HttpWebResponse)req.GetResponse();
                string json = ReadResponse(resp);
                resp.Close();

                var ser = new JavaScriptSerializer();
                var repos = ser.Deserialize<List<Dictionary<string, object>>>(json);
                foreach (var r in repos)
                {
                    var repoOwner = (Dictionary<string, object>)r["owner"];
                    if (string.Equals((string)repoOwner["login"], owner, StringComparison.OrdinalIgnoreCase))
                        result.Add((string)r["name"]);
                }

                // Owner isn't the token's account or one of its orgs; list their public repos
                if (result.Count == 0)
                {
                    var req2 = MakeRequest(
                        "https://api.github.com/users/" + owner + "/repos?per_page=100&sort=updated",
                        "GET", token);
                    var resp2 = (HttpWebResponse)req2.GetResponse();
                    string json2 = ReadResponse(resp2);
                    resp2.Close();

                    var repos2 = ser.Deserialize<List<Dictionary<string, object>>>(json2);
                    foreach (var r in repos2)
                        result.Add((string)r["name"]);
                }

                result.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch { }
            return result;
        }

        public static List<string> GetBranches(string token, string owner, string repo)
        {
            var result = new List<string>();
            try
            {
                var req = MakeRequest(
                    "https://api.github.com/repos/" + owner + "/" + repo + "/branches",
                    "GET", token);
                var resp = (HttpWebResponse)req.GetResponse();
                string json = ReadResponse(resp);
                resp.Close();

                var ser = new JavaScriptSerializer();
                var branches = ser.Deserialize<List<Dictionary<string, object>>>(json);
                foreach (var b in branches)
                    result.Add((string)b["name"]);
            }
            catch { }
            return result;
        }

        // Computes the Git blob SHA1 of a local file (matches what GitHub stores)
        public static string CalcLocalSha(string filePath)
        {
            byte[] data = File.ReadAllBytes(filePath);
            byte[] prefix = Encoding.ASCII.GetBytes("blob " + data.Length + "\0");
            byte[] blob = new byte[prefix.Length + data.Length];
            Buffer.BlockCopy(prefix, 0, blob, 0, prefix.Length);
            Buffer.BlockCopy(data, 0, blob, prefix.Length, data.Length);
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(blob);
                var sb = new StringBuilder();
                foreach (byte b in hash) sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }

        public class RemoteFile
        {
            public string Path { get; set; }
            public string Sha { get; set; }
            public int Size { get; set; }
        }
    }
}
