namespace QuanLyVatTu;

public static class AppSession
{
    public static BranchSite Branch { get; set; } = BranchSite.CongTy;
    public static string? ConnectionString { get; set; }
    public static string? SqlUsername { get; set; }
}

