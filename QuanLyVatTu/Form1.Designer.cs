namespace QuanLyVatTu;

partial class Form1
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
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Text = "Form1";

        // menuStrip
        menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            hệThốngToolStripMenuItem
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

        // tạo user
        tạoUserToolStripMenuItem.Name = "tạoUserToolStripMenuItem";
        tạoUserToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        tạoUserToolStripMenuItem.Text = "Tạo User";
        tạoUserToolStripMenuItem.Click += tạoUserToolStripMenuItem_Click;

        Controls.Add(menuStrip1);
        MainMenuStrip = menuStrip1;
    }

    #endregion

    private System.Windows.Forms.MenuStrip menuStrip1;
    private System.Windows.Forms.ToolStripMenuItem hệThốngToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem tạoUserToolStripMenuItem;
}