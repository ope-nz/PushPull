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
