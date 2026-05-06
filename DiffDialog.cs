using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PushPull
{
    public class DiffDialog : Form
    {
        RichTextBox _rtb;
        Label _lblHeader;

        static readonly Color ColorAdded   = Color.FromArgb(180, 255, 180);
        static readonly Color ColorRemoved = Color.FromArgb(255, 200, 200);

        const int WM_SETREDRAW = 0x000B;
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public DiffDialog()
        {
            BuildUI();
            AppIcon.Apply(this);
        }

        void BuildUI()
        {
            this.Text = "File Diff";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ClientSize = new System.Drawing.Size(820, 540);
            this.MinimumSize = new System.Drawing.Size(500, 300);

            _lblHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font("Segoe UI", 8.5f),
                Padding = new Padding(4, 0, 0, 0)
            };

            var pnlLegend = new Panel { Dock = DockStyle.Top, Height = 22 };
            AddLegend(pnlLegend,   4, ColorAdded,   "+ Local (added / changed)");
            AddLegend(pnlLegend, 190, ColorRemoved, "- Remote (removed / changed)");

            _rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 9f),
                BackColor = SystemColors.Window,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = false
            };

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 36 };
            var btnClose = new Button { Text = "Close", DialogResult = DialogResult.Cancel, Width = 75, Height = 26 };
            btnClose.Location = new System.Drawing.Point(this.ClientSize.Width - btnClose.Width - 8, 5);
            btnClose.Anchor = AnchorStyles.Right;
            pnlBottom.Controls.Add(btnClose);

            this.Controls.Add(_rtb);
            this.Controls.Add(pnlLegend);
            this.Controls.Add(_lblHeader);
            this.Controls.Add(pnlBottom);
            this.CancelButton = btnClose;
        }

        void AddLegend(Panel p, int x, Color bg, string text)
        {
            var lbl = new Label
            {
                AutoSize = false,
                Width = 185,
                Height = p.Height,
                Location = new System.Drawing.Point(x, 0),
                Text = "  " + text,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                BackColor = bg
            };
            p.Controls.Add(lbl);
        }

        public void SetMessage(string fileName, string message)
        {
            this.Text = "Diff: " + fileName;
            _lblHeader.Text = fileName;
            _rtb.Text = message;
        }

        public void SetDiff(string fileName, string localText, string remoteText)
        {
            this.Text = "Diff: " + fileName;
            _lblHeader.Text = fileName;

            string[] localLines  = localText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            string[] remoteLines = remoteText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            if (localLines.Length + remoteLines.Length > 5000)
            {
                _rtb.Text = "File too large for inline diff ("
                    + (localLines.Length + remoteLines.Length) + " total lines). Open the files directly to compare.";
                return;
            }

            var diff = BuildDiff(localLines, remoteLines);

            IntPtr hwnd = _rtb.Handle;
            SendMessage(hwnd, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
            try
            {
                _rtb.Clear();
                foreach (var item in diff)
                {
                    if (item.Kind == DiffKind.Same)
                    {
                        _rtb.AppendText("  " + item.Text + "\n");
                    }
                    else
                    {
                        int start = _rtb.TextLength;
                        string prefix = item.Kind == DiffKind.Added ? "+ " : "- ";
                        _rtb.AppendText(prefix + item.Text + "\n");
                        _rtb.Select(start, prefix.Length + item.Text.Length + 1);
                        _rtb.SelectionBackColor = item.Kind == DiffKind.Added ? ColorAdded : ColorRemoved;
                    }
                }
            }
            finally
            {
                SendMessage(hwnd, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                _rtb.Invalidate();
            }
            _rtb.SelectionStart = 0;
            _rtb.SelectionLength = 0;
        }

        enum DiffKind { Same, Added, Removed }

        struct DiffItem
        {
            public string Text;
            public DiffKind Kind;
        }

        static List<DiffItem> BuildDiff(string[] local, string[] remote)
        {
            int m = local.Length, n = remote.Length;
            var c = new int[m + 1, n + 1];
            for (int i = 1; i <= m; i++)
                for (int j = 1; j <= n; j++)
                    c[i, j] = string.Equals(local[i - 1], remote[j - 1])
                        ? c[i - 1, j - 1] + 1
                        : Math.Max(c[i - 1, j], c[i, j - 1]);

            var temp = new List<DiffItem>();
            int li = m, ri = n;
            while (li > 0 || ri > 0)
            {
                if (li > 0 && ri > 0 && string.Equals(local[li - 1], remote[ri - 1]))
                {
                    temp.Add(new DiffItem { Text = local[li - 1], Kind = DiffKind.Same });
                    li--; ri--;
                }
                else if (ri > 0 && (li == 0 || c[li, ri - 1] >= c[li - 1, ri]))
                {
                    temp.Add(new DiffItem { Text = remote[ri - 1], Kind = DiffKind.Removed });
                    ri--;
                }
                else
                {
                    temp.Add(new DiffItem { Text = local[li - 1], Kind = DiffKind.Added });
                    li--;
                }
            }
            temp.Reverse();
            return temp;
        }
    }
}
