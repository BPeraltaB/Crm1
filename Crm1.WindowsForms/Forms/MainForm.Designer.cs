namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Código generado por el Diseñador de Windows Forms para el formulario principal.
/// </summary>
partial class MainForm
{
    /// <summary>
    /// Variable del diseñador requerida.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Limpiar los recursos que se estén usando.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Código generado por el Diseñador de Windows Forms

    /// <summary>
    /// Método necesario para admitir el diseñador. No se puede modificar
    /// con el editor de código.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.menuStrip = new System.Windows.Forms.MenuStrip();
        this.appTitleItem = new System.Windows.Forms.ToolStripLabel();
        this.topSeparator = new System.Windows.Forms.ToolStripSeparator();
        this.clientsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.searchMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.prospectsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.bottomSeparator = new System.Windows.Forms.ToolStripSeparator();
        this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.panelContent = new System.Windows.Forms.Panel();
        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.statusLabelApp = new System.Windows.Forms.ToolStripStatusLabel();
        this.statusLabelSpring = new System.Windows.Forms.ToolStripStatusLabel();
        this.statusLabelClock = new System.Windows.Forms.ToolStripStatusLabel();
        this.clockTimer = new System.Windows.Forms.Timer(this.components);
        this.menuStrip.SuspendLayout();
        this.panelContent.SuspendLayout();
        this.statusStrip.SuspendLayout();
        this.SuspendLayout();
        // 
        // menuStrip
        // 
        this.menuStrip.AutoSize = false;
        this.menuStrip.BackColor = System.Drawing.Color.FromArgb(45, 55, 72);
        this.menuStrip.Dock = System.Windows.Forms.DockStyle.Left;
        this.menuStrip.Font = new System.Drawing.Font("Segoe UI", 10.5F);
        this.menuStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
        this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.appTitleItem,
            this.topSeparator,
            this.clientsMenuItem,
            this.searchMenuItem,
            this.prospectsMenuItem,
            this.bottomSeparator,
            this.exitMenuItem});
        this.menuStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
        this.menuStrip.Padding = new System.Windows.Forms.Padding(4, 8, 4, 8);
        this.menuStrip.Size = new System.Drawing.Size(220, 750);
        // 
        // appTitleItem
        // 
        this.appTitleItem.AutoSize = false;
        this.appTitleItem.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold);
        this.appTitleItem.ForeColor = System.Drawing.Color.White;
        this.appTitleItem.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
        this.appTitleItem.Size = new System.Drawing.Size(212, 56);
        this.appTitleItem.Text = "CRM1";
        this.appTitleItem.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // topSeparator
        // 
        this.topSeparator.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
        // 
        // clientsMenuItem
        // 
        this.clientsMenuItem.AutoSize = false;
        this.clientsMenuItem.ForeColor = System.Drawing.Color.FromArgb(226, 232, 240);
        this.clientsMenuItem.Margin = new System.Windows.Forms.Padding(4, 1, 4, 1);
        this.clientsMenuItem.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
        this.clientsMenuItem.Size = new System.Drawing.Size(212, 40);
        this.clientsMenuItem.Text = "Clientes";
        this.clientsMenuItem.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // searchMenuItem
        // 
        this.searchMenuItem.AutoSize = false;
        this.searchMenuItem.ForeColor = System.Drawing.Color.FromArgb(226, 232, 240);
        this.searchMenuItem.Margin = new System.Windows.Forms.Padding(4, 1, 4, 1);
        this.searchMenuItem.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
        this.searchMenuItem.Size = new System.Drawing.Size(212, 40);
        this.searchMenuItem.Text = "Búsqueda";
        this.searchMenuItem.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // prospectsMenuItem
        // 
        this.prospectsMenuItem.AutoSize = false;
        this.prospectsMenuItem.ForeColor = System.Drawing.Color.FromArgb(226, 232, 240);
        this.prospectsMenuItem.Margin = new System.Windows.Forms.Padding(4, 1, 4, 1);
        this.prospectsMenuItem.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
        this.prospectsMenuItem.Size = new System.Drawing.Size(212, 40);
        this.prospectsMenuItem.Text = "Futuros clientes";
        this.prospectsMenuItem.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // bottomSeparator
        // 
        this.bottomSeparator.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
        // 
        // exitMenuItem
        // 
        this.exitMenuItem.AutoSize = false;
        this.exitMenuItem.ForeColor = System.Drawing.Color.FromArgb(226, 232, 240);
        this.exitMenuItem.Margin = new System.Windows.Forms.Padding(4, 1, 4, 1);
        this.exitMenuItem.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
        this.exitMenuItem.Size = new System.Drawing.Size(212, 40);
        this.exitMenuItem.Text = "Salir";
        this.exitMenuItem.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.exitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
        // 
        // panelContent
        // 
        this.panelContent.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
        this.panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelContent.Location = new System.Drawing.Point(220, 0);
        this.panelContent.Padding = new System.Windows.Forms.Padding(24, 20, 24, 16);
        this.panelContent.Size = new System.Drawing.Size(980, 750);
        // 
        // statusStrip
        // 
        this.statusStrip.BackColor = System.Drawing.Color.White;
        this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabelApp,
            this.statusLabelSpring,
            this.statusLabelClock});
        this.statusStrip.Location = new System.Drawing.Point(220, 728);
        this.statusStrip.Size = new System.Drawing.Size(980, 22);
        this.statusStrip.SizingGrip = true;
        // 
        // statusLabelApp
        // 
        this.statusLabelApp.Name = "statusLabelApp";
        this.statusLabelApp.Text = "CRM1 — v0.1";
        // 
        // statusLabelSpring
        // 
        this.statusLabelSpring.Name = "statusLabelSpring";
        this.statusLabelSpring.Spring = true;
        // 
        // statusLabelClock
        // 
        this.statusLabelClock.Name = "statusLabelClock";
        this.statusLabelClock.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        // 
        // clockTimer
        // 
        this.clockTimer.Enabled = true;
        this.clockTimer.Interval = 30000;
        this.clockTimer.Tick += new System.EventHandler(this.ClockTimer_Tick);
        // 
        // MainForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 750);
        this.Controls.Add(this.panelContent);
        this.Controls.Add(this.statusStrip);
        this.Controls.Add(this.menuStrip);
        this.MinimumSize = new System.Drawing.Size(900, 600);
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "CRM1";
        this.menuStrip.ResumeLayout(false);
        this.panelContent.ResumeLayout(false);
        this.statusStrip.ResumeLayout(false);
        this.statusStrip.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.MenuStrip menuStrip;
    private System.Windows.Forms.ToolStripLabel appTitleItem;
    private System.Windows.Forms.ToolStripSeparator topSeparator;
    private System.Windows.Forms.ToolStripMenuItem clientsMenuItem;
    private System.Windows.Forms.ToolStripMenuItem searchMenuItem;
    private System.Windows.Forms.ToolStripMenuItem prospectsMenuItem;
    private System.Windows.Forms.ToolStripSeparator bottomSeparator;
    private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
    private System.Windows.Forms.Panel panelContent;
    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel statusLabelApp;
    private System.Windows.Forms.ToolStripStatusLabel statusLabelSpring;
    private System.Windows.Forms.ToolStripStatusLabel statusLabelClock;
    private System.Windows.Forms.Timer clockTimer;
}
