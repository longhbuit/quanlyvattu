namespace QuanLyVatTu;

public partial class MainForm : Form
{
    private readonly BranchSite _site;

    public MainForm() : this(BranchSite.CongTy) { }

    public MainForm(BranchSite site)
    {
        _site = site;
        InitializeComponent();
        var branchText = _site switch
        {
            BranchSite.CongTy => "Công Ty",
            BranchSite.ChiNhanh1 => "Chi Nhánh 1",
            BranchSite.ChiNhanh2 => "Chi Nhánh 2",
            _ => "Không rõ"
        };
        var user = AppSession.SqlUsername ?? "(chưa đăng nhập)";
        Text = $"QLVT - {branchText} - User: {user}";
    }

    private void tạoUserToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var dlg = new CreateUserForm();
        dlg.ShowDialog(this);
    }

    private void chỉnhSửaToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // Placeholder: open a dialog to edit current user's settings.
        // For now we show an informational message. We can replace this with a proper edit form later.
        MessageBox.Show("Chức năng 'Chỉnh sửa' chưa được triển khai.", "Chỉnh sửa", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void đăngXuấtToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // Hide the main form and show the login form alone.
        // If login succeeds, update session/UI and show main form again.
        // If login is cancelled, exit the application.
        this.Hide();
        using var login = new LoginForm();
        var result = login.ShowDialog(); // show without owner so only login is visible
        if (result == System.Windows.Forms.DialogResult.OK)
        {
            // LoginForm sets AppSession on success. Update title and show main form again.
            var selected = AppSession.Branch;
            var branchText = selected switch
            {
                BranchSite.CongTy => "Công Ty",
                BranchSite.ChiNhanh1 => "Chi Nhánh 1",
                BranchSite.ChiNhanh2 => "Chi Nhánh 2",
                _ => "Không rõ"
            };
            var user = AppSession.SqlUsername ?? "(chưa đăng nhập)";
            Text = $"QLVT - {branchText} - User: {user}";
            this.Show();
        }
        else
        {
            // user cancelled login -> exit application
            Application.Exit();
        }
    }

    // Handler called from designer: open WarehouseForm
    private void QuanLyKhoToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var dlg = new WarehouseForm();
        dlg.ShowDialog(this);
    }

    // Handler for new menu item: open EmployeeForm
    private void QuanLyNhanVienToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var dlg = new EmployeeForm();
        dlg.ShowDialog(this);
    }
}