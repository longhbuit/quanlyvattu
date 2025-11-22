using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.Data;

namespace QuanLyVatTu;

public class WarehouseForm : Form
{
    private ListView listView;
    private Button btnAdd;
    private Button btnEdit;
    private Button btnDelete;
    private Label _lblInfo;

    public WarehouseForm()
    {
        Text = "Quản lý kho";
        Width = 600;
        Height = 400;

        listView = new ListView
        {
            Dock = DockStyle.Top,
            Height = 280,
            View = View.Details,
            FullRowSelect = true
        };
        // Hiển thị chỉ 2 cột: MAKHO và TenKho
        listView.Columns.Add("Mã kho", 120);
        listView.Columns.Add("Tên kho", 440);

        btnAdd = new Button { Text = "Thêm", Left = 10, Width = 80, Top = 300 };
        btnEdit = new Button { Text = "Chỉnh sửa", Left = 100, Width = 80, Top = 300 };
        btnDelete = new Button { Text = "Xóa", Left = 190, Width = 80, Top = 300 };

        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;
        
        _lblInfo = new Label { Left = 290, Top = 300, Width = 280 };

        Controls.Add(listView);
        Controls.Add(btnAdd);
        Controls.Add(btnEdit);
        Controls.Add(btnDelete);
        Controls.Add(_lblInfo);

        Load += WarehouseForm_Load;
    }

    private void WarehouseForm_Load(object? sender, System.EventArgs e)
    {
        // Enable add/edit/delete only for branch users (ChiNhanh1 or ChiNhanh2)
        var isBranchUser = AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2;
        btnAdd.Enabled = isBranchUser;
        btnEdit.Enabled = isBranchUser;
        btnDelete.Enabled = isBranchUser;
        _lblInfo.Text = isBranchUser ? "" : "(Chỉ user Chi Nhánh mới được thêm/chỉnh sửa/xóa)";

        // Load warehouses from DB table Kho. Use session connection string if available.
        string? connStr = AppSession.ConnectionString ?? ConnectionConfig.GetBase(AppSession.Branch);
        if (string.IsNullOrWhiteSpace(connStr))
        {
            MessageBox.Show("Không tìm thấy connection string. Vui lòng đăng nhập trước.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        listView.Items.Clear();
        Cursor = Cursors.WaitCursor;
        try
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT MAKHO, TenKho FROM Kho";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var ma = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();
                var ten = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                listView.Items.Add(new ListViewItem(new[] { ma, ten }));
            }

            // Ensure buttons reflect selection
            listView.SelectedIndexChanged += (_, _) => UpdateButtonsForSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể load danh sách kho: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void BtnAdd_Click(object? sender, System.EventArgs e)
    {
        using var dlg = CreateEditDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            var makho = dlg.Tag as string[] ?? Array.Empty<string>();
            if (makho.Length >= 2)
            {
                var code = makho[0];
                var name = makho[1];
                // DiaChi optional empty, MaCN from session
                var diachi = makho.Length > 2 ? makho[2] : string.Empty;
                if (UpsertWarehouse(code, name, diachi))
                {
                    LoadData();
                }
            }
        }
    }

    private void BtnEdit_Click(object? sender, System.EventArgs e)
    {
        if (listView.SelectedItems.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn kho để chỉnh sửa.", "Chỉnh sửa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var item = listView.SelectedItems[0];
        var currentCode = item.SubItems[0].Text;
        var currentName = item.SubItems[1].Text;
        using var dlg = CreateEditDialog(currentCode, currentName);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            var values = dlg.Tag as string[] ?? Array.Empty<string>();
            if (values.Length >= 2)
            {
                if (UpsertWarehouse(values[0], values[1], values.Length > 2 ? values[2] : string.Empty))
                {
                    LoadData();
                }
            }
        }
    }

    private void BtnDelete_Click(object? sender, System.EventArgs e)
    {
        if (listView.SelectedItems.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn kho để xóa.", "Xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirmation = MessageBox.Show("Bạn có chắc muốn xóa kho đã chọn?", "Xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirmation == DialogResult.Yes)
        {
            var code = listView.SelectedItems[0].SubItems[0].Text;
            if (DeleteLocalWarehouse(code))
            {
                LoadData();
            }
        }
    }

    // Helper to reload data
    private void LoadData()
    {
        WarehouseForm_Load(this, System.EventArgs.Empty);
    }

    private void UpdateButtonsForSelection()
    {
        var hasSelection = listView.SelectedItems.Count > 0;
        btnEdit.Enabled = hasSelection && (AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2);
        btnDelete.Enabled = hasSelection && (AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2);
    }

    // Show a small dialog to input MAKHO, TenKho (and optional DiaChi)
    private Form CreateEditDialog(string? code = null, string? name = null)
    {
        var form = new Form { Width = 420, Height = 200, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, Text = string.IsNullOrEmpty(code) ? "Thêm kho" : "Chỉnh sửa kho" };
        var lblCode = new Label { Text = "Mã kho:", Left = 10, Top = 15, Width = 80 };
        var txtCode = new TextBox { Left = 100, Top = 12, Width = 280, Text = code ?? string.Empty };
        if (!string.IsNullOrEmpty(code)) txtCode.ReadOnly = true; // don't allow changing primary key on edit
        var lblName = new Label { Text = "Tên kho:", Left = 10, Top = 50, Width = 80 };
        var txtName = new TextBox { Left = 100, Top = 47, Width = 280, Text = name ?? string.Empty };
        var lblDiaChi = new Label { Text = "Địa chỉ (tùy chọn):", Left = 10, Top = 85, Width = 120 };
        var txtDiaChi = new TextBox { Left = 140, Top = 82, Width = 240, Text = string.Empty };
        var btnOk = new Button { Text = "OK", Left = 220, Width = 80, Top = 120, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Hủy", Left = 310, Width = 80, Top = 120, DialogResult = DialogResult.Cancel };
        form.Controls.AddRange(new Control[] { lblCode, txtCode, lblName, txtName, lblDiaChi, txtDiaChi, btnOk, btnCancel });
        form.AcceptButton = btnOk; form.CancelButton = btnCancel;
        btnOk.Click += (_, _) =>
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(txtCode.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Mã kho và Tên kho không được để trống.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                form.DialogResult = DialogResult.None;
                return;
            }
            // normalize MAKHO length
            var m = txtCode.Text.Trim();
            if (m.Length > 4) m = m.Substring(0,4);
            form.Tag = new string[] { m, txtName.Text.Trim(), txtDiaChi.Text.Trim() };
        };
        return form;
    }

    // Upsert via linked server to CTY
    private bool UpsertWarehouse(string makho, string tenKho, string diachi)
    {
        string? connStr = AppSession.ConnectionString ?? ConnectionConfig.GetBase(AppSession.Branch);
        if (string.IsNullOrWhiteSpace(connStr))
        {
            MessageBox.Show("Không tìm thấy connection string.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        // MaCN based on branch
        var macn = AppSession.Branch == BranchSite.ChiNhanh1 ? "CN1" : AppSession.Branch == BranchSite.ChiNhanh2 ? "CN2" : "CTY";

        try
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            // Call stored procedure on central server via linked server LINK_CTY
            cmd.CommandText = "EXEC LINK_CTY.CTY.dbo.SP_Kho_Upsert @MAKHO, @TenKho, @DiaChi, @MaCN";
            cmd.Parameters.Add(new SqlParameter("@MAKHO", System.Data.SqlDbType.Char, 4) { Value = makho });
            cmd.Parameters.Add(new SqlParameter("@TenKho", System.Data.SqlDbType.NVarChar, 50) { Value = tenKho });
            cmd.Parameters.Add(new SqlParameter("@DiaChi", System.Data.SqlDbType.NVarChar, 100) { Value = (object?)diachi ?? string.Empty });
            cmd.Parameters.Add(new SqlParameter("@MaCN", System.Data.SqlDbType.Char, 3) { Value = macn });
            cmd.CommandTimeout = 30;
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi thực hiện upsert kho: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    // Delete local warehouse
    private bool DeleteLocalWarehouse(string makho)
    {
        string? connStr = AppSession.ConnectionString ?? ConnectionConfig.GetBase(AppSession.Branch);
        if (string.IsNullOrWhiteSpace(connStr))
        {
            MessageBox.Show("Không tìm thấy connection string.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        try
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Kho WHERE MAKHO = @MAKHO";
            cmd.Parameters.Add(new SqlParameter("@MAKHO", System.Data.SqlDbType.Char, 4) { Value = makho });
            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi xóa kho: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}
