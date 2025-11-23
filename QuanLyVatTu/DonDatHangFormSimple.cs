using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace QuanLyVatTu;

public class DonDatHangFormSimple : Form
{
    private TextBox txtOrderNo = new();
    private DateTimePicker dtpOrderDate = new();
    private DataGridView dgvLineItems = new();
    private Button btnSave = new() { Text = "Lưu" };
    private Button btnCancel = new() { Text = "Hủy" };

    public DonDatHangFormSimple()
    {
        Text = "Đơn Đặt Hàng";
        Width = 700;
        Height = 480;
        StartPosition = FormStartPosition.CenterParent;

        var lblNo = new Label { Text = "Số đơn:", Left = 10, Top = 12, Width = 60 };
        txtOrderNo.Left = 80; txtOrderNo.Top = 10; txtOrderNo.Width = 200;
        var lblDate = new Label { Text = "Ngày:", Left = 300, Top = 12, Width = 40 };
        dtpOrderDate.Left = 350; dtpOrderDate.Top = 8; dtpOrderDate.Width = 120; dtpOrderDate.Format = DateTimePickerFormat.Short;

        dgvLineItems.Left = 10; dgvLineItems.Top = 50; dgvLineItems.Width = 660; dgvLineItems.Height = 340;
        dgvLineItems.Columns.Add("ItemCode", "Mã hàng");
        dgvLineItems.Columns.Add("Description", "Diễn giải");
        dgvLineItems.Columns.Add("Quantity", "Số lượng");
        dgvLineItems.Columns.Add("UnitPrice", "Đơn giá");

        btnSave.Left = 480; btnSave.Top = 410; btnSave.Click += BtnSave_Click;
        btnCancel.Left = 580; btnCancel.Top = 410; btnCancel.Click += (_, _) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        Controls.AddRange(new Control[] { lblNo, txtOrderNo, lblDate, dtpOrderDate, dgvLineItems, btnSave, btnCancel });
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtOrderNo.Text))
        {
            MessageBox.Show(this, "Số đơn không được để trống.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtOrderNo.Focus();
            return;
        }

        var hasLine = false;
        foreach (DataGridViewRow r in dgvLineItems.Rows)
        {
            if (r.IsNewRow) continue;
            if (!string.IsNullOrWhiteSpace(r.Cells[0].Value?.ToString())) { hasLine = true; break; }
        }
        if (!hasLine)
        {
            MessageBox.Show(this, "Đơn hàng phải có ít nhất một mặt hàng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show(this, "Đã lưu đơn hàng (stub).", "Lưu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}

