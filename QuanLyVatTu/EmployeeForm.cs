using System;
using System.Windows.Forms;
using System.Globalization;
using System.Collections.Generic;

namespace QuanLyVatTu;

public class EmployeeForm : Form
{
    private ListView listView;
    private Button btnAdd;
    private Button btnEdit;
    private Button btnDelete;
    private Label _lblInfo;

    public EmployeeForm()
    {
        Text = "Quản lý nhân viên";
        Width = 900;
        Height = 520;

        listView = new ListView
        {
            Dock = DockStyle.Top,
            Height = 360,
            View = View.Details,
            FullRowSelect = true
        };
        // Columns: MaNV, Ho, Ten, DiaChi, NgaySinh, Luong, MaCN
        listView.Columns.Add("Mã NV", 120);
        listView.Columns.Add("Họ", 160);
        listView.Columns.Add("Tên", 120);
        listView.Columns.Add("Địa chỉ", 200);
        listView.Columns.Add("Ngày sinh", 100);
        listView.Columns.Add("Lương", 100);
        listView.Columns.Add("Mã CN", 80);
        // Subscribe once
        listView.SelectedIndexChanged += (_, _) => UpdateButtonsForSelection();

        btnAdd = new Button { Text = "Thêm", Left = 10, Width = 100, Top = 380 };
        btnEdit = new Button { Text = "Chỉnh sửa", Left = 120, Width = 100, Top = 380 };
        btnDelete = new Button { Text = "Xóa", Left = 230, Width = 100, Top = 380 };

        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;

        _lblInfo = new Label { Left = 350, Top = 384, Width = 500 };

        Controls.Add(listView);
        Controls.Add(btnAdd);
        Controls.Add(btnEdit);
        Controls.Add(btnDelete);
        Controls.Add(_lblInfo);

        Load += EmployeeForm_Load;
    }

    private void EmployeeForm_Load(object? sender, EventArgs e)
    {
        var isBranchUser = AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2;
        btnAdd.Enabled = isBranchUser;
        btnEdit.Enabled = isBranchUser;
        btnDelete.Enabled = isBranchUser;
        _lblInfo.Text = isBranchUser ? string.Empty : "(Chỉ user Chi Nhánh mới được thêm/chỉnh sửa/xóa)";

        // Load data; EmployeeService.LoadAll will decide central vs local based on AppSession.Branch
        LoadData();
    }

    private void LoadData()
    {
        listView.Items.Clear();
        Cursor = Cursors.WaitCursor;
        try
        {
            var list = EmployeeService.LoadAll();
            foreach (var e in list)
            {
                var ngay = e.NgaySinh.HasValue ? e.NgaySinh.Value.ToString("yyyy-MM-dd") : string.Empty;
                var luong = e.Luong.HasValue ? e.Luong.Value.ToString("N0", CultureInfo.CurrentCulture) : string.Empty;
                var item = new ListViewItem(new[] { e.MaNV, e.Ho, e.Ten, e.DiaChi ?? string.Empty, ngay, luong, e.MaCN ?? string.Empty });
                listView.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể load danh sách nhân viên: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            var values = dlg.Tag as object[] ?? Array.Empty<object>();
            if (values.Length >= 4 && values[0] is string manv && values[1] is string ho && values[2] is string ten)
            {
                var diachi = values.Length > 3 && values[3] is string s ? s : string.Empty;
                DateTime? ngaysinh = null;
                if (values.Length > 4 && values[4] is DateTime d) ngaysinh = d;
                decimal? luong = null;
                if (values.Length > 5 && values[5] is decimal m) luong = m;

                if (EmployeeService.UpsertEmployee(manv, ho, ten, diachi, ngaysinh, luong))
                {
                    LoadData();
                }
            }
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (listView.SelectedItems.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn nhân viên để chỉnh sửa.", "Chỉnh sửa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var item = listView.SelectedItems[0];
        var manv = item.SubItems[0].Text;
        var ho = item.SubItems[1].Text;
        var ten = item.SubItems[2].Text;
        var diachi = item.SubItems[3].Text;
        DateTime? ngaysinh = null;
        if (DateTime.TryParse(item.SubItems[4].Text, out var d)) ngaysinh = d;
        decimal? luong = null;
        if (decimal.TryParse(item.SubItems[5].Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var dec)) luong = dec;

        using var dlg = CreateEditDialog(manv, ho, ten, diachi, ngaysinh, luong);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            var values = dlg.Tag as object[] ?? Array.Empty<object>();
            if (values.Length >= 4 && values[0] is string m && values[1] is string h && values[2] is string t)
            {
                var dc = values.Length > 3 && values[3] is string s ? s : string.Empty;
                DateTime? ns = null;
                if (values.Length > 4 && values[4] is DateTime dt) ns = dt;
                decimal? l = null;
                if (values.Length > 5 && values[5] is decimal mm) l = mm;

                if (EmployeeService.UpsertEmployee(m, h, t, dc, ns, l))
                {
                    LoadData();
                }
            }
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (listView.SelectedItems.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn nhân viên để xóa.", "Xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirmation = MessageBox.Show("Bạn có chắc muốn xóa nhân viên đã chọn?", "Xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirmation == DialogResult.Yes)
        {
            var manv = listView.SelectedItems[0].SubItems[0].Text;
            if (EmployeeService.DeleteLocalEmployee(manv))
            {
                LoadData();
            }
        }
    }

    private void UpdateButtonsForSelection()
    {
        var hasSelection = listView.SelectedItems.Count > 0;
        btnEdit.Enabled = hasSelection && (AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2);
        btnDelete.Enabled = hasSelection && (AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2);
    }

    private Form CreateEditDialog(string? manv = null, string? ho = null, string? ten = null, string? diachi = null, DateTime? ngaysinh = null, decimal? luong = null)
    {
        var form = new Form { Width = 720, Height = 360, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, Text = string.IsNullOrEmpty(manv) ? "Thêm nhân viên" : "Chỉnh sửa nhân viên" };
        var lblMa = new Label { Text = "Mã NV:", Left = 10, Top = 15, Width = 80 };
        var txtMa = new TextBox { Left = 100, Top = 12, Width = 580, Text = manv ?? string.Empty };
        if (!string.IsNullOrEmpty(manv)) txtMa.ReadOnly = true;
        var lblHo = new Label { Text = "Họ:", Left = 10, Top = 50, Width = 80 };
        var txtHo = new TextBox { Left = 100, Top = 47, Width = 280, Text = ho ?? string.Empty };
        var lblTen = new Label { Text = "Tên:", Left = 400, Top = 50, Width = 40 };
        var txtTen = new TextBox { Left = 450, Top = 47, Width = 230, Text = ten ?? string.Empty };
        var lblDc = new Label { Text = "Địa chỉ (tùy chọn):", Left = 10, Top = 90, Width = 120 };
        var txtDc = new TextBox { Left = 140, Top = 87, Width = 540, Text = diachi ?? string.Empty };

        var lblNs = new Label { Text = "Ngày sinh (tùy chọn):", Left = 10, Top = 130, Width = 120 };
        var dtpNs = new DateTimePicker { Left = 140, Top = 127, Width = 150, Format = DateTimePickerFormat.Short, ShowCheckBox = true };
        if (ngaysinh.HasValue) { dtpNs.Value = ngaysinh.Value; dtpNs.Checked = true; } else dtpNs.Checked = false;

        var lblLuong = new Label { Text = "Lương (tùy chọn):", Left = 310, Top = 130, Width = 100 };
        var txtLuong = new TextBox { Left = 420, Top = 127, Width = 260, Text = luong.HasValue ? luong.Value.ToString("N0", CultureInfo.CurrentCulture) : string.Empty };

        var btnOk = new Button { Text = "OK", Left = 420, Width = 100, Top = 200, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Hủy", Left = 540, Width = 100, Top = 200, DialogResult = DialogResult.Cancel };
        form.Controls.AddRange(new Control[] { lblMa, txtMa, lblHo, txtHo, lblTen, txtTen, lblDc, txtDc, lblNs, dtpNs, lblLuong, txtLuong, btnOk, btnCancel });
        form.AcceptButton = btnOk; form.CancelButton = btnCancel;
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtMa.Text) || string.IsNullOrWhiteSpace(txtHo.Text) || string.IsNullOrWhiteSpace(txtTen.Text))
            {
                MessageBox.Show("Mã NV, Họ và Tên không được để trống.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                form.DialogResult = DialogResult.None;
                return;
            }

            DateTime? ns = null;
            if (dtpNs.Checked) ns = dtpNs.Value.Date;

            decimal? l = null;
            if (!string.IsNullOrWhiteSpace(txtLuong.Text))
            {
                if (!decimal.TryParse(txtLuong.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var tmp))
                {
                    MessageBox.Show("Giá trị Lương không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    form.DialogResult = DialogResult.None;
                    return;
                }
                l = tmp;
            }

            form.Tag = new object[] { txtMa.Text.Trim(), txtHo.Text.Trim(), txtTen.Text.Trim(), txtDc.Text.Trim(), ns, l };
        };
        return form;
    }
}
