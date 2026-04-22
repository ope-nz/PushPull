using System;
using System.Windows.Forms;

namespace GFD
{
    public class SettingsDialog : Form
    {
        GfdConfig _config;
        TextBox txtToken;

        public SettingsDialog(GfdConfig config)
        {
            _config = config;
            BuildUI();
        }

        void BuildUI()
        {
            this.Text = "Settings";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new System.Drawing.Size(480, 120);
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

            var btnTest = new Button { Text = "Test Connection", Location = new System.Drawing.Point(12, 75), Width = 120 };
            btnTest.Click += (s, e) => TestConnection();

            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(300, 75), Width = 75 };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(382, 75), Width = 75 };

            btnOk.Click += (s, e) => { _config.Token = txtToken.Text.Trim(); };

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
            this.Controls.AddRange(new Control[] { lblToken, txtToken, btnShow, btnTest, btnOk, btnCancel });
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
                req.UserAgent = "GFD-app";
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
