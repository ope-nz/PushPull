using System;
using System.Drawing;
using System.Windows.Forms;

namespace PushPull
{
    public class CommentDialog : Form
    {
        private Label lblPrompt;
        private TextBox txtComment;
        private Button btnPush;
        private Button btnCancel;

        public string Comment { get { return txtComment.Text.Trim(); } }

        public CommentDialog()
        {
            this.Text = "Commit Message";
            this.ClientSize = new Size(390, 120);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            lblPrompt = new Label();
            lblPrompt.Text = "Enter a commit message:";
            lblPrompt.Location = new Point(12, 12);
            lblPrompt.AutoSize = true;

            txtComment = new TextBox();
            txtComment.Location = new Point(12, 32);
            txtComment.Size = new Size(366, 23);
            txtComment.Text = "PushPull update";

            btnPush = new Button();
            btnPush.Text = "Push";
            btnPush.Location = new Point(222, 72);
            btnPush.Size = new Size(75, 26);
            btnPush.DialogResult = DialogResult.OK;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(303, 72);
            btnCancel.Size = new Size(75, 26);
            btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblPrompt);
            this.Controls.Add(txtComment);
            this.Controls.Add(btnPush);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnPush;
            this.CancelButton = btnCancel;

            this.Load += (s, e) => txtComment.SelectAll();
        }
    }
}
