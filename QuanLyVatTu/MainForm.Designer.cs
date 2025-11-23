namespace QuanLyVatTu;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        menuStrip1 = new System.Windows.Forms.MenuStrip();
        hệThốngToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        tạoUserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        tàiKhoảnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        chỉnhSửaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        đăngXuấtToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        quảnLýToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        quảnLýKhoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        quảnLýNhanVienToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        quảnLýVatTuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Text = "Form1";

        // menuStrip
        menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            hệThốngToolStripMenuItem,
            tàiKhoảnToolStripMenuItem,
            quảnLýToolStripMenuItem
        });
        menuStrip1.Location = new System.Drawing.Point(0, 0);
        menuStrip1.Name = "menuStrip1";
        menuStrip1.Size = new System.Drawing.Size(800, 24);
        menuStrip1.TabIndex = 0;
        menuStrip1.Text = "menuStrip1";

        // hệ thống menu
        hệThốngToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            tạoUserToolStripMenuItem
        });
        hệThốngToolStripMenuItem.Name = "hệThốngToolStripMenuItem";
        hệThốngToolStripMenuItem.Size = new System.Drawing.Size(69, 20);
        hệThốngToolStripMenuItem.Text = "Hệ thống";

        // tài khoản menu
        tàiKhoảnToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            chỉnhSửaToolStripMenuItem,
            đăngXuấtToolStripMenuItem
        });
        tàiKhoảnToolStripMenuItem.Name = "tàiKhoảnToolStripMenuItem";
        tàiKhoảnToolStripMenuItem.Size = new System.Drawing.Size(65, 20);
        tàiKhoảnToolStripMenuItem.Text = "Tài khoản";

        // quản lý menu
        quảnLýToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            quảnLýKhoToolStripMenuItem,
            quảnLýNhanVienToolStripMenuItem,
            quảnLýVatTuToolStripMenuItem
        });
        quảnLýToolStripMenuItem.Name = "quảnLýToolStripMenuItem";
        quảnLýToolStripMenuItem.Size = new System.Drawing.Size(60, 20);
        quảnLýToolStripMenuItem.Text = "Quản lý";

        // chỉnh sửa
        chỉnhSửaToolStripMenuItem.Name = "chỉnhSửaToolStripMenuItem";
        chỉnhSửaToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        chỉnhSửaToolStripMenuItem.Text = "Chỉnh sửa";
        chỉnhSửaToolStripMenuItem.Click += chỉnhSửaToolStripMenuItem_Click;

        // đăng xuất
        đăngXuấtToolStripMenuItem.Name = "đăngXuấtToolStripMenuItem";
        đăngXuấtToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        đăngXuấtToolStripMenuItem.Text = "Đăng xuất";
        đăngXuấtToolStripMenuItem.Click += đăngXuấtToolStripMenuItem_Click;

        // tạo user
        tạoUserToolStripMenuItem.Name = "tạoUserToolStripMenuItem";
        tạoUserToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        tạoUserToolStripMenuItem.Text = "Tạo User";
        tạoUserToolStripMenuItem.Click += tạoUserToolStripMenuItem_Click;

        // quản lý kho
        quảnLýKhoToolStripMenuItem.Name = "quảnLýKhoToolStripMenuItem";
        quảnLýKhoToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        quảnLýKhoToolStripMenuItem.Text = "Quản lý kho";
        quảnLýKhoToolStripMenuItem.Click += QuanLyKhoToolStripMenuItem_Click;

        // quản lý nhân viên
        quảnLýNhanVienToolStripMenuItem.Name = "quảnLýNhanVienToolStripMenuItem";
        quảnLýNhanVienToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        quảnLýNhanVienToolStripMenuItem.Text = "Quản lý nhân viên";
        quảnLýNhanVienToolStripMenuItem.Click += (s, e) =>
        {
            using var dlg = new EmployeeForm();
            dlg.ShowDialog(this);
        };

        // quản lý vật tư
        quảnLýVatTuToolStripMenuItem.Name = "quảnLýVatTuToolStripMenuItem";
        quảnLýVatTuToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        quảnLýVatTuToolStripMenuItem.Text = "Quản lý vật tư";
        quảnLýVatTuToolStripMenuItem.Click += (s, e) =>
        {
            using var dlg = new VatTuForm();
            dlg.ShowDialog(this);
        };

        Controls.Add(menuStrip1);
        MainMenuStrip = menuStrip1;
    }

    #endregion

    private System.Windows.Forms.MenuStrip menuStrip1;
    private System.Windows.Forms.ToolStripMenuItem hệThốngToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem tạoUserToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem tàiKhoảnToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem chỉnhSửaToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem đăngXuấtToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem quảnLýToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem quảnLýKhoToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem quảnLýNhanVienToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem quảnLýVatTuToolStripMenuItem;
}