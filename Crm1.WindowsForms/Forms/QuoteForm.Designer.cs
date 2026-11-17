namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Código generado por el Diseñador de Windows Forms para el formulario de presupuestos.
/// </summary>
partial class QuoteForm
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
        this.buttonPanel = new System.Windows.Forms.Panel();
        this.newButton = new System.Windows.Forms.Button();
        this.modifyButton = new System.Windows.Forms.Button();
        this.deleteButton = new System.Windows.Forms.Button();
        this.saveButton = new System.Windows.Forms.Button();
        this.cancelButton = new System.Windows.Forms.Button();
        this.acceptButton = new System.Windows.Forms.Button();
        this.filterPanel = new System.Windows.Forms.Panel();
        this.searchLabel = new System.Windows.Forms.Label();
        this.searchTextBox = new System.Windows.Forms.TextBox();
        this.yearLabel = new System.Windows.Forms.Label();
        this.yearFilterComboBox = new System.Windows.Forms.ComboBox();
        this.showAllButton = new System.Windows.Forms.Button();
        this.filterLabel = new System.Windows.Forms.Label();
        this.totalsLabel = new System.Windows.Forms.Label();
        this.counterLabel = new System.Windows.Forms.Label();
        this.quotesDataGridView = new System.Windows.Forms.DataGridView();
        this.quoteDateColumn = new Crm1.WindowsForms.Forms.CalendarColumn();
        this.amountColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.referenceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.descriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.statusColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this.relatedColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this.buttonPanel.SuspendLayout();
        this.filterPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)this.quotesDataGridView).BeginInit();
        this.SuspendLayout();
        //
        // buttonPanel
        //
        this.buttonPanel.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
        this.buttonPanel.Controls.Add(this.newButton);
        this.buttonPanel.Controls.Add(this.modifyButton);
        this.buttonPanel.Controls.Add(this.deleteButton);
        this.buttonPanel.Controls.Add(this.saveButton);
        this.buttonPanel.Controls.Add(this.cancelButton);
        this.buttonPanel.Controls.Add(this.acceptButton);
        this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.buttonPanel.Location = new System.Drawing.Point(0, 0);
        this.buttonPanel.Name = "buttonPanel";
        this.buttonPanel.Size = new System.Drawing.Size(1200, 60);
        this.buttonPanel.TabIndex = 0;
        //
        // newButton
        //
        this.newButton.Location = new System.Drawing.Point(12, 12);
        this.newButton.Name = "newButton";
        this.newButton.Size = new System.Drawing.Size(120, 35);
        this.newButton.TabIndex = 0;
        this.newButton.Text = "Nuevo";
        this.newButton.UseVisualStyleBackColor = true;
        this.newButton.Click += new System.EventHandler(this.NewButton_Click);
        //
        // modifyButton
        //
        this.modifyButton.Location = new System.Drawing.Point(144, 12);
        this.modifyButton.Name = "modifyButton";
        this.modifyButton.Size = new System.Drawing.Size(120, 35);
        this.modifyButton.TabIndex = 1;
        this.modifyButton.Text = "Modificar";
        this.modifyButton.UseVisualStyleBackColor = true;
        this.modifyButton.Click += new System.EventHandler(this.ModifyButton_Click);
        //
        // deleteButton
        //
        this.deleteButton.Location = new System.Drawing.Point(276, 12);
        this.deleteButton.Name = "deleteButton";
        this.deleteButton.Size = new System.Drawing.Size(120, 35);
        this.deleteButton.TabIndex = 2;
        this.deleteButton.Text = "Eliminar";
        this.deleteButton.UseVisualStyleBackColor = true;
        this.deleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
        //
        // saveButton
        //
        this.saveButton.Location = new System.Drawing.Point(408, 12);
        this.saveButton.Name = "saveButton";
        this.saveButton.Size = new System.Drawing.Size(120, 35);
        this.saveButton.TabIndex = 3;
        this.saveButton.Text = "Guardar";
        this.saveButton.UseVisualStyleBackColor = true;
        this.saveButton.Click += new System.EventHandler(this.SaveButton_Click);
        //
        // cancelButton
        //
        this.cancelButton.Enabled = false;
        this.cancelButton.Location = new System.Drawing.Point(540, 12);
        this.cancelButton.Name = "cancelButton";
        this.cancelButton.Size = new System.Drawing.Size(120, 35);
        this.cancelButton.TabIndex = 4;
        this.cancelButton.Text = "Cancelar";
        this.cancelButton.UseVisualStyleBackColor = true;
        this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
        //
        // acceptButton
        // Acepta el presupuesto marcado (Pendiente) o crea su pedido (Aceptado
        // sin pedido); su texto lo ajusta la lógica según la fila marcada.
        //
        this.acceptButton.Location = new System.Drawing.Point(672, 12);
        this.acceptButton.Name = "acceptButton";
        this.acceptButton.Size = new System.Drawing.Size(150, 35);
        this.acceptButton.TabIndex = 5;
        this.acceptButton.Text = "Aceptar";
        this.acceptButton.UseVisualStyleBackColor = true;
        this.acceptButton.Click += new System.EventHandler(this.AcceptButton_Click);
        //
        // filterPanel
        //
        this.filterPanel.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
        this.filterPanel.Controls.Add(this.searchLabel);
        this.filterPanel.Controls.Add(this.searchTextBox);
        this.filterPanel.Controls.Add(this.yearLabel);
        this.filterPanel.Controls.Add(this.yearFilterComboBox);
        this.filterPanel.Controls.Add(this.showAllButton);
        this.filterPanel.Controls.Add(this.filterLabel);
        this.filterPanel.Controls.Add(this.totalsLabel);
        this.filterPanel.Controls.Add(this.counterLabel);
        this.filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.filterPanel.Location = new System.Drawing.Point(0, 60);
        this.filterPanel.Name = "filterPanel";
        this.filterPanel.Size = new System.Drawing.Size(1200, 40);
        this.filterPanel.TabIndex = 5;
        //
        // searchLabel
        //
        this.searchLabel.Location = new System.Drawing.Point(10, 9);
        this.searchLabel.Name = "searchLabel";
        this.searchLabel.Size = new System.Drawing.Size(45, 20);
        this.searchLabel.Text = "Buscar:";
        //
        // searchTextBox
        // Búsqueda en vivo del listado (parcial, sin mayúsculas ni tildes).
        //
        this.searchTextBox.Location = new System.Drawing.Point(60, 6);
        this.searchTextBox.Name = "searchTextBox";
        this.searchTextBox.Size = new System.Drawing.Size(170, 23);
        this.searchTextBox.TabIndex = 0;
        this.searchTextBox.TextChanged += new System.EventHandler(this.SearchTextBox_TextChanged);
        //
        // yearLabel
        //
        this.yearLabel.Location = new System.Drawing.Point(240, 9);
        this.yearLabel.Name = "yearLabel";
        this.yearLabel.Size = new System.Drawing.Size(35, 20);
        this.yearLabel.Text = "Año:";
        //
        // yearFilterComboBox
        // «Todos» (por defecto) y los años presentes en los presupuestos.
        //
        this.yearFilterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.yearFilterComboBox.Location = new System.Drawing.Point(280, 6);
        this.yearFilterComboBox.Name = "yearFilterComboBox";
        this.yearFilterComboBox.Size = new System.Drawing.Size(85, 23);
        this.yearFilterComboBox.TabIndex = 1;
        this.yearFilterComboBox.SelectedIndexChanged += new System.EventHandler(this.YearFilterComboBox_SelectedIndexChanged);
        //
        // showAllButton
        // Quita el filtro y vuelve a mostrar todos los presupuestos.
        //
        this.showAllButton.Location = new System.Drawing.Point(375, 6);
        this.showAllButton.Name = "showAllButton";
        this.showAllButton.Size = new System.Drawing.Size(90, 27);
        this.showAllButton.TabIndex = 2;
        this.showAllButton.Text = "Ver todas";
        this.showAllButton.Visible = false;
        this.showAllButton.Click += new System.EventHandler(this.ShowAllButton_Click);
        //
        // filterLabel
        // Aviso «Presupuestos de: (Cliente) Ana»; visible solo con filtro activo.
        //
        this.filterLabel.Location = new System.Drawing.Point(475, 9);
        this.filterLabel.Name = "filterLabel";
        this.filterLabel.Size = new System.Drawing.Size(210, 20);
        this.filterLabel.TabIndex = 3;
        this.filterLabel.Visible = false;
        //
        // totalsLabel
        // Resumen de presupuestos según el año y el cliente filtrados.
        //
        this.totalsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.totalsLabel.Location = new System.Drawing.Point(690, 9);
        this.totalsLabel.Name = "totalsLabel";
        this.totalsLabel.Size = new System.Drawing.Size(280, 20);
        this.totalsLabel.TabIndex = 4;
        this.totalsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        //
        // counterLabel
        // El texto lo actualizará la lógica del contador «Mostrando X de Y».
        //
        this.counterLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.counterLabel.Location = new System.Drawing.Point(980, 9);
        this.counterLabel.Name = "counterLabel";
        this.counterLabel.Size = new System.Drawing.Size(210, 20);
        this.counterLabel.TabIndex = 0;
        this.counterLabel.Text = "Mostrando 0 de 0";
        this.counterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        //
        // quotesDataGridView
        // Tabla en solo lectura por defecto; el borde de selección sí está activo.
        //
        this.quotesDataGridView.AllowUserToAddRows = false;
        this.quotesDataGridView.AllowUserToDeleteRows = false;
        this.quotesDataGridView.AllowUserToResizeRows = false;
        this.quotesDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
        this.quotesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.quotesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
        this.quoteDateColumn,
        this.amountColumn,
        this.referenceColumn,
        this.descriptionColumn,
        this.statusColumn,
        this.relatedColumn});
        this.quotesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.quotesDataGridView.Location = new System.Drawing.Point(0, 100);
        this.quotesDataGridView.MultiSelect = false;
        this.quotesDataGridView.Name = "quotesDataGridView";
        this.quotesDataGridView.RowHeadersVisible = false;
        // Las celdas se ven siempre blancas: la selección no se resalta en azul.
        this.quotesDataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.White;
        this.quotesDataGridView.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;
        this.quotesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.quotesDataGridView.Size = new System.Drawing.Size(1200, 590);
        this.quotesDataGridView.TabIndex = 6;
        this.quotesDataGridView.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.QuotesDataGridView_RowPostPaint);
        this.quotesDataGridView.SelectionChanged += new System.EventHandler(this.QuotesDataGridView_SelectionChanged);
        this.quotesDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.QuotesDataGridView_CellValueChanged);
        this.quotesDataGridView.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.QuotesDataGridView_RowsAdded);
        this.quotesDataGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.QuotesDataGridView_DataError);
        this.quotesDataGridView.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.QuotesDataGridView_CellPainting);
        //
        // quoteDateColumn
        // Columna de calendario: al editar se despliega un DateTimePicker.
        //
        this.quoteDateColumn.HeaderText = "Fecha";
        this.quoteDateColumn.Name = "quoteDateColumn";
        //
        // amountColumn
        // Precio en formato español («1.234,56»); la cabecera indica los euros.
        //
        this.amountColumn.HeaderText = "Precio (€)";
        this.amountColumn.Name = "amountColumn";
        //
        // referenceColumn
        //
        this.referenceColumn.HeaderText = "Referencia";
        this.referenceColumn.Name = "referenceColumn";
        //
        // descriptionColumn
        //
        this.descriptionColumn.HeaderText = "Descripción";
        this.descriptionColumn.Name = "descriptionColumn";
        //
        // statusColumn
        //
        this.statusColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
        this.statusColumn.HeaderText = "Estado";
        this.statusColumn.Items.AddRange(new object[] {
        "Pendiente",
        "Aceptado",
        "Descartado"});
        this.statusColumn.Name = "statusColumn";
        //
        // relatedColumn
        // Desplegable con la unión de clientes y futuros clientes; se carga
        // al abrir el formulario (prefijo «(Cliente) …» / «(Futuro cliente) …»).
        //
        this.relatedColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
        this.relatedColumn.HeaderText = "Cliente/Futuro cliente";
        this.relatedColumn.Name = "relatedColumn";
        //
        // QuoteForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 650);
        this.Controls.Add(this.quotesDataGridView);
        this.Controls.Add(this.filterPanel);
        this.Controls.Add(this.buttonPanel);
        this.Name = "QuoteForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Presupuestos";
        this.buttonPanel.ResumeLayout(false);
        this.filterPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)this.quotesDataGridView).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel buttonPanel;
    private System.Windows.Forms.Button newButton;
    private System.Windows.Forms.Button modifyButton;
    private System.Windows.Forms.Button deleteButton;
    private System.Windows.Forms.Button saveButton;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Button acceptButton;
    private System.Windows.Forms.Panel filterPanel;
    private System.Windows.Forms.Label searchLabel;
    private System.Windows.Forms.TextBox searchTextBox;
    private System.Windows.Forms.Label yearLabel;
    private System.Windows.Forms.ComboBox yearFilterComboBox;
    private System.Windows.Forms.Button showAllButton;
    private System.Windows.Forms.Label filterLabel;
    private System.Windows.Forms.Label totalsLabel;
    private System.Windows.Forms.Label counterLabel;
    private System.Windows.Forms.DataGridView quotesDataGridView;
    private Crm1.WindowsForms.Forms.CalendarColumn quoteDateColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn amountColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn referenceColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn descriptionColumn;
    private System.Windows.Forms.DataGridViewComboBoxColumn statusColumn;
    private System.Windows.Forms.DataGridViewComboBoxColumn relatedColumn;
}
