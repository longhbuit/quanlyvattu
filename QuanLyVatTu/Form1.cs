namespace QuanLyVatTu;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
    }

    private void tạoUserToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var dlg = new CreateUserForm();
        dlg.ShowDialog(this);
    }
}