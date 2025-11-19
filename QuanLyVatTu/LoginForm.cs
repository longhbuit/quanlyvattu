namespace QuanLyVatTu;

using System;
using System.Windows.Forms;

public class LoginForm : Form
{
    private TextBox txtUsername;
    private TextBox txtPassword;
    private Button btnLogin;
    private Button btnCancel;

    public LoginForm()
    {
        Text = "Đăng nhập";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 320;
        Height = 180;

        var lblUser = new Label { Text = "Tài khoản:", Left = 12, Top = 15, Width = 80 };
        txtUsername = new TextBox { Left = 100, Top = 12, Width = 180 };

        var lblPass = new Label { Text = "Mật khẩu:", Left = 12, Top = 50, Width = 80 };
        txtPassword = new TextBox { Left = 100, Top = 47, Width = 180, UseSystemPasswordChar = true };

        btnLogin = new Button { Text = "Đăng nhập", Left = 100, Width = 90, Top = 85, DialogResult = DialogResult.None };
        btnCancel = new Button { Text = "Thoát", Left = 190, Width = 90, Top = 85, DialogResult = DialogResult.Cancel };

        btnLogin.Click += BtnLogin_Click;
        btnCancel.Click += (s,e)=> Close();

        Controls.AddRange(new Control[] { lblUser, txtUsername, lblPass, txtPassword, btnLogin, btnCancel });
        AcceptButton = btnLogin;
        CancelButton = btnCancel;
    }

    private void BtnLogin_Click(object sender, EventArgs e)
    {
        if (UserStore.ValidateUser(txtUsername.Text.Trim(), txtPassword.Text))
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show("Tài khoản hoặc mật khẩu không đúng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

