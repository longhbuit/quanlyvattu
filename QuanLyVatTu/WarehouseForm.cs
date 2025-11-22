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

        Controls.Add(listView);
        Controls.Add(btnAdd);
        Controls.Add(btnEdit);
        Controls.Add(btnDelete);

        Load += WarehouseForm_Load;
    }

    private void WarehouseForm_Load(object? sender, System.EventArgs e)
    {
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
        MessageBox.Show("Thêm kho - chức năng chưa triển khai.", "Thêm", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnEdit_Click(object? sender, System.EventArgs e)
    {
        if (listView.SelectedItems.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn kho để chỉnh sửa.", "Chỉnh sửa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show("Chỉnh sửa kho - chức năng chưa triển khai.", "Chỉnh sửa", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            // placeholder: delete from DB later
            listView.Items.Remove(listView.SelectedItems[0]);
        }
    }
}
