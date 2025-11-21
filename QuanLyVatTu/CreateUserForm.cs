namespace QuanLyVatTu;

using System;
using System.Windows.Forms;

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
            // Delegate creation to service
            var service = new UserCreationService(AppSession.ConnectionString);
            var result = service.CreateUser(loginName, pass, scope!, role!);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            MessageBox.Show("Không thể tạo login: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
