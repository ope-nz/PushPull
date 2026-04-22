using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace PushPull
{
    public partial class MainForm : Form
    {
        GfdConfig _config;
        GfdProject _currentProject;
        List<FileEntry> _entries = new List<FileEntry>();

        static readonly Color ColorLocalNewer = Color.FromArgb(200, 255, 200);
        static readonly Color ColorRemoteNewer = Color.FromArgb(200, 220, 255);
        static readonly Color ColorLocalOnly = Color.FromArgb(180, 255, 180);
        static readonly Color ColorRemoteOnly = Color.FromArgb(180, 200, 255);

        public MainForm()
        {
            InitializeComponent();
            AppIcon.Apply(this);
            WireEvents();
            LoadConfig();
        }

        void WireEvents()
        {
            this.Load += (s, e) =>
            {
                splitContainer.SplitterDistance = splitContainer.Width / 2;
                SetButtonStates();
                if (_currentProject != null) DoRefresh();
            };
            menuNewProject.Click += (s, e) => EditProject(null);
            menuEditProject.Click += (s, e) => EditProject(_currentProject);
            menuRemoveProject.Click += (s, e) => RemoveCurrentProject();
            menuExit.Click += (s, e) => Close();
            menuSettings.Click += (s, e) => ShowSettings();
            menuAbout.Click += (s, e) => MessageBox.Show("PushPull for GitHub", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnRefresh.Click += (s, e) => DoRefresh();
            btnPushSelected.Click += (s, e) => PushSelected();
            btnPullSelected.Click += (s, e) => PullSelected();
            btnPushAll.Click += (s, e) => PushAll();
            btnPullAll.Click += (s, e) => PullAll();
            cboProject.SelectedIndexChanged += (s, e) => OnProjectChanged();
            listLocal.ColumnClick += (s, e) => SortList(listLocal, e.Column);
            listRemote.ColumnClick += (s, e) => SortList(listRemote, e.Column);
        }

        void LoadConfig()
        {
            _config = ConfigManager.Load();
            RefreshProjectCombo();
        }

        void RefreshProjectCombo()
        {
            cboProject.Items.Clear();
            foreach (var p in _config.Projects) cboProject.Items.Add(p);

            // Restore last used project
            int restoreIdx = 0;
            if (!string.IsNullOrEmpty(_config.LastProjectName))
            {
                for (int i = 0; i < cboProject.Items.Count; i++)
                {
                    if (cboProject.Items[i].ToString() == _config.LastProjectName)
                    { restoreIdx = i; break; }
                }
            }
            if (cboProject.Items.Count > 0) cboProject.SelectedIndex = restoreIdx;
            SetButtonStates();
        }

        void OnProjectChanged()
        {
            _currentProject = cboProject.SelectedItem as GfdProject;
            _entries.Clear();
            listLocal.Items.Clear();
            listRemote.Items.Clear();
            SetButtonStates();
            if (_currentProject != null)
            {
                lblLocal.Text = "Local: " + _currentProject.LocalFolder;
                _config.LastProjectName = _currentProject.ToString();
                ConfigManager.Save(_config);
            }
        }

        void SetButtonStates()
        {
            bool hasProject = _currentProject != null;
            bool hasToken = _config != null && !string.IsNullOrWhiteSpace(_config.Token);
            btnRefresh.Enabled = hasProject && hasToken;
            btnPushSelected.Enabled = hasProject && hasToken;
            btnPullSelected.Enabled = hasProject && hasToken;
            btnPushAll.Enabled = hasProject && hasToken;
            btnPullAll.Enabled = hasProject && hasToken;
            menuEditProject.Enabled = hasProject;
            menuRemoveProject.Enabled = hasProject;
        }

        void DoRefresh()
        {
            if (_currentProject == null) return;
            SetStatus("Fetching remote file list...");
            Cursor = Cursors.WaitCursor;
            btnRefresh.Enabled = false;

            var project = _currentProject;
            var token = _config.Token;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                List<FileEntry> entries = null;
                string error = null;
                try
                {
                    var remote = GitHub.GetRepoTree(token, project.Owner, project.Repo, project.Branch);
                    entries = SyncEngine.Compare(project, remote);
                }
                catch (Exception ex) { error = ex.Message; }

                Invoke((Action)(() =>
                {
                    Cursor = Cursors.Default;
                    btnRefresh.Enabled = true;
                    if (error != null) { SetStatus("Error: " + error); MessageBox.Show(error, "Refresh Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                    _entries = entries;
                    PopulateLists();
                    SetStatus("Refreshed. " + entries.Count + " files.");
                }));
            });
        }

        void PopulateLists()
        {
            listLocal.BeginUpdate();
            listRemote.BeginUpdate();
            listLocal.Items.Clear();
            listRemote.Items.Clear();

            foreach (var e in _entries)
            {
                if (e.ExistsLocally)
                {
                    var item = new ListViewItem(e.DisplayName);
                    item.SubItems.Add(FormatSize(e.LocalSize));
                    item.SubItems.Add(e.LocalModified.ToString("yyyy-MM-dd HH:mm:ss"));
                    item.SubItems.Add(StatusLabel(e.Status, true));
                    item.Tag = e;
                    item.BackColor = StatusColor(e.Status, true);
                    listLocal.Items.Add(item);
                }

                if (e.ExistsRemotely || e.Status == SyncStatus.LocalOnly)
                {
                    var item = new ListViewItem(e.DisplayName);
                    item.SubItems.Add(e.ExistsRemotely ? FormatSize(e.RemoteSize) : "");
                    item.SubItems.Add(e.ExistsRemotely ? e.RemoteSha.Substring(0, 7) : "");
                    item.SubItems.Add(StatusLabel(e.Status, false));
                    item.Tag = e;
                    item.BackColor = StatusColor(e.Status, false);
                    listRemote.Items.Add(item);
                }
            }

            listLocal.EndUpdate();
            listRemote.EndUpdate();
        }

        string StatusLabel(SyncStatus s, bool local)
        {
            switch (s)
            {
                case SyncStatus.Same: return "Same";
                case SyncStatus.LocalNewer: return local ? "Changed" : "Outdated";
                case SyncStatus.RemoteNewer: return local ? "Outdated" : "Changed";
                case SyncStatus.LocalOnly: return local ? "Local Only" : "Not on GitHub";
                case SyncStatus.RemoteOnly: return local ? "Not Local" : "Remote Only";
                default: return "";
            }
        }

        Color StatusColor(SyncStatus s, bool local)
        {
            switch (s)
            {
                case SyncStatus.LocalNewer: return local ? ColorLocalNewer : SystemColors.Window;
                case SyncStatus.RemoteNewer: return local ? SystemColors.Window : ColorRemoteNewer;
                case SyncStatus.LocalOnly: return local ? ColorLocalOnly : SystemColors.Window;
                case SyncStatus.RemoteOnly: return local ? SystemColors.Window : ColorRemoteOnly;
                default: return SystemColors.Window;
            }
        }

        void PushSelected()
        {
            var toSync = GetSelectedEntries(listLocal);
            if (toSync.Count == 0) { MessageBox.Show("Select files in the Local pane to push.", "Push Selected"); return; }
            RunSync(toSync, push: true);
        }

        void PullSelected()
        {
            var toSync = GetSelectedEntries(listRemote);
            if (toSync.Count == 0) { MessageBox.Show("Select files in the Remote pane to pull.", "Pull Selected"); return; }
            RunSync(toSync, push: false);
        }

        void PushAll()
        {
            var toSync = _entries.FindAll(e => e.Status == SyncStatus.LocalNewer || e.Status == SyncStatus.LocalOnly);
            if (toSync.Count == 0) { MessageBox.Show("Nothing to push."); return; }
            RunSync(toSync, push: true);
        }

        void PullAll()
        {
            var toSync = _entries.FindAll(e => e.Status == SyncStatus.RemoteNewer || e.Status == SyncStatus.RemoteOnly);
            if (toSync.Count == 0) { MessageBox.Show("Nothing to pull."); return; }
            RunSync(toSync, push: false);
        }

        List<FileEntry> GetSelectedEntries(ListView lv)
        {
            var list = new List<FileEntry>();
            foreach (ListViewItem item in lv.SelectedItems)
            {
                FileEntry e = item.Tag as FileEntry;
                if (e != null) list.Add(e);
            }
            return list;
        }

        void RunSync(List<FileEntry> entries, bool push)
        {
            var project = _currentProject;
            var token = _config.Token;
            int done = 0, failed = 0;

            SetStatus((push ? "Pushing" : "Pulling") + " " + entries.Count + " file(s)...");
            Cursor = Cursors.WaitCursor;
            SetAllButtons(false);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                foreach (var e in entries)
                {
                    try
                    {
                        if (push)
                        {
                            string localPath = Path.Combine(project.LocalFolder, e.RelativePath.Replace('/', '\\'));
                            GitHub.UploadFile(token, project.Owner, project.Repo, project.Branch,
                                e.RelativePath, localPath, e.ExistsRemotely ? e.RemoteSha : null);
                        }
                        else
                        {
                            byte[] data = GitHub.DownloadFile(token, project.Owner, project.Repo, project.Branch, e.RelativePath);
                            string localPath = Path.Combine(project.LocalFolder, e.RelativePath.Replace('/', '\\'));
                            Directory.CreateDirectory(Path.GetDirectoryName(localPath));
                            File.WriteAllBytes(localPath, data);
                        }
                        done++;
                    }
                    catch { failed++; }
                }

                Invoke((Action)(() =>
                {
                    Cursor = Cursors.Default;
                    SetAllButtons(true);
                    SetStatus(string.Format("{0} complete. {1} OK, {2} failed.", push ? "Push" : "Pull", done, failed));
                    DoRefresh();
                }));
            });
        }

        void SetAllButtons(bool enabled)
        {
            btnRefresh.Enabled = enabled;
            btnPushSelected.Enabled = enabled;
            btnPullSelected.Enabled = enabled;
            btnPushAll.Enabled = enabled;
            btnPullAll.Enabled = enabled;
        }

        void EditProject(GfdProject existing)
        {
            using (var dlg = new ProjectDialog(_config, existing))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (existing == null)
                        _config.Projects.Add(dlg.Project);
                    else
                    {
                        int idx = _config.Projects.IndexOf(existing);
                        _config.Projects[idx] = dlg.Project;
                    }
                    ConfigManager.Save(_config);
                    RefreshProjectCombo();
                    DoRefresh();
                }
            }
        }

        void RemoveCurrentProject()
        {
            if (_currentProject == null) return;
            if (MessageBox.Show("Remove project '" + _currentProject + "'?", "Remove",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _config.Projects.Remove(_currentProject);
                ConfigManager.Save(_config);
                _currentProject = null;
                RefreshProjectCombo();
            }
        }

        void ShowSettings()
        {
            using (var dlg = new SettingsDialog(_config))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    ConfigManager.Save(_config);
                    SetButtonStates();
                }
            }
        }

        void SetStatus(string msg)
        {
            statusLabel.Text = msg;
        }

        static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024) + " KB";
            return (bytes / (1024 * 1024)) + " MB";
        }

        int _sortCol = -1;
        bool _sortAsc = true;

        void SortList(ListView lv, int col)
        {
            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
            lv.ListViewItemSorter = new ListViewSorter(col, _sortAsc);
            lv.Sort();
        }
    }

    class ListViewSorter : System.Collections.IComparer
    {
        int _col;
        bool _asc;
        public ListViewSorter(int col, bool asc) { _col = col; _asc = asc; }
        public int Compare(object x, object y)
        {
            var a = ((ListViewItem)x).SubItems[_col].Text;
            var b = ((ListViewItem)y).SubItems[_col].Text;
            int r = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            return _asc ? r : -r;
        }
    }
}
