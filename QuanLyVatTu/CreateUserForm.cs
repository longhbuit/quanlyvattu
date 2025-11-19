namespace QuanLyVatTu;

using System;
using System.Windows.Forms;

public class CreateUserForm : Form
{
    private TextBox txtUsername;
    private TextBox txtPassword;
    private TextBox txtConfirm;
    private Button btnCreate;
    private Button btnCancel;

    public CreateUserForm()
    {
        Text = "Tạo User";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 360;
        Height = 220;

        var lblUser = new Label { Text = "Tài khoản:", Left = 12, Top = 15, Width = 90 };
        txtUsername = new TextBox { Left = 110, Top = 12, Width = 220 };

        var lblPass = new Label { Text = "Mật khẩu:", Left = 12, Top = 50, Width = 90 };
        txtPassword = new TextBox { Left = 110, Top = 47, Width = 220, UseSystemPasswordChar = true };

        var lblConfirm = new Label { Text = "Xác nhận:", Left = 12, Top = 85, Width = 90 };
        txtConfirm = new TextBox { Left = 110, Top = 82, Width = 220, UseSystemPasswordChar = true };

        btnCreate = new Button { Text = "Tạo", Left = 110, Width = 100, Top = 120 };
        btnCancel = new Button { Text = "Hủy", Left = 230, Width = 100, Top = 120, DialogResult = DialogResult.Cancel };

        btnCreate.Click += BtnCreate_Click;
        btnCancel.Click += (s,e)=> Close();

        Controls.AddRange(new Control[] { lblUser, txtUsername, lblPass, txtPassword, lblConfirm, txtConfirm, btnCreate, btnCancel });
        AcceptButton = btnCreate;
        CancelButton = btnCancel;
    }

    private void BtnCreate_Click(object sender, EventArgs e)
    {
        var username = txtUsername.Text.Trim();
        var pass = txtPassword.Text;
        var confirm = txtConfirm.Text;
        if (string.IsNullOrWhiteSpace(username)) { MessageBox.Show("Nhập tài khoản.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrEmpty(pass)) { MessageBox.Show("Nhập mật khẩu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (pass != confirm) { MessageBox.Show("Mật khẩu xác nhận không khớp.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (UserStore.AddUser(username, pass, out var error))
        {
            MessageBox.Show("Tạo user thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show("Không thể tạo user: " + error, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

