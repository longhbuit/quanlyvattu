using System;
using System.Windows.Forms;

namespace QuanLyVatTu;

public class DonDatHangLineItemForm : Form
{
    private ComboBox cmbMaVT = new();
    private TextBox txtDescription = new();
    private NumericUpDown nudQuantity = new();
    private NumericUpDown nudUnitPrice = new();
    private Button btnOk = new() { Text = "OK" };
    private Button btnCancel = new() { Text = "Hủy" };

    public DonDatHangFormSimple.OrderLineDto Line { get; private set; } = new DonDatHangFormSimple.OrderLineDto();

    public DonDatHangLineItemForm()
    {
        Initialize(null);
    }

    public DonDatHangLineItemForm(DonDatHangFormSimple.OrderLineDto existing)
    {
        Initialize(existing);
    }

    private void Initialize(DonDatHangFormSimple.OrderLineDto? existing)
    {
        Text = existing == null ? "Thêm mặt hàng" : "Sửa mặt hàng";
        Width = 420;
        Height = 220;
        StartPosition = FormStartPosition.CenterParent;

        var lblCode = new Label { Text = "Mã hàng:", Left = 10, Top = 15, Width = 80 };
        cmbMaVT.Left = 100; cmbMaVT.Top = 12; cmbMaVT.Width = 280; cmbMaVT.DropDownStyle = ComboBoxStyle.DropDownList;

        var lblDesc = new Label { Text = "Diễn giải:", Left = 10, Top = 50, Width = 80 };
        txtDescription.Left = 100; txtDescription.Top = 47; txtDescription.Width = 280;

        var lblQty = new Label { Text = "Số lượng:", Left = 10, Top = 85, Width = 80 };
        nudQuantity.Left = 100; nudQuantity.Top = 82; nudQuantity.Width = 120; nudQuantity.DecimalPlaces = 0; nudQuantity.Maximum = 1000000; nudQuantity.Minimum = 1;

        var lblPrice = new Label { Text = "Đơn giá:", Left = 230, Top = 85, Width = 60 };
        nudUnitPrice.Left = 300; nudUnitPrice.Top = 82; nudUnitPrice.Width = 80; nudUnitPrice.DecimalPlaces = 2; nudUnitPrice.Maximum = 100000000; nudUnitPrice.Minimum = 0;

        btnOk.Left = 200; btnOk.Top = 130; btnOk.Width = 80; btnOk.Click += BtnOk_Click;
        btnCancel.Left = 290; btnCancel.Top = 130; btnCancel.Width = 80; btnCancel.Click += (_, _) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        Controls.AddRange(new Control[] { lblCode, cmbMaVT, lblDesc, txtDescription, lblQty, nudQuantity, lblPrice, nudUnitPrice, btnOk, btnCancel });

        // Populate VatTu list for combo
        try
        {
            var vtList = VatTuService.LoadAll();
            cmbMaVT.DataSource = vtList;
            cmbMaVT.DisplayMember = "TenVT";
            cmbMaVT.ValueMember = "MaVT";
            cmbMaVT.SelectedIndexChanged += (_, _) =>
            {
                if (cmbMaVT.SelectedItem != null)
                {
                    var p = cmbMaVT.SelectedItem.GetType().GetProperty("TenVT");
                    if (p != null) txtDescription.Text = p.GetValue(cmbMaVT.SelectedItem)?.ToString() ?? string.Empty;
                }
            };
        }
        catch
        {
            // ignore - leave combo empty
        }

        if (existing != null)
        {
            // Select the item in combo if present
            if (!string.IsNullOrEmpty(existing.MaVT) && cmbMaVT.Items.Count > 0)
            {
                for (int i = 0; i < cmbMaVT.Items.Count; i++)
                {
                    var obj = cmbMaVT.Items[i];
                    var prop = obj.GetType().GetProperty("MaVT");
                    if (prop != null && string.Equals(prop.GetValue(obj)?.ToString(), existing.MaVT, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbMaVT.SelectedIndex = i;
                        break;
                    }
                }
            }
            txtDescription.Text = existing.Description;
            nudQuantity.Value = existing.Quantity;
            nudUnitPrice.Value = Convert.ToDecimal(existing.UnitPrice);
            Line = new DonDatHangFormSimple.OrderLineDto
            {
                MaVT = existing.MaVT,
                Description = existing.Description,
                Quantity = existing.Quantity,
                UnitPrice = existing.UnitPrice
            };
        }
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (cmbMaVT.SelectedItem == null)
        {
            MessageBox.Show(this, "Vui lòng chọn một vật tư.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            cmbMaVT.Focus();
            return;
        }

        var qty = (int)nudQuantity.Value;
        if (qty <= 0)
        {
            MessageBox.Show(this, "Số lượng phải lớn hơn 0.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            nudQuantity.Focus();
            return;
        }

        // Get selected MaVT from combo's SelectedValue
        var selectedMaVT = cmbMaVT.SelectedValue?.ToString() ?? string.Empty;

        Line = new DonDatHangFormSimple.OrderLineDto
        {
            MaVT = selectedMaVT,
            Description = txtDescription.Text.Trim(),
            Quantity = (int)nudQuantity.Value,
            UnitPrice = Convert.ToDecimal(nudUnitPrice.Value)
        };

        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
