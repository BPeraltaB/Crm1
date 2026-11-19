namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Código generado por el Diseñador de Windows Forms para el formulario de
/// historial (specs/010).
/// </summary>
partial class HistoryForm
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
        this.filterPanel = new System.Windows.Forms.Panel();
        this.filterLabel = new System.Windows.Forms.Label();
        this.totalsLabel = new System.Windows.Forms.Label();
        this.counterLabel = new System.Windows.Forms.Label();
        this.historyDataGridView = new System.Windows.Forms.DataGridView();
        this.dateColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.typeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.detailColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.statusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.filterPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.historyDataGridView)).BeginInit();
        this.SuspendLayout();
        //
        // filterPanel
        //
        this.filterPanel.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
        this.filterPanel.Controls.Add(this.filterLabel);
        this.filterPanel.Controls.Add(this.totalsLabel);
        this.filterPanel.Controls.Add(this.counterLabel);
        this.filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.filterPanel.Location = new System.Drawing.Point(0, 0);
        this.filterPanel.Name = "filterPanel";
        this.filterPanel.Size = new System.Drawing.Size(1200, 40);
        this.filterPanel.TabIndex = 0;
        //
        // filterLabel
        //
        // Aviso «Historial de: (Cliente) Ana»: la vista siempre está filtrada
        // a un registro (specs/010 §3); no hay botón «Ver todas/os».
        this.filterLabel.Location = new System.Drawing.Point(12, 9);
        this.filterLabel.Name = "filterLabel";
        this.filterLabel.Size = new System.Drawing.Size(360, 20);
        this.filterLabel.TabIndex = 1;
        this.filterLabel.Visible = false;
        //
        // totalsLabel
        //
        // Totales del registro (specs/010 §1): pedidos, abonos, consumido y
        // presupuestos por estado; se recorta con puntos suspensivos si no cabe.
        this.totalsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.totalsLabel.AutoEllipsis = true;
        this.totalsLabel.Location = new System.Drawing.Point(360, 9);
        this.totalsLabel.Name = "totalsLabel";
        this.totalsLabel.Size = new System.Drawing.Size(610, 20);
        this.totalsLabel.TabIndex = 2;
        this.totalsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        //
        // counterLabel
        //
        // El texto lo actualizará la lógica del contador «Mostrando X de Y».
        this.counterLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.counterLabel.Location = new System.Drawing.Point(980, 9);
        this.counterLabel.Name = "counterLabel";
        this.counterLabel.Size = new System.Drawing.Size(210, 20);
        this.counterLabel.TabIndex = 0;
        this.counterLabel.Text = "Mostrando 0 de 0";
        this.counterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        //
        // historyDataGridView
        //
        // Tabla cronológica en solo lectura (specs/010): no hay edición.
        this.historyDataGridView.AllowUserToAddRows = false;
        this.historyDataGridView.AllowUserToDeleteRows = false;
        this.historyDataGridView.AllowUserToResizeRows = false;
        this.historyDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
        this.historyDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.historyDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
        this.dateColumn,
        this.typeColumn,
        this.detailColumn,
        this.statusColumn});
        this.historyDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.historyDataGridView.Location = new System.Drawing.Point(0, 40);
        this.historyDataGridView.MultiSelect = false;
        this.historyDataGridView.Name = "historyDataGridView";
        this.historyDataGridView.ReadOnly = true;
        this.historyDataGridView.RowHeadersVisible = false;
        // Las celdas se ven siempre blancas: la selección no se resalta en azul.
        this.historyDataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.White;
        this.historyDataGridView.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;
        this.historyDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.historyDataGridView.Size = new System.Drawing.Size(1200, 610);
        this.historyDataGridView.TabIndex = 1;
        this.historyDataGridView.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.HistoryDataGridView_RowPostPaint);
        this.historyDataGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.HistoryDataGridView_DataError);
        //
        // dateColumn
        //
        this.dateColumn.HeaderText = "Fecha";
        this.dateColumn.Name = "dateColumn";
        this.dateColumn.ReadOnly = true;
        //
        // typeColumn
        //
        this.typeColumn.HeaderText = "Tipo";
        this.typeColumn.Name = "typeColumn";
        this.typeColumn.ReadOnly = true;
        //
        // detailColumn
        //
        this.detailColumn.HeaderText = "Detalle";
        this.detailColumn.Name = "detailColumn";
        this.detailColumn.ReadOnly = true;
        //
        // statusColumn
        //
        this.statusColumn.HeaderText = "Estado";
        this.statusColumn.Name = "statusColumn";
        this.statusColumn.ReadOnly = true;
        //
        // HistoryForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 650);
        this.Controls.Add(this.historyDataGridView);
        this.Controls.Add(this.filterPanel);
        this.Name = "HistoryForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Historial";
        this.filterPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.historyDataGridView)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel filterPanel;
    private System.Windows.Forms.Label filterLabel;
    private System.Windows.Forms.Label totalsLabel;
    private System.Windows.Forms.Label counterLabel;
    private System.Windows.Forms.DataGridView historyDataGridView;
    private System.Windows.Forms.DataGridViewTextBoxColumn dateColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn typeColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn detailColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn statusColumn;
}
