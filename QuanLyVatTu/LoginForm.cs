namespace QuanLyVatTu;

using System;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

public class LoginForm : Form
{
    private TextBox _txtUsername;
    private TextBox _txtPassword;
    private Button _btnLogin;
    private Button _btnCancel;
    private ComboBox _cboBranch;

    public BranchSite SelectedBranch { get; private set; } = BranchSite.CongTy;
    public string? ConnectionString { get; private set; }

    public LoginForm()
    {
        Text = "Đăng nhập SQL";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 360;
        Height = 235;

        var lblUser = new Label { Text = "SQL User:", Left = 12, Top = 15, Width = 90 };
        _txtUsername = new TextBox { Left = 110, Top = 12, Width = 220 };

        var lblPass = new Label { Text = "Mật khẩu:", Left = 12, Top = 50, Width = 90 };
        _txtPassword = new TextBox { Left = 110, Top = 47, Width = 220, UseSystemPasswordChar = true };

        var lblBranch = new Label { Text = "Chọn server:", Left = 12, Top = 85, Width = 90 };
        _cboBranch = new ComboBox { Left = 110, Top = 82, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        PopulateBranches();

        _btnLogin = new Button { Text = "Đăng nhập", Left = 110, Width = 100, Top = 125, DialogResult = DialogResult.None };
        _btnCancel = new Button { Text = "Thoát", Left = 230, Width = 100, Top = 125, DialogResult = DialogResult.Cancel };

        _btnLogin.Click += BtnLogin_Click;
        _btnCancel.Click += (_, _) => Close();

        Controls.AddRange(new Control[] { lblUser, _txtUsername, lblPass, _txtPassword, lblBranch, _cboBranch, _btnLogin, _btnCancel });
        AcceptButton = _btnLogin;
        CancelButton = _btnCancel;
    }

    private void PopulateBranches()
    {
        _cboBranch.Items.Clear();
        var order = new[] { BranchSite.CongTy, BranchSite.ChiNhanh1, BranchSite.ChiNhanh2 };
        foreach (var site in order)
        {
            var baseConn = ConnectionConfig.GetBase(site);
            string display;
            if (!string.IsNullOrWhiteSpace(baseConn))
            {
                try
                {
                    var builder = new SqlConnectionStringBuilder(baseConn);
                    // Extract port if present
                    var ds = builder.DataSource; // e.g. localhost,14331
                    string port = ds.Contains(',') ? ds.Split(',')[1] : "";
                    display = site switch
                    {
                        BranchSite.CongTy => $"Công Ty",
                        BranchSite.ChiNhanh1 => $"Chi Nhánh 1",
                        BranchSite.ChiNhanh2 => $"Chi Nhánh 2",
                        _ => site.ToString()
                    };
                }
                catch
                {
                    display = site + " (config lỗi)";
                }
            }
            else
            {
                display = site + " (không có config)";
            }
            _cboBranch.Items.Add(display);
        }
        if (_cboBranch.Items.Count > 0) _cboBranch.SelectedIndex = 0;
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        SelectedBranch = _cboBranch.SelectedIndex switch
        {
            0 => BranchSite.CongTy,
            1 => BranchSite.ChiNhanh1,
            2 => BranchSite.ChiNhanh2,
            _ => BranchSite.CongTy
        };

        var loginPrefix = SelectedBranch switch
        {
            BranchSite.CongTy => "cty_",
            BranchSite.ChiNhanh1 => "cn1_",
            BranchSite.ChiNhanh2 => "cn2_",
            _ => string.Empty
        };
        
        var user = loginPrefix+_txtUsername.Text.Trim();
        var pass = _txtPassword.Text;
        if (string.IsNullOrWhiteSpace(user)) { MessageBox.Show("Nhập SQL User.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrEmpty(pass)) { MessageBox.Show("Nhập mật khẩu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var baseConn = ConnectionConfig.GetBase(SelectedBranch);
        if (string.IsNullOrWhiteSpace(baseConn))
        {
            MessageBox.Show("Không tìm thấy connection string cho chi nhánh.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(baseConn)
            {
                UserID = user,
                Password = pass
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show("Connection string không hợp lệ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            // Delegate connection validation to LoginService
            var service = new LoginService();
            var result = service.Login(user, pass, SelectedBranch);
            if (result.Success && result.ConnectionString is not null)
            {
                ConnectionString = result.ConnectionString;
                AppSession.Branch = SelectedBranch;
                AppSession.ConnectionString = ConnectionString;
                AppSession.SqlUsername = user;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Kết nối SQL thất bại: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
