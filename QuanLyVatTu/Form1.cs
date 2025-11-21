namespace QuanLyVatTu;

public partial class Form1 : Form
{
    private readonly BranchSite _site;

    public Form1() : this(BranchSite.CongTy) { }

    public Form1(BranchSite site)
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
}