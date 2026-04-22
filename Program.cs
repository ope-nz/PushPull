using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PushPull
{
    static class Program
    {
        [DllImport("kernel32.dll")] static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll")] static extern bool AllocConsole();

        [STAThread]
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            if (args.Length > 0)
            {
                if (!AttachConsole(-1)) AllocConsole();
                var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(writer);
                Environment.Exit(RunCliPush(args[0]));
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        static int RunCliPush(string projectName)
        {
            var config = ConfigManager.Load();

            if (string.IsNullOrWhiteSpace(config.Token))
            {
                Console.WriteLine("Error: no GitHub token configured. Open the app and set one via Tools > Settings.");
                return 1;
            }

            GfdProject project = null;
            foreach (var p in config.Projects)
            {
                if (string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.ToString(), projectName, StringComparison.OrdinalIgnoreCase))
                {
                    project = p;
                    break;
                }
            }

            if (project == null)
            {
                Console.WriteLine("Error: project '" + projectName + "' not found.");
                if (config.Projects.Count > 0)
                {
                    Console.WriteLine("Known projects:");
                    foreach (var p in config.Projects) Console.WriteLine("  " + p);
                }
                return 1;
            }

            Console.WriteLine("Project: " + project);
            Console.WriteLine("Fetching remote file list...");

            Dictionary<string, GitHub.RemoteFile> remote;
            try
            {
                remote = GitHub.GetRepoTree(config.Token, project.Owner, project.Repo, project.Branch);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 1;
            }

            var entries = SyncEngine.Compare(project, remote);
            var toPush = entries.FindAll(e => e.Status == SyncStatus.LocalNewer || e.Status == SyncStatus.LocalOnly);

            if (toPush.Count == 0)
            {
                Console.WriteLine("Nothing to push.");
                return 0;
            }

            Console.WriteLine("Pushing " + toPush.Count + " file(s)...");
            int done = 0, failed = 0;

            foreach (var e in toPush)
            {
                string localPath = Path.Combine(project.LocalFolder, e.RelativePath.Replace('/', '\\'));
                try
                {
                    GitHub.UploadFile(config.Token, project.Owner, project.Repo, project.Branch,
                        e.RelativePath, localPath, e.ExistsRemotely ? e.RemoteSha : null);
                    Console.WriteLine("  OK    " + e.RelativePath);
                    done++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  FAIL  " + e.RelativePath + " (" + ex.Message + ")");
                    failed++;
                }
            }

            Console.WriteLine(string.Format("Done: {0} pushed, {1} failed.", done, failed));
            return done == 0 ? 1 : 0;
        }
    }
}
