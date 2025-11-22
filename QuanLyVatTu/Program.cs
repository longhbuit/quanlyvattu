namespace QuanLyVatTu;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        using var login = new LoginForm();
        var result = login.ShowDialog();
        if (result != System.Windows.Forms.DialogResult.OK)
        {
            return; // exit if not logged in
        }

        var selected = login.SelectedBranch;
        Application.Run(new MainForm(selected));
    }
}