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
                RestoreWindowBounds();
                splitContainer.SplitterDistance = splitContainer.Width / 2;
                SetButtonStates();
                if (_currentProject != null) DoRefresh();
            };
            this.FormClosing += (s, e) => SaveWindowBounds();
            menuNewProject.Click += (s, e) => EditProject(null);
            menuEditProject.Click += (s, e) => EditProject(_currentProject);
            menuRemoveProject.Click += (s, e) => RemoveCurrentProject();
            menuDeleteRemoteSelected.Click += (s, e) => DeleteRemoteSelected();
            menuDeleteAllRemote.Click += (s, e) => DeleteAllRemote();
            menuExit.Click += (s, e) => Close();
            menuSettings.Click += (s, e) => ShowSettings();
            menuAbout.Click += (s, e) => MessageBox.Show("PushPull for GitHub\n\n" + Assembly.GetExecutingAssembly().GetName().Version + "\n\nby Ope Ltd", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            contextMenuLocal.Opening += (s, e) =>
            {
                var item = listLocal.FocusedItem;
                if (item == null) { e.Cancel = true; return; }
                string folder = FolderOf(((FileEntry)item.Tag).RelativePath);
                menuPushFolder.Text = "Push Folder: " + folder;
                menuPushFolder.Tag = folder;
            };
            menuPushFolder.Click += (s, e) => { string f = menuPushFolder.Tag as string; if (f != null) PushFolder(f); };
            contextMenuRemote.Opening += (s, e) =>
            {
                var item = listRemote.FocusedItem;
                if (item == null) { e.Cancel = true; return; }
                string folder = FolderOf(((FileEntry)item.Tag).RelativePath);
                menuPullFolder.Text = "Pull Folder: " + folder;
                menuPullFolder.Tag = folder;
            };
            menuPullFolder.Click += (s, e) => { string f = menuPullFolder.Tag as string; if (f != null) PullFolder(f); };
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
                DoRefresh();
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
            menuDeleteRemoteSelected.Enabled = hasProject && hasToken;
            menuDeleteAllRemote.Enabled = hasProject && hasToken;
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
            listLocal.Groups.Clear();
            listRemote.Groups.Clear();

            // Build ordered group maps
            var localGroups = new System.Collections.Generic.SortedDictionary<string, ListViewGroup>(StringComparer.OrdinalIgnoreCase);
            var remoteGroups = new System.Collections.Generic.SortedDictionary<string, ListViewGroup>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in _entries)
            {
                string folder = FolderOf(e.RelativePath);
                if (e.ExistsLocally && !localGroups.ContainsKey(folder))
                    localGroups[folder] = new ListViewGroup(folder);
                if ((e.ExistsRemotely || e.Status == SyncStatus.LocalOnly) && !remoteGroups.ContainsKey(folder))
                    remoteGroups[folder] = new ListViewGroup(folder);
            }
            foreach (var g in localGroups.Values) listLocal.Groups.Add(g);
            foreach (var g in remoteGroups.Values) listRemote.Groups.Add(g);

            foreach (var e in _entries)
            {
                string folder = FolderOf(e.RelativePath);

                if (e.ExistsLocally)
                {
                    var item = new ListViewItem(e.DisplayName);
                    item.SubItems.Add(FormatSize(e.LocalSize));
                    item.SubItems.Add(e.LocalModified.ToString("yyyy-MM-dd HH:mm:ss"));
                    item.SubItems.Add(StatusLabel(e.Status, true));
                    item.Tag = e;
                    item.BackColor = StatusColor(e.Status, true);
                    item.Group = localGroups[folder];
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
                    item.Group = remoteGroups[folder];
                    listRemote.Items.Add(item);
                }
            }

            listLocal.EndUpdate();
            listRemote.EndUpdate();
        }

        static string FolderOf(string relativePath)
        {
            int slash = relativePath.LastIndexOfAny(new[] { '/', '\\' });
            return slash >= 0 ? relativePath.Substring(0, slash) : "(root)";
        }

        void PushFolder(string folder)
        {
            var toSync = _entries.FindAll(e =>
                FolderOf(e.RelativePath) == folder &&
                (e.Status == SyncStatus.LocalNewer || e.Status == SyncStatus.LocalOnly));
            if (toSync.Count == 0) { MessageBox.Show("Nothing to push in this folder."); return; }
            RunSync(toSync, push: true);
        }

        void PullFolder(string folder)
        {
            var toSync = _entries.FindAll(e =>
                FolderOf(e.RelativePath) == folder &&
                (e.Status == SyncStatus.RemoteNewer || e.Status == SyncStatus.RemoteOnly));
            if (toSync.Count == 0) { MessageBox.Show("Nothing to pull in this folder."); return; }
            RunSync(toSync, push: false);
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

        void DeleteRemoteSelected()
        {
            var toDelete = GetSelectedEntries(listRemote).FindAll(e => e.ExistsRemotely);
            if (toDelete.Count == 0) { MessageBox.Show("Select remote files to delete.", "Delete Remote"); return; }
            string msg = string.Format("Delete {0} file(s) from GitHub? This cannot be undone.", toDelete.Count);
            if (MessageBox.Show(msg, "Delete Remote Files", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            RunDeleteRemote(toDelete);
        }

        void DeleteAllRemote()
        {
            var toDelete = _entries.FindAll(e => e.ExistsRemotely);
            if (toDelete.Count == 0) { MessageBox.Show("No remote files to delete."); return; }
            string msg = string.Format("Delete ALL {0} remote file(s) from GitHub? This cannot be undone.", toDelete.Count);
            if (MessageBox.Show(msg, "Delete All Remote Files", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            RunDeleteRemote(toDelete);
        }

        void RunDeleteRemote(List<FileEntry> entries)
        {
            var project = _currentProject;
            var token = _config.Token;
            int done = 0, failed = 0;

            SetStatus("Deleting " + entries.Count + " remote file(s)...");
            Cursor = Cursors.WaitCursor;
            SetAllButtons(false);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                foreach (var e in entries)
                {
                    if (GitHub.DeleteFile(token, project.Owner, project.Repo, project.Branch, e.RelativePath, e.RemoteSha))
                        done++;
                    else
                        failed++;
                }

                Invoke((Action)(() =>
                {
                    Cursor = Cursors.Default;
                    SetAllButtons(true);
                    SetStatus(string.Format("Delete complete. {0} deleted, {1} failed.", done, failed));
                    DoRefresh();
                }));
            });
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

        void SaveWindowBounds()
        {
            if (WindowState == FormWindowState.Normal)
            {
                _config.WindowX = Location.X;
                _config.WindowY = Location.Y;
                _config.WindowWidth = Width;
                _config.WindowHeight = Height;
                ConfigManager.Save(_config);
            }
        }

        void RestoreWindowBounds()
        {
            if (_config.WindowWidth > 0 && _config.WindowHeight > 0)
            {
                var bounds = new System.Drawing.Rectangle(_config.WindowX, _config.WindowY, _config.WindowWidth, _config.WindowHeight);
                if (System.Windows.Forms.Screen.FromRectangle(bounds).WorkingArea.IntersectsWith(bounds))
                {
                    Location = bounds.Location;
                    Size = bounds.Size;
                }
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
