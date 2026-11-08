namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Código generado por el Diseñador de Windows Forms para el formulario de futuros clientes.
/// </summary>
partial class ContactForm
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
        this.convertButton = new System.Windows.Forms.Button();
        this.filterPanel = new System.Windows.Forms.Panel();
        this.counterLabel = new System.Windows.Forms.Label();
        this.contactsDataGridView = new System.Windows.Forms.DataGridView();
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
        ((System.ComponentModel.ISupportInitialize)(this.contactsDataGridView)).BeginInit();
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
        this.buttonPanel.Controls.Add(this.convertButton);
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
        // convertButton
        //
        this.convertButton.Location = new System.Drawing.Point(672, 12);
        this.convertButton.Name = "convertButton";
        this.convertButton.Size = new System.Drawing.Size(150, 35);
        this.convertButton.TabIndex = 5;
        this.convertButton.Text = "Convertir en cliente";
        this.convertButton.UseVisualStyleBackColor = true;
        this.convertButton.Click += new System.EventHandler(this.ConvertButton_Click);
        //
        // filterPanel
        //
        this.filterPanel.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
        this.filterPanel.Controls.Add(this.counterLabel);
        this.filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.filterPanel.Location = new System.Drawing.Point(0, 60);
        this.filterPanel.Name = "filterPanel";
        this.filterPanel.Size = new System.Drawing.Size(1200, 40);
        this.filterPanel.TabIndex = 6;
        //
        // counterLabel
        //
        this.counterLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.counterLabel.Location = new System.Drawing.Point(980, 9);
        this.counterLabel.Name = "counterLabel";
        this.counterLabel.Size = new System.Drawing.Size(210, 20);
        this.counterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        // El texto se rellena desde UpdateCounter.
        //
        // contactsDataGridView
        //
        this.contactsDataGridView.AllowUserToAddRows = false;
        this.contactsDataGridView.AllowUserToDeleteRows = false;
        this.contactsDataGridView.AllowUserToResizeRows = false;
        this.contactsDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
        this.contactsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.contactsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
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
        this.contactsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.contactsDataGridView.Location = new System.Drawing.Point(0, 100);
        this.contactsDataGridView.MultiSelect = false;
        this.contactsDataGridView.Name = "contactsDataGridView";
        this.contactsDataGridView.RowHeadersVisible = false;
        // Las celdas se ven siempre blancas: la selección no se resalta en azul.
        this.contactsDataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.White;
        this.contactsDataGridView.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;
        this.contactsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.contactsDataGridView.Size = new System.Drawing.Size(1200, 590);
        this.contactsDataGridView.TabIndex = 1;
        this.contactsDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.ContactsDataGridView_CellValueChanged);
        this.contactsDataGridView.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.ContactsDataGridView_RowsAdded);
        this.contactsDataGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.ContactsDataGridView_DataError);
        this.contactsDataGridView.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.ContactsDataGridView_CellPainting);
        this.contactsDataGridView.SelectionChanged += new System.EventHandler(this.ContactsDataGridView_SelectionChanged);
        this.contactsDataGridView.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.ContactsDataGridView_RowPostPaint);
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
        "Pendiente",
        "Descartado"});
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
        // ContactForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 650);
        this.Controls.Add(this.contactsDataGridView);
        this.Controls.Add(this.filterPanel);
        this.Controls.Add(this.buttonPanel);
        this.Name = "ContactForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Futuros clientes";
        this.buttonPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.contactsDataGridView)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel buttonPanel;
    private System.Windows.Forms.Button newButton;
    private System.Windows.Forms.Button modifyButton;
    private System.Windows.Forms.Button deleteButton;
    private System.Windows.Forms.Button saveButton;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Button convertButton;
    private System.Windows.Forms.Panel filterPanel;
    private System.Windows.Forms.Label counterLabel;
    private System.Windows.Forms.DataGridView contactsDataGridView;
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
