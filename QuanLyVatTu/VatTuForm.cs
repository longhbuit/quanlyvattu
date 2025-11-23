using System;
using System.Windows.Forms;

namespace QuanLyVatTu;

public class VatTuForm : Form
{
    private ListView listView;
    private Button btnAdd;
    private Button btnEdit;
    private Button btnDelete;
    private Label _lblInfo;

    public VatTuForm()
    {
        Text = "Quản lý Vật tư";
        Width = 700;
        Height = 420;

        listView = new ListView
        {
            Dock = DockStyle.Top,
            Height = 300,
            View = View.Details,
            FullRowSelect = true
        };
        listView.Columns.Add("Mã VT", 100);
        listView.Columns.Add("Tên VT", 300);
        listView.Columns.Add("ĐVT", 100);
        listView.Columns.Add("SL tồn", 100);

        btnAdd = new Button { Text = "Thêm", Left = 10, Width = 80, Top = 320 };
        btnEdit = new Button { Text = "Chỉnh sửa", Left = 100, Width = 80, Top = 320 };
        btnDelete = new Button { Text = "Xóa", Left = 190, Width = 80, Top = 320 };

        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;

        _lblInfo = new Label { Left = 290, Top = 320, Width = 380 };

        Controls.Add(listView);
        Controls.Add(btnAdd);
        Controls.Add(btnEdit);
        Controls.Add(btnDelete);
        Controls.Add(_lblInfo);

        Load += VatTuForm_Load;
    }

    private void VatTuForm_Load(object? sender, EventArgs e)
    {
        var isBranchUser = AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2;
        btnAdd.Enabled = isBranchUser;
        btnEdit.Enabled = isBranchUser;
        btnDelete.Enabled = isBranchUser;
        _lblInfo.Text = isBranchUser ? string.Empty : "(Chỉ user Chi Nhánh mới được thêm/chỉnh sửa/xóa)";

        LoadData();
    }

    private void LoadData()
    {
        listView.Items.Clear();
        Cursor = Cursors.WaitCursor;
        try
        {
            var items = VatTuService.LoadAll();
            foreach (var v in items)
            {
                listView.Items.Add(new ListViewItem(new[] { v.MaVT, v.TenVT, v.DVT ?? string.Empty, (v.SoLuongTon.HasValue ? v.SoLuongTon.Value.ToString() : "") }));
            }
            listView.SelectedIndexChanged += (_, _) => UpdateButtonsForSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể load danh sách vật tư: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var dlg = CreateEditDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            var values = dlg.Tag as string[] ?? Array.Empty<string>();
            if (values.Length >= 3)
            {
                var code = values[0];
                var name = values[1];
                var dvt = values[2];
                if (UpsertVatTu(code, name, dvt)) LoadData();
            }
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (listView.SelectedItems.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn vật tư để chỉnh sửa.", "Chỉnh sửa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var item = listView.SelectedItems[0];
        var code = item.SubItems[0].Text;
        var name = item.SubItems[1].Text;
        var dvt = item.SubItems[2].Text;
        using var dlg = CreateEditDialog(code, name, dvt);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            var values = dlg.Tag as string[] ?? Array.Empty<string>();
            if (values.Length >= 3)
            {
                if (UpsertVatTu(values[0], values[1], values[2])) LoadData();
            }
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (listView.SelectedItems.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn vật tư để xóa.", "Xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var confirmation = MessageBox.Show("Bạn có chắc muốn xóa vật tư đã chọn?", "Xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirmation == DialogResult.Yes)
        {
            var code = listView.SelectedItems[0].SubItems[0].Text;
            if (DeleteLocalVatTu(code)) LoadData();
        }
    }

    private void UpdateButtonsForSelection()
    {
        var hasSelection = listView.SelectedItems.Count > 0;
        btnEdit.Enabled = hasSelection && (AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2);
        btnDelete.Enabled = hasSelection && (AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2);
    }

    private Form CreateEditDialog(string? code = null, string? name = null, string? dvt = null)
    {
        var form = new Form { Width = 460, Height = 230, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, Text = string.IsNullOrEmpty(code) ? "Thêm vật tư" : "Chỉnh sửa vật tư" };
        var lblCode = new Label { Text = "Mã VT:", Left = 10, Top = 15, Width = 100 };
        var txtCode = new TextBox { Left = 120, Top = 12, Width = 300, Text = code ?? string.Empty };
        if (!string.IsNullOrEmpty(code)) txtCode.ReadOnly = true;
        var lblName = new Label { Text = "Tên VT:", Left = 10, Top = 50, Width = 100 };
        var txtName = new TextBox { Left = 120, Top = 47, Width = 300, Text = name ?? string.Empty };
        var lblDVT = new Label { Text = "ĐVT:", Left = 10, Top = 85, Width = 100 };
        var txtDVT = new TextBox { Left = 120, Top = 82, Width = 300, Text = dvt ?? string.Empty };

        var btnOk = new Button { Text = "OK", Left = 240, Width = 80, Top = 140, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Hủy", Left = 340, Width = 80, Top = 140, DialogResult = DialogResult.Cancel };
        form.Controls.AddRange(new Control[] { lblCode, txtCode, lblName, txtName, lblDVT, txtDVT, btnOk, btnCancel });
        form.AcceptButton = btnOk; form.CancelButton = btnCancel;
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtCode.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Mã VT và Tên VT không được để trống.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                form.DialogResult = DialogResult.None;
                return;
            }
            var m = txtCode.Text.Trim();
            if (m.Length > 4) m = m.Substring(0, 4);
            form.Tag = new string[] { m, txtName.Text.Trim(), txtDVT.Text.Trim() };
        };
        return form;
    }

    private bool UpsertVatTu(string mavt, string tenvt, string dvt)
    {
        try
        {
            return VatTuService.UpsertVatTu(mavt, tenvt, dvt);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi thực hiện upsert vật tư: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private bool DeleteLocalVatTu(string mavt)
    {
        try
        {
            return VatTuService.DeleteLocalVatTu(mavt);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi xóa vật tư: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}

