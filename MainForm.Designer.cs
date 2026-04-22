namespace PushPull
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuNewProject = new System.Windows.Forms.ToolStripMenuItem();
            this.menuEditProject = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRemoveProject = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileSep = new System.Windows.Forms.ToolStripSeparator();
            this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRemote = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDeleteRemoteSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDeleteAllRemote = new System.Windows.Forms.ToolStripMenuItem();
            this.menuTools = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAbout = new System.Windows.Forms.ToolStripMenuItem();

            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnPushSelected = new System.Windows.Forms.ToolStripButton();
            this.btnPullSelected = new System.Windows.Forms.ToolStripButton();
            this.toolSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnPushAll = new System.Windows.Forms.ToolStripButton();
            this.btnPullAll = new System.Windows.Forms.ToolStripButton();

            this.topPanel = new System.Windows.Forms.Panel();
            this.lblProject = new System.Windows.Forms.Label();
            this.cboProject = new System.Windows.Forms.ComboBox();

            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.lblLocal = new System.Windows.Forms.Label();
            this.listLocal = new System.Windows.Forms.ListView();
            this.colLocalName = new System.Windows.Forms.ColumnHeader();
            this.colLocalSize = new System.Windows.Forms.ColumnHeader();
            this.colLocalModified = new System.Windows.Forms.ColumnHeader();
            this.colLocalStatus = new System.Windows.Forms.ColumnHeader();
            this.lblRemote = new System.Windows.Forms.Label();
            this.listRemote = new System.Windows.Forms.ListView();
            this.colRemoteName = new System.Windows.Forms.ColumnHeader();
            this.colRemoteSize = new System.Windows.Forms.ColumnHeader();
            this.colRemoteSha = new System.Windows.Forms.ColumnHeader();
            this.colRemoteStatus = new System.Windows.Forms.ColumnHeader();

            this.contextMenuLocal = new System.Windows.Forms.ContextMenuStrip();
            this.menuPushFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuRemote = new System.Windows.Forms.ContextMenuStrip();
            this.menuPullFolder = new System.Windows.Forms.ToolStripMenuItem();

            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();

            // menuStrip
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuFile, this.menuRemote, this.menuTools, this.menuHelp });
            this.menuFile.Text = "&File";
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuNewProject, this.menuEditProject, this.menuRemoveProject,
                this.menuFileSep, this.menuExit });
            this.menuRemote.Text = "&Remote";
            this.menuRemote.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuDeleteRemoteSelected, this.menuDeleteAllRemote });
            this.menuDeleteRemoteSelected.Text = "Delete Selected Remote Files...";
            this.menuDeleteAllRemote.Text = "Delete All Remote Files...";
            this.menuNewProject.Text = "&New Project...";
            this.menuEditProject.Text = "&Edit Project...";
            this.menuRemoveProject.Text = "&Remove Project";
            this.menuExit.Text = "E&xit";
            this.menuTools.Text = "&Options";
            this.menuTools.DropDownItems.Add(this.menuSettings);
            this.menuSettings.Text = "&Settings...";
            this.menuHelp.Text = "&Help";
            this.menuHelp.DropDownItems.Add(this.menuAbout);
            this.menuAbout.Text = "&About...";

            // toolStrip
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnPushSelected.Text = "Push Selected";
            this.btnPushSelected.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnPullSelected.Text = "Pull Selected";
            this.btnPullSelected.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnPushAll.Text = "Push All";
            this.btnPushAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnPullAll.Text = "Pull All";
            this.btnPullAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.btnRefresh, this.toolSep1,
                this.btnPushSelected, this.btnPushAll,
                this.toolSep2, this.btnPullSelected, this.btnPullAll });

            // topPanel
            this.topPanel.Height = 32;
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Padding = new System.Windows.Forms.Padding(4, 4, 4, 0);
            this.lblProject.Text = "Project:";
            this.lblProject.AutoSize = true;
            this.lblProject.Location = new System.Drawing.Point(4, 8);
            this.cboProject.Location = new System.Drawing.Point(60, 4);
            this.cboProject.Width = 300;
            this.cboProject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.topPanel.Controls.Add(this.lblProject);
            this.topPanel.Controls.Add(this.cboProject);

            // splitContainer
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Vertical;

            // local pane
            this.lblLocal.Text = "Local";
            this.lblLocal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLocal.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            this.lblLocal.Height = 20;
            this.colLocalName.Text = "Name";
            this.colLocalName.Width = 280;
            this.colLocalSize.Text = "Size";
            this.colLocalSize.Width = 70;
            this.colLocalModified.Text = "Modified";
            this.colLocalModified.Width = 130;
            this.colLocalStatus.Text = "Status";
            this.colLocalStatus.Width = 90;
            this.listLocal.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colLocalName, this.colLocalSize, this.colLocalModified, this.colLocalStatus });
            this.listLocal.View = System.Windows.Forms.View.Details;
            this.listLocal.FullRowSelect = true;
            this.listLocal.GridLines = true;
            this.listLocal.HideSelection = false;
            this.listLocal.ShowGroups = true;
            this.listLocal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listLocal.OwnerDraw = false;
            this.contextMenuLocal.Items.Add(this.menuPushFolder);
            this.menuPushFolder.Text = "Push Folder";
            this.listLocal.ContextMenuStrip = this.contextMenuLocal;
            this.splitContainer.Panel1.Controls.Add(this.listLocal);
            this.splitContainer.Panel1.Controls.Add(this.lblLocal);

            // remote pane
            this.lblRemote.Text = "GitHub (Remote)";
            this.lblRemote.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRemote.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            this.lblRemote.Height = 20;
            this.colRemoteName.Text = "Name";
            this.colRemoteName.Width = 280;
            this.colRemoteSize.Text = "Size";
            this.colRemoteSize.Width = 70;
            this.colRemoteSha.Text = "SHA";
            this.colRemoteSha.Width = 80;
            this.colRemoteStatus.Text = "Status";
            this.colRemoteStatus.Width = 90;
            this.listRemote.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colRemoteName, this.colRemoteSize, this.colRemoteSha, this.colRemoteStatus });
            this.listRemote.View = System.Windows.Forms.View.Details;
            this.listRemote.FullRowSelect = true;
            this.listRemote.GridLines = true;
            this.listRemote.HideSelection = false;
            this.listRemote.ShowGroups = true;
            this.listRemote.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contextMenuRemote.Items.Add(this.menuPullFolder);
            this.menuPullFolder.Text = "Pull Folder";
            this.listRemote.ContextMenuStrip = this.contextMenuRemote;
            this.splitContainer.Panel2.Controls.Add(this.listRemote);
            this.splitContainer.Panel2.Controls.Add(this.lblRemote);

            // statusStrip
            this.statusStrip.Items.Add(this.statusLabel);
            this.statusLabel.Text = "Ready";
            this.statusLabel.Spring = true;
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 620);
            this.Text = "PushPull for GitHub";
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.statusStrip);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new System.Drawing.Size(700, 400);
        }

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem menuFile;
        private System.Windows.Forms.ToolStripMenuItem menuNewProject;
        private System.Windows.Forms.ToolStripMenuItem menuEditProject;
        private System.Windows.Forms.ToolStripMenuItem menuRemoveProject;
        private System.Windows.Forms.ToolStripSeparator menuFileSep;
        private System.Windows.Forms.ToolStripMenuItem menuExit;
        private System.Windows.Forms.ToolStripMenuItem menuRemote;
        private System.Windows.Forms.ToolStripMenuItem menuDeleteRemoteSelected;
        private System.Windows.Forms.ToolStripMenuItem menuDeleteAllRemote;
        private System.Windows.Forms.ToolStripMenuItem menuTools;
        private System.Windows.Forms.ToolStripMenuItem menuSettings;
        private System.Windows.Forms.ToolStripMenuItem menuHelp;
        private System.Windows.Forms.ToolStripMenuItem menuAbout;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton btnRefresh;
        private System.Windows.Forms.ToolStripSeparator toolSep1;
        private System.Windows.Forms.ToolStripButton btnPushSelected;
        private System.Windows.Forms.ToolStripButton btnPullSelected;
        private System.Windows.Forms.ToolStripSeparator toolSep2;
        private System.Windows.Forms.ToolStripButton btnPushAll;
        private System.Windows.Forms.ToolStripButton btnPullAll;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Label lblProject;
        private System.Windows.Forms.ComboBox cboProject;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Label lblLocal;
        private System.Windows.Forms.ListView listLocal;
        private System.Windows.Forms.ColumnHeader colLocalName;
        private System.Windows.Forms.ColumnHeader colLocalSize;
        private System.Windows.Forms.ColumnHeader colLocalModified;
        private System.Windows.Forms.ColumnHeader colLocalStatus;
        private System.Windows.Forms.Label lblRemote;
        private System.Windows.Forms.ListView listRemote;
        private System.Windows.Forms.ColumnHeader colRemoteName;
        private System.Windows.Forms.ColumnHeader colRemoteSize;
        private System.Windows.Forms.ColumnHeader colRemoteSha;
        private System.Windows.Forms.ColumnHeader colRemoteStatus;
        private System.Windows.Forms.ContextMenuStrip contextMenuLocal;
        private System.Windows.Forms.ToolStripMenuItem menuPushFolder;
        private System.Windows.Forms.ContextMenuStrip contextMenuRemote;
        private System.Windows.Forms.ToolStripMenuItem menuPullFolder;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
    }
}
