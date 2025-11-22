using System.Windows.Forms;

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
        listView.Columns.Add("Mã kho", 100);
        listView.Columns.Add("Tên kho", 300);
        listView.Columns.Add("Địa chỉ", 180);

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
        // placeholder: load warehouses from DB later
        listView.Items.Clear();
        listView.Items.Add(new ListViewItem(new[] { "K01", "Kho chính", "Hà Nội" }));
        listView.Items.Add(new ListViewItem(new[] { "K02", "Kho chi nhánh 1", "Hải Phòng" }));
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

