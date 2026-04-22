using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PushPull
{
    public class SettingsDialog : Form
    {
        GfdConfig _config;
        TextBox txtToken;
        TextBox txtDefaultIgnore;

        public SettingsDialog(GfdConfig config)
        {
            _config = config;
            BuildUI();
            AppIcon.Apply(this);
        }

        void BuildUI()
        {
            this.Text = "Settings";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new System.Drawing.Size(480, 260);
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            var lblToken = new Label { Text = "GitHub Auth Token:", Location = new System.Drawing.Point(12, 15), AutoSize = true };
            txtToken = new TextBox
            {
                Location = new System.Drawing.Point(12, 35),
                Width = 380,
                UseSystemPasswordChar = true,
                Text = _config.Token ?? ""
            };
            var btnShow = new Button { Text = "Show", Location = new System.Drawing.Point(398, 33), Width = 60 };
            btnShow.Click += (s, e) => { txtToken.UseSystemPasswordChar = !txtToken.UseSystemPasswordChar; btnShow.Text = txtToken.UseSystemPasswordChar ? "Show" : "Hide"; };

            var lblIgnore = new Label { Text = "Default Ignore Patterns:", Location = new System.Drawing.Point(12, 75), AutoSize = true };
            var lblIgnoreHint = new Label { Text = "(applied to new projects, one per line)", Location = new System.Drawing.Point(175, 75), AutoSize = true, ForeColor = System.Drawing.Color.Gray };
            txtDefaultIgnore = new TextBox
            {
                Location = new System.Drawing.Point(12, 95),
                Width = 446,
                Height = 100,
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical,
                Text = _config.DefaultIgnorePatterns != null ? string.Join("\r\n", _config.DefaultIgnorePatterns) : ""
            };

            var btnTest = new Button { Text = "Test Connection", Location = new System.Drawing.Point(12, 215), Width = 120 };
            btnTest.Click += (s, e) => TestConnection();

            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(300, 215), Width = 75 };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(382, 215), Width = 75 };

            btnOk.Click += (s, e) =>
            {
                _config.Token = txtToken.Text.Trim();
                var patterns = new List<string>();
                foreach (string line in txtDefaultIgnore.Text.Split('\n'))
                {
                    string t = line.Trim();
                    if (t.Length > 0) patterns.Add(t);
                }
                _config.DefaultIgnorePatterns = patterns.Count > 0 ? patterns : null;
            };

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
            this.Controls.AddRange(new Control[] { lblToken, txtToken, btnShow, lblIgnore, lblIgnoreHint, txtDefaultIgnore, btnTest, btnOk, btnCancel });
        }

        void TestConnection()
        {
            string token = txtToken.Text.Trim();
            if (string.IsNullOrEmpty(token)) { MessageBox.Show("Enter a token first."); return; }
            Cursor = Cursors.WaitCursor;
            try
            {
                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("https://api.github.com/user");
                req.Headers.Add("Authorization", "token " + token);
                req.UserAgent = "PushPull-app";
                req.Timeout = 10000;
                var resp = (System.Net.HttpWebResponse)req.GetResponse();
                resp.Close();
                MessageBox.Show("Connection successful.", "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Failed: " + ex.Message, "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { Cursor = Cursors.Default; }
        }
    }
}
