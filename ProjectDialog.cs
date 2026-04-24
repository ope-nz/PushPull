using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace PushPull
{
    public class ProjectDialog : Form
    {
        GfdConfig _config;
        GfdProject _original;
        public GfdProject Project { get; private set; }

        TextBox txtName, txtFolder, txtOwner, txtRepo, txtIgnore;
        ComboBox cboBranch;
        Button btnLoadBranches;

        public ProjectDialog(GfdConfig config, GfdProject existing)
        {
            _config = config;
            _original = existing;
            BuildUI();
            AppIcon.Apply(this);
            if (existing != null)
                Populate(existing);
            else
            {
                if (!string.IsNullOrEmpty(config.DefaultOwner))
                    txtOwner.Text = config.DefaultOwner;
                if (config.DefaultIgnorePatterns != null && config.DefaultIgnorePatterns.Count > 0)
                    txtIgnore.Text = string.Join("\r\n", config.DefaultIgnorePatterns);
            }
        }

        void BuildUI()
        {
            this.Text = _original == null ? "New Project" : "Edit Project";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new System.Drawing.Size(500, 310);
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            int y = 12;
            int lw = 110, tw = 360, lx = 12, tx = 125;

            AddRow("Name:", ref y, lx, tx, lw, tw, out txtName);
            AddFolderRow("Local Folder:", ref y, lx, tx, lw, out txtFolder);
            AddRow("Owner:", ref y, lx, tx, lw, tw, out txtOwner);
            AddRow("Repo:", ref y, lx, tx, lw, tw, out txtRepo);

            var lblBranch = new Label { Text = "Branch:", Location = new System.Drawing.Point(lx, y + 3), AutoSize = true };
            cboBranch = new ComboBox { Location = new System.Drawing.Point(tx, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDown };
            cboBranch.Items.Add("main");
            cboBranch.SelectedIndex = 0;
            btnLoadBranches = new Button { Text = "Load Branches", Location = new System.Drawing.Point(tx + 210, y - 1), Width = 110 };
            btnLoadBranches.Click += (s, e) => LoadBranches();
            this.Controls.Add(lblBranch);
            this.Controls.Add(cboBranch);
            this.Controls.Add(btnLoadBranches);
            y += 32;

            var lblIgnore = new Label { Text = "Ignore:", Location = new System.Drawing.Point(lx, y + 3), AutoSize = true };
            var lblIgnoreHint = new Label { Text = "(one per line, e.g. *.exe  or  bin/)", Location = new System.Drawing.Point(tx, y + 3), AutoSize = true, ForeColor = System.Drawing.Color.Gray };
            txtIgnore = new TextBox
            {
                Location = new System.Drawing.Point(tx, y + 20),
                Width = tw,
                Height = 70,
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical
            };
            this.Controls.Add(lblIgnore);
            this.Controls.Add(lblIgnoreHint);
            this.Controls.Add(txtIgnore);
            y += 100;

            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(330, y), Width = 75 };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(412, y), Width = 75 };
            btnOk.Click += (s, e) => SaveProject();

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
        }

        void AddRow(string label, ref int y, int lx, int tx, int lw, int tw, out TextBox txt)
        {
            this.Controls.Add(new Label { Text = label, Location = new System.Drawing.Point(lx, y + 3), AutoSize = true });
            txt = new TextBox { Location = new System.Drawing.Point(tx, y), Width = tw };
            this.Controls.Add(txt);
            y += 32;
        }

        void AddFolderRow(string label, ref int y, int lx, int tx, int lw, out TextBox txt)
        {
            this.Controls.Add(new Label { Text = label, Location = new System.Drawing.Point(lx, y + 3), AutoSize = true });
            var tb = new TextBox { Location = new System.Drawing.Point(tx, y), Width = 310 };
            txt = tb;
            var btn = new Button { Text = "...", Location = new System.Drawing.Point(tx + 318, y - 1), Width = 42 };
            btn.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    if (!string.IsNullOrEmpty(tb.Text) && Directory.Exists(tb.Text))
                        dlg.SelectedPath = tb.Text;
                    if (dlg.ShowDialog() == DialogResult.OK) tb.Text = dlg.SelectedPath;
                }
            };
            this.Controls.Add(tb);
            this.Controls.Add(btn);
            y += 32;
        }

        void Populate(GfdProject p)
        {
            txtName.Text = p.Name ?? "";
            txtFolder.Text = p.LocalFolder ?? "";
            txtOwner.Text = p.Owner ?? "";
            txtRepo.Text = p.Repo ?? "";
            cboBranch.Text = p.Branch ?? "main";
            if (p.IgnorePatterns != null)
                txtIgnore.Text = string.Join("\r\n", p.IgnorePatterns);
        }

        void LoadBranches()
        {
            string token = _config.Token;
            string owner = txtOwner.Text.Trim();
            string repo = txtRepo.Text.Trim();
            if (string.IsNullOrEmpty(token)) { MessageBox.Show("Set your GitHub token in Settings first."); return; }
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)) { MessageBox.Show("Enter Owner and Repo first."); return; }
            Cursor = Cursors.WaitCursor;
            try
            {
                var branches = GitHub.GetBranches(token, owner, repo);
                if (branches.Count == 0) { MessageBox.Show("No branches found. The repo may be empty (no commits yet) - just type the branch name, e.g. \"main\"."); return; }
                cboBranch.Items.Clear();
                foreach (var b in branches) cboBranch.Items.Add(b);
                cboBranch.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            finally { Cursor = Cursors.Default; }
        }

        void SaveProject()
        {
            var ignoreLines = new List<string>();
            foreach (string line in txtIgnore.Text.Split('\n'))
            {
                string t = line.Trim();
                if (t.Length > 0) ignoreLines.Add(t);
            }

            Project = new GfdProject
            {
                Name = txtName.Text.Trim(),
                LocalFolder = txtFolder.Text.Trim(),
                Owner = txtOwner.Text.Trim(),
                Repo = txtRepo.Text.Trim(),
                Branch = cboBranch.Text.Trim(),
                IgnorePatterns = ignoreLines
            };
        }
    }
}
