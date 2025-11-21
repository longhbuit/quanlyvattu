namespace QuanLyVatTu;

using System;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

public class CreateUserForm : Form
{
    private TextBox _txtUsername;
    private TextBox _txtPassword;
    private TextBox _txtConfirm;
    private Button _btnCreate;
    private Button _btnCancel;
    private ComboBox _cmbScope; // Công Ty / Chi Nhánh
    private ComboBox _cmbRole;  // Role selection

    public CreateUserForm()
    {
        Text = "Tạo SQL Login";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 420;
        Height = 330; // increased to fit new controls

        var lblUser = new Label { Text = "Login name:", Left = 12, Top = 15, Width = 100 };
        _txtUsername = new TextBox { Left = 120, Top = 12, Width = 220 };

        var lblPass = new Label { Text = "Password:", Left = 12, Top = 50, Width = 100 };
        _txtPassword = new TextBox { Left = 120, Top = 47, Width = 220, UseSystemPasswordChar = true };

        var lblConfirm = new Label { Text = "Confirm:", Left = 12, Top = 85, Width = 100 };
        _txtConfirm = new TextBox { Left = 120, Top = 82, Width = 220, UseSystemPasswordChar = true };

        var lblScope = new Label { Text = "Phạm vi:", Left = 12, Top = 120, Width = 100 };
        _cmbScope = new ComboBox { Left = 120, Top = 117, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbScope.Items.AddRange(new object[] { "Công Ty", "Chi Nhánh" });
        _cmbScope.SelectedIndex = 0;

        var lblRole = new Label { Text = "Role:", Left = 12, Top = 155, Width = 100 };
        _cmbRole = new ComboBox { Left = 120, Top = 152, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbRole.Items.AddRange(new object[] { "CongTy_Role", "ChiNhanh_Role", "User_Role" });
        _cmbRole.SelectedIndex = 0;

        _btnCreate = new Button { Text = "Tạo", Left = 120, Width = 100, Top = 200 };
        _btnCancel = new Button { Text = "Hủy", Left = 240, Width = 100, Top = 200, DialogResult = DialogResult.Cancel };

        _btnCreate.Click += BtnCreate_Click;
        _btnCancel.Click += (_, _) => Close();

        Controls.AddRange(new Control[] { lblUser, _txtUsername, lblPass, _txtPassword, lblConfirm, _txtConfirm, lblScope, _cmbScope, lblRole, _cmbRole, _btnCreate, _btnCancel });
        AcceptButton = _btnCreate;
        CancelButton = _btnCancel;
    }

    private void BtnCreate_Click(object? sender, EventArgs e)
    {
        var loginName = _txtUsername.Text.Trim();
        var pass = _txtPassword.Text;
        var confirm = _txtConfirm.Text;
        var scope = _cmbScope.SelectedItem?.ToString();
        var role = _cmbRole.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(loginName)) { MessageBox.Show("Nhập login name.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrEmpty(pass)) { MessageBox.Show("Nhập password.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (pass != confirm) { MessageBox.Show("Password xác nhận không khớp.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrEmpty(scope) || string.IsNullOrEmpty(role)) { MessageBox.Show("Chọn phạm vi và role.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        if (AppSession.ConnectionString is null)
        {
            MessageBox.Show("Chưa có kết nối SQL.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Basic password complexity hint (not enforced here beyond length)
        if (pass.Length < 6)
        {
            if (MessageBox.Show("Password ngắn (<6). Tiếp tục?", "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No) return;
        }

        try
        {
            var csb = new SqlConnectionStringBuilder(AppSession.ConnectionString);
            var initialDb = (csb.InitialCatalog ?? string.Empty).Trim();
            using var conn = new SqlConnection(AppSession.ConnectionString);
            conn.Open();

            // Role restrictions
            if (scope == "Chi Nhánh" && role == "CongTy_Role")
            {
                MessageBox.Show("Chi nhánh không được tạo tài khoản Công Ty.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (scope == "Công Ty" && !string.Equals(initialDb, "CTY", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Kết nối hiện tại không phải database CTY.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (scope == "Chi Nhánh" && string.Equals(initialDb, "CTY", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Đang ở CTY, chọn phạm vi Công Ty hoặc đổi kết nối sang CN.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check if login exists (server-wide)
            using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.server_principals WHERE name = @name", conn))
            {
                checkCmd.Parameters.AddWithValue("@name", loginName);
                var exists = (int)checkCmd.ExecuteScalar()! > 0;
                if (exists)
                {
                    MessageBox.Show("Login đã tồn tại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (scope == "Công Ty")
            {
                // 1. Tạo login toàn cục
                using (var spGlobal = new SqlCommand("dbo.SP_TaoLogin_Global", conn))
                {
                    spGlobal.CommandType = System.Data.CommandType.StoredProcedure;
                    spGlobal.Parameters.AddWithValue("@LoginName", loginName);
                    spGlobal.Parameters.AddWithValue("@Password", pass);
                    var ret = spGlobal.ExecuteScalar(); // assume returns 0 or maybe uses RETURN; we ignore scalar if not present
                }
                // 2. Tạo user + gán role trong CTY
                using (var spCompany = new SqlCommand("dbo.SP_TaoTaiKhoan_CongTy", conn))
                {
                    spCompany.CommandType = System.Data.CommandType.StoredProcedure;
                    spCompany.Parameters.AddWithValue("@LoginName", loginName);
                    spCompany.Parameters.AddWithValue("@Password", pass);
                    spCompany.Parameters.AddWithValue("@Role", role);
                    spCompany.ExecuteNonQuery();
                }
            }
            else // Chi Nhánh
            {
                using (var spBranch = new SqlCommand("dbo.SP_TaoTaiKhoan_ChiNhanh", conn))
                {
                    spBranch.CommandType = System.Data.CommandType.StoredProcedure;
                    spBranch.Parameters.AddWithValue("@LoginName", loginName);
                    spBranch.Parameters.AddWithValue("@Password", pass);
                    spBranch.Parameters.AddWithValue("@Role", role);
                    spBranch.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Tạo tài khoản thành công qua Stored Procedure.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (SqlException sqlEx)
        {
            MessageBox.Show("SQL lỗi: " + sqlEx.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tạo login: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
