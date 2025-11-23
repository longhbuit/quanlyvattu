using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace QuanLyVatTu;

public class DonDatHangFormSimple : Form
{
    // MADH is identity in DB; user must not enter it. We don't expose a txtOrderNo.
    private TextBox txtSupplier = new();
    private DateTimePicker dtpOrderDate = new();
    private ComboBox cmbMaKho = new();
    private NumericUpDown nudTrangThai = new();
    private Label lblManv = new();
    private DataGridView dgvLineItems = new();
    private Button btnSave = new() { Text = "Lưu" };
    private Button btnCancel = new() { Text = "Hủy" };

    // New buttons for line item management
    private Button btnAddLine = new() { Text = "Thêm" };
    private Button btnEditLine = new() { Text = "Sửa" };
    private Button btnRemoveLine = new() { Text = "Xóa" };

    // Internal list of line items (source of truth)
    private readonly List<OrderLineDto> _lines = new();

    public DonDatHangFormSimple()
    {
        Text = "Đơn Đặt Hàng";
        Width = 700;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;

        // Header: supplier, date, warehouse, and current MANV (read-only)
        var lblSupplier = new Label { Text = "Nhà CC:", Left = 10, Top = 12, Width = 70 };
        txtSupplier.Left = 90; txtSupplier.Top = 10; txtSupplier.Width = 240;
        var lblDate = new Label { Text = "Ngày:", Left = 340, Top = 12, Width = 40 };
        dtpOrderDate.Left = 390; dtpOrderDate.Top = 8; dtpOrderDate.Width = 110; dtpOrderDate.Format = DateTimePickerFormat.Short;
        var lblMaKho = new Label { Text = "Mã kho:", Left = 510, Top = 12, Width = 50 };
        cmbMaKho.Left = 565; cmbMaKho.Top = 8; cmbMaKho.Width = 80; cmbMaKho.DropDownStyle = ComboBoxStyle.DropDownList;
        var lblTrangThai = new Label { Text = "Trạng thái:", Left = 10, Top = 40, Width = 80 };
        nudTrangThai.Left = 90; nudTrangThai.Top = 36; nudTrangThai.Width = 60; nudTrangThai.Minimum = 0; nudTrangThai.Maximum = 999;
        lblManv.Left = 215; lblManv.Top = 40; lblManv.Width = 400; lblManv.Text = "";

        dgvLineItems.Left = 10; dgvLineItems.Top = 50; dgvLineItems.Width = 660; dgvLineItems.Height = 340;
        dgvLineItems.ReadOnly = true;
        dgvLineItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvLineItems.AllowUserToAddRows = false;
        dgvLineItems.AllowUserToDeleteRows = false;
        dgvLineItems.MultiSelect = false;
        // Column keys match DB field names where sensible (MAVT/SoLuong/DonGia)
        dgvLineItems.Columns.Add("MAVT", "Mã hàng");
        dgvLineItems.Columns.Add("Description", "Diễn giải");
        dgvLineItems.Columns.Add("SoLuong", "Số lượng");
        dgvLineItems.Columns.Add("DonGia", "Đơn giá");

        // update buttons when selection changes
        dgvLineItems.SelectionChanged += (_, _) => UpdateLineButtons();

        // position line item buttons
        btnAddLine.Left = 10; btnAddLine.Top = 400; btnAddLine.Width = 80; btnAddLine.Click += BtnAddLine_Click;
        btnEditLine.Left = 100; btnEditLine.Top = 400; btnEditLine.Width = 80; btnEditLine.Click += BtnEditLine_Click;
        btnRemoveLine.Left = 190; btnRemoveLine.Top = 400; btnRemoveLine.Width = 80; btnRemoveLine.Click += BtnRemoveLine_Click;

        btnSave.Left = 480; btnSave.Top = 450; btnSave.Click += BtnSave_Click;
        btnCancel.Left = 580; btnCancel.Top = 450; btnCancel.Click += (_, _) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        Controls.AddRange(new Control[] { lblSupplier, txtSupplier, lblDate, dtpOrderDate, lblMaKho, cmbMaKho, lblTrangThai, nudTrangThai, lblManv, dgvLineItems, btnAddLine, btnEditLine, btnRemoveLine, btnSave, btnCancel });

        // Populate warehouses in combo
        try
        {
            var wh = WarehouseService.LoadAll();
            cmbMaKho.Items.Clear();
            foreach (var w in wh)
            {
                cmbMaKho.Items.Add(w.MaKho);
            }
            if (cmbMaKho.Items.Count > 0) cmbMaKho.SelectedIndex = 0;
        }
        catch { /* ignore if can't load */ }

        // Show current MANV (readonly)
        lblManv.Text = $"MANV: {AppSession.SqlUsername ?? "(chưa đăng nhập)"}";

        RefreshGrid();
    }

    private void RefreshGrid()
    {
        dgvLineItems.Rows.Clear();
        foreach (var l in _lines)
        {
            // Use column keys consistent with DB: MAVT, Description, SoLuong, DonGia
            dgvLineItems.Rows.Add(l.MaVT, l.Description, l.Quantity.ToString("G"), l.UnitPrice.ToString("G"));
        }
        UpdateLineButtons();
    }

    private void UpdateLineButtons()
    {
        var hasSelection = dgvLineItems.SelectedRows.Count > 0;
        btnEditLine.Enabled = hasSelection;
        btnRemoveLine.Enabled = hasSelection;
    }

    private void BtnAddLine_Click(object? sender, EventArgs e)
    {
        using var dlg = new QuanLyVatTu.DonDatHangLineItemForm();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _lines.Add(dlg.Line);
            RefreshGrid();
        }
    }

    private void BtnEditLine_Click(object? sender, EventArgs e)
    {
        if (dgvLineItems.SelectedRows.Count == 0) return;
        var idx = dgvLineItems.SelectedRows[0].Index;
        if (idx < 0 || idx >= _lines.Count) return;
        var existing = _lines[idx];
        using var dlg = new QuanLyVatTu.DonDatHangLineItemForm(existing);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _lines[idx] = dlg.Line;
            RefreshGrid();
        }
    }

    private void BtnRemoveLine_Click(object? sender, EventArgs e)
    {
        if (dgvLineItems.SelectedRows.Count == 0) return;
        var idx = dgvLineItems.SelectedRows[0].Index;
        if (idx < 0 || idx >= _lines.Count) return;
        var confirm = MessageBox.Show(this, "Bạn có chắc muốn xóa mặt hàng đã chọn?", "Xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            _lines.RemoveAt(idx);
            RefreshGrid();
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSupplier.Text))
        {
            MessageBox.Show(this, "Nhà cung cấp không được để trống.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtSupplier.Focus();
            return;
        }

        if (_lines.Count == 0)
        {
            MessageBox.Show(this, "Đơn hàng phải có ít nhất một mặt hàng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            // Map to OrderService DTO
            var lines = new List<OrderService.OrderLine>();
            foreach (var l in _lines)
            {
                lines.Add(new OrderService.OrderLine(l.MaVT, l.Quantity, l.UnitPrice));
            }

            var maNV = AppSession.SqlUsername ?? string.Empty;
            // Get selected MAKHO
            var makho = cmbMaKho.SelectedItem?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(makho))
            {
                MessageBox.Show(this, "Vui lòng chọn kho lưu đơn hàng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Get TrangThai
            var trangThai = (int)nudTrangThai.Value;

            // Get MaCN from Branch
            
            // Do not collect MaCN from UI; let DB default it if not provided.
            var orderDto = new OrderService.OrderDto(dtpOrderDate.Value.Date, txtSupplier.Text.Trim(), maNV, makho, trangThai, (string?)null, lines);
            var newId = OrderService.CreateOrder(orderDto);
            MessageBox.Show(this, $"Đã lưu đơn hàng: MADH={newId}", "Lưu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Lỗi khi lưu đơn hàng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Local DTO for line item
    public class OrderLineDto
    {
        // Use property name MaVT to match DB/Service
        public string MaVT { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
