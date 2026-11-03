namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Código generado por el Diseñador de Windows Forms para el formulario de búsqueda.
/// </summary>
partial class SearchForm
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
        this.searchPanel = new System.Windows.Forms.Panel();
        this.searchLabel = new System.Windows.Forms.Label();
        this.searchTextBox = new System.Windows.Forms.TextBox();
        this.resultsDataGridView = new System.Windows.Forms.DataGridView();
        this.moduleColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.firstNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lastName1Column = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lastName2Column = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.taxIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.phoneColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.emailColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.addressColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.postalCodeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.cityColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.provinceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.statusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.registrationDateColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.notesColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.searchPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)this.resultsDataGridView).BeginInit();
        this.SuspendLayout();
        //
        // searchPanel
        //
        this.searchPanel.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
        this.searchPanel.Controls.Add(this.searchLabel);
        this.searchPanel.Controls.Add(this.searchTextBox);
        this.searchPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.searchPanel.Location = new System.Drawing.Point(0, 0);
        this.searchPanel.Name = "searchPanel";
        this.searchPanel.Size = new System.Drawing.Size(1000, 60);
        this.searchPanel.TabIndex = 0;
        //
        // searchLabel
        //
        this.searchLabel.AutoSize = true;
        this.searchLabel.Location = new System.Drawing.Point(28, 22);
        this.searchLabel.Name = "searchLabel";
        this.searchLabel.Size = new System.Drawing.Size(46, 15);
        this.searchLabel.TabIndex = 0;
        this.searchLabel.Text = "Buscar:";
        //
        // searchTextBox
        //
        this.searchTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
        | System.Windows.Forms.AnchorStyles.Right)));
        this.searchTextBox.Location = new System.Drawing.Point(90, 19);
        this.searchTextBox.Name = "searchTextBox";
        this.searchTextBox.Size = new System.Drawing.Size(860, 23);
        this.searchTextBox.TabIndex = 1;
        this.searchTextBox.TextChanged += new System.EventHandler(this.SearchTextBox_TextChanged);
        //
        // resultsDataGridView
        //
        this.resultsDataGridView.AllowUserToAddRows = false;
        this.resultsDataGridView.AllowUserToDeleteRows = false;
        this.resultsDataGridView.AllowUserToResizeRows = false;
        this.resultsDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
        this.resultsDataGridView.BackgroundColor = System.Drawing.Color.White;
        this.resultsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.resultsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.moduleColumn,
            this.firstNameColumn,
            this.lastName1Column,
            this.lastName2Column,
            this.taxIdColumn,
            this.phoneColumn,
            this.emailColumn,
            this.addressColumn,
            this.postalCodeColumn,
            this.cityColumn,
            this.provinceColumn,
            this.statusColumn,
            this.registrationDateColumn,
            this.notesColumn});
        this.resultsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.resultsDataGridView.Location = new System.Drawing.Point(0, 60);
        this.resultsDataGridView.MultiSelect = false;
        this.resultsDataGridView.Name = "resultsDataGridView";
        this.resultsDataGridView.ReadOnly = true;
        this.resultsDataGridView.RowHeadersVisible = false;
        this.resultsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.resultsDataGridView.Size = new System.Drawing.Size(1000, 590);
        this.resultsDataGridView.TabIndex = 1;
        this.resultsDataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ResultsDataGridView_CellDoubleClick);
        this.resultsDataGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ResultsDataGridView_KeyDown);
        this.resultsDataGridView.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.ResultsDataGridView_RowPostPaint);
        this.resultsDataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.White;
        this.resultsDataGridView.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;
        //
        // moduleColumn
        //
        this.moduleColumn.HeaderText = "Módulo";
        this.moduleColumn.Name = "moduleColumn";
        this.moduleColumn.ReadOnly = true;
        //
        // firstNameColumn
        //
        this.firstNameColumn.HeaderText = "Nombre";
        this.firstNameColumn.Name = "firstNameColumn";
        this.firstNameColumn.ReadOnly = true;
        //
        // lastName1Column
        //
        this.lastName1Column.HeaderText = "Primer apellido";
        this.lastName1Column.Name = "lastName1Column";
        this.lastName1Column.ReadOnly = true;
        //
        // lastName2Column
        //
        this.lastName2Column.HeaderText = "Segundo apellido";
        this.lastName2Column.Name = "lastName2Column";
        this.lastName2Column.ReadOnly = true;
        //
        // taxIdColumn
        //
        this.taxIdColumn.HeaderText = "Documento";
        this.taxIdColumn.Name = "taxIdColumn";
        this.taxIdColumn.ReadOnly = true;
        //
        // phoneColumn
        //
        this.phoneColumn.HeaderText = "Teléfono";
        this.phoneColumn.Name = "phoneColumn";
        this.phoneColumn.ReadOnly = true;
        //
        // emailColumn
        //
        this.emailColumn.HeaderText = "Correo electrónico";
        this.emailColumn.Name = "emailColumn";
        this.emailColumn.ReadOnly = true;
        //
        // addressColumn
        //
        this.addressColumn.HeaderText = "Dirección";
        this.addressColumn.Name = "addressColumn";
        this.addressColumn.ReadOnly = true;
        //
        // postalCodeColumn
        //
        this.postalCodeColumn.HeaderText = "Código postal";
        this.postalCodeColumn.Name = "postalCodeColumn";
        this.postalCodeColumn.ReadOnly = true;
        //
        // cityColumn
        //
        this.cityColumn.HeaderText = "Ciudad";
        this.cityColumn.Name = "cityColumn";
        this.cityColumn.ReadOnly = true;
        //
        // provinceColumn
        //
        this.provinceColumn.HeaderText = "Provincia";
        this.provinceColumn.Name = "provinceColumn";
        this.provinceColumn.ReadOnly = true;
        //
        // statusColumn
        //
        this.statusColumn.HeaderText = "Estado";
        this.statusColumn.Name = "statusColumn";
        this.statusColumn.ReadOnly = true;
        //
        // registrationDateColumn
        //
        this.registrationDateColumn.HeaderText = "Fecha de alta";
        this.registrationDateColumn.Name = "registrationDateColumn";
        this.registrationDateColumn.ReadOnly = true;
        //
        // notesColumn
        //
        this.notesColumn.HeaderText = "Observaciones";
        this.notesColumn.Name = "notesColumn";
        this.notesColumn.ReadOnly = true;
        //
        // SearchForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
        this.ClientSize = new System.Drawing.Size(1000, 650);
        this.Controls.Add(this.resultsDataGridView);
        this.Controls.Add(this.searchPanel);
        this.Name = "SearchForm";
        this.Text = "Búsqueda";
        this.searchPanel.ResumeLayout(false);
        this.searchPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)this.resultsDataGridView).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel searchPanel;
    private System.Windows.Forms.Label searchLabel;
    private System.Windows.Forms.TextBox searchTextBox;
    private System.Windows.Forms.DataGridView resultsDataGridView;
    private System.Windows.Forms.DataGridViewTextBoxColumn moduleColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn firstNameColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn lastName1Column;
    private System.Windows.Forms.DataGridViewTextBoxColumn lastName2Column;
    private System.Windows.Forms.DataGridViewTextBoxColumn taxIdColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn phoneColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn emailColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn addressColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn postalCodeColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn cityColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn provinceColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn statusColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn registrationDateColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn notesColumn;
}
