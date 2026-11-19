namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Código generado por el Diseñador de Windows Forms para el formulario de clientes.
/// </summary>
partial class ClientForm
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
        this.tasksButton = new System.Windows.Forms.Button();
        this.quotesButton = new System.Windows.Forms.Button();
        this.ordersButton = new System.Windows.Forms.Button();
        this.historyButton = new System.Windows.Forms.Button();
        this.filterPanel = new System.Windows.Forms.Panel();
        this.searchLabel = new System.Windows.Forms.Label();
        this.searchTextBox = new System.Windows.Forms.TextBox();
        this.counterLabel = new System.Windows.Forms.Label();
        this.clientsDataGridView = new System.Windows.Forms.DataGridView();
        this.firstNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lastName1Column = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lastName2Column = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.taxIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.phoneColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.emailColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.addressColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.postalCodeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.cityColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.provinceColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this.statusColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this.registrationDateColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.notesColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.buttonPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.clientsDataGridView)).BeginInit();
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
        this.buttonPanel.Controls.Add(this.tasksButton);
        this.buttonPanel.Controls.Add(this.quotesButton);
        this.buttonPanel.Controls.Add(this.ordersButton);
        this.buttonPanel.Controls.Add(this.historyButton);
        this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.buttonPanel.Location = new System.Drawing.Point(0, 0);
        this.buttonPanel.Name = "buttonPanel";
        this.buttonPanel.Size = new System.Drawing.Size(1200, 100);
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
        // tasksButton
        // Segunda fila de la botonera: botones del registro marcado.
        //
        this.tasksButton.Location = new System.Drawing.Point(12, 47);
        this.tasksButton.Name = "tasksButton";
        this.tasksButton.Size = new System.Drawing.Size(120, 35);
        this.tasksButton.TabIndex = 5;
        this.tasksButton.Text = "Tareas";
        this.tasksButton.UseVisualStyleBackColor = true;
        this.tasksButton.Click += new System.EventHandler(this.TasksButton_Click);
        //
        // historyButton
        //
        this.historyButton.Location = new System.Drawing.Point(144, 47);
        this.historyButton.Name = "historyButton";
        this.historyButton.Size = new System.Drawing.Size(120, 35);
        this.historyButton.TabIndex = 6;
        this.historyButton.Text = "Historial";
        this.historyButton.UseVisualStyleBackColor = true;
        this.historyButton.Click += new System.EventHandler(this.HistoryButton_Click);
        //
        // quotesButton
        //
        this.quotesButton.Location = new System.Drawing.Point(276, 47);
        this.quotesButton.Name = "quotesButton";
        this.quotesButton.Size = new System.Drawing.Size(150, 35);
        this.quotesButton.TabIndex = 7;
        this.quotesButton.Text = "Presupuestos";
        this.quotesButton.UseVisualStyleBackColor = true;
        this.quotesButton.Click += new System.EventHandler(this.QuotesButton_Click);
        //
        // ordersButton
        //
        this.ordersButton.Location = new System.Drawing.Point(438, 47);
        this.ordersButton.Name = "ordersButton";
        this.ordersButton.Size = new System.Drawing.Size(120, 35);
        this.ordersButton.TabIndex = 8;
        this.ordersButton.Text = "Pedidos";
        this.ordersButton.UseVisualStyleBackColor = true;
        this.ordersButton.Click += new System.EventHandler(this.OrdersButton_Click);
        //
        // filterPanel
        //
        this.filterPanel.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
        this.filterPanel.Controls.Add(this.searchLabel);
        this.filterPanel.Controls.Add(this.searchTextBox);
        this.filterPanel.Controls.Add(this.counterLabel);
        this.filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.filterPanel.Location = new System.Drawing.Point(0, 100);
        this.filterPanel.Name = "filterPanel";
        this.filterPanel.Size = new System.Drawing.Size(1200, 40);
        this.filterPanel.TabIndex = 5;
        //
        // searchLabel
        //
        this.searchLabel.Location = new System.Drawing.Point(10, 9);
        this.searchLabel.Name = "searchLabel";
        this.searchLabel.Size = new System.Drawing.Size(50, 20);
        this.searchLabel.Text = "Buscar:";
        //
        // searchTextBox
        //
        this.searchTextBox.Location = new System.Drawing.Point(65, 6);
        this.searchTextBox.Name = "searchTextBox";
        this.searchTextBox.Size = new System.Drawing.Size(240, 23);
        this.searchTextBox.TabIndex = 0;
        this.searchTextBox.TextChanged += new System.EventHandler(this.SearchTextBox_TextChanged);
        //
        // counterLabel
        //
        this.counterLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.counterLabel.Location = new System.Drawing.Point(980, 9);
        this.counterLabel.Name = "counterLabel";
        this.counterLabel.Size = new System.Drawing.Size(210, 20);
        this.counterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        // El texto se rellena desde ApplyFilter.
        //
        // clientsDataGridView
        //
        this.clientsDataGridView.AllowUserToAddRows = false;
        this.clientsDataGridView.AllowUserToDeleteRows = false;
        this.clientsDataGridView.AllowUserToResizeRows = false;
        this.clientsDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
        this.clientsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.clientsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
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
        this.clientsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.clientsDataGridView.Location = new System.Drawing.Point(0, 140);
        this.clientsDataGridView.MultiSelect = false;
        this.clientsDataGridView.Name = "clientsDataGridView";
        this.clientsDataGridView.RowHeadersVisible = false;
        // Las celdas se ven siempre blancas: la selección no se resalta en azul.
        this.clientsDataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.White;
        this.clientsDataGridView.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;
        this.clientsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.clientsDataGridView.Size = new System.Drawing.Size(1200, 590);
        this.clientsDataGridView.TabIndex = 1;
        this.clientsDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.ClientsDataGridView_CellValueChanged);
        this.clientsDataGridView.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.ClientsDataGridView_RowsAdded);
        this.clientsDataGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.ClientsDataGridView_DataError);
        this.clientsDataGridView.SelectionChanged += new System.EventHandler(this.ClientsDataGridView_SelectionChanged);
        this.clientsDataGridView.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.ClientsDataGridView_RowPostPaint);
        this.clientsDataGridView.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.ClientsDataGridView_CellPainting);
        //
        // firstNameColumn
        //
        this.firstNameColumn.HeaderText = "Nombre";
        this.firstNameColumn.Name = "firstNameColumn";
        //
        // lastName1Column
        //
        this.lastName1Column.HeaderText = "Primer apellido";
        this.lastName1Column.Name = "lastName1Column";
        //
        // lastName2Column
        //
        this.lastName2Column.HeaderText = "Segundo apellido";
        this.lastName2Column.Name = "lastName2Column";
        //
        // taxIdColumn
        //
        this.taxIdColumn.HeaderText = "Documento";
        this.taxIdColumn.Name = "taxIdColumn";
        //
        // phoneColumn
        //
        this.phoneColumn.HeaderText = "Teléfono";
        this.phoneColumn.Name = "phoneColumn";
        //
        // emailColumn
        //
        this.emailColumn.HeaderText = "Correo electrónico";
        this.emailColumn.Name = "emailColumn";
        //
        // addressColumn
        //
        this.addressColumn.HeaderText = "Dirección";
        this.addressColumn.Name = "addressColumn";
        //
        // postalCodeColumn
        //
        this.postalCodeColumn.HeaderText = "Código postal";
        this.postalCodeColumn.Name = "postalCodeColumn";
        //
        // cityColumn
        //
        this.cityColumn.HeaderText = "Ciudad";
        this.cityColumn.Name = "cityColumn";
        //
        // provinceColumn
        //
        this.provinceColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
        this.provinceColumn.HeaderText = "Provincia";
        this.provinceColumn.Name = "provinceColumn";
        //
        // statusColumn
        //
        this.statusColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
        this.statusColumn.HeaderText = "Estado";
        this.statusColumn.Items.AddRange(new object[] {
        "Activo",
        "Inactivo"});
        this.statusColumn.Name = "statusColumn";
        //
        // registrationDateColumn
        //
        this.registrationDateColumn.HeaderText = "Fecha de alta";
        this.registrationDateColumn.Name = "registrationDateColumn";
        //
        // notesColumn
        //
        this.notesColumn.HeaderText = "Observaciones";
        this.notesColumn.Name = "notesColumn";
        //
        // ClientForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 650);
        this.Controls.Add(this.clientsDataGridView);
        this.Controls.Add(this.filterPanel);
        this.Controls.Add(this.buttonPanel);
        this.Name = "ClientForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Clientes";
        this.buttonPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.clientsDataGridView)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel buttonPanel;
    private System.Windows.Forms.Button newButton;
    private System.Windows.Forms.Button modifyButton;
    private System.Windows.Forms.Button deleteButton;
    private System.Windows.Forms.Button saveButton;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Button tasksButton;
    private System.Windows.Forms.Button quotesButton;
    private System.Windows.Forms.Button ordersButton;
    private System.Windows.Forms.Button historyButton;
    private System.Windows.Forms.Panel filterPanel;
    private System.Windows.Forms.Label searchLabel;
    private System.Windows.Forms.TextBox searchTextBox;
    private System.Windows.Forms.Label counterLabel;
    private System.Windows.Forms.DataGridView clientsDataGridView;
    private System.Windows.Forms.DataGridViewTextBoxColumn firstNameColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn lastName1Column;
    private System.Windows.Forms.DataGridViewTextBoxColumn lastName2Column;
    private System.Windows.Forms.DataGridViewTextBoxColumn taxIdColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn phoneColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn emailColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn addressColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn postalCodeColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn cityColumn;
    private System.Windows.Forms.DataGridViewComboBoxColumn provinceColumn;
    private System.Windows.Forms.DataGridViewComboBoxColumn statusColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn registrationDateColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn notesColumn;
}
