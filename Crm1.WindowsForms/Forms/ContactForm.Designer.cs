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
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        buttonPanel = new Panel();
        newButton = new Button();
        modifyButton = new Button();
        deleteButton = new Button();
        saveButton = new Button();
        cancelButton = new Button();
        convertButton = new Button();
        tasksButton = new Button();
        quotesButton = new Button();
        historyButton = new Button();
        filterPanel = new Panel();
        searchLabel = new Label();
        searchTextBox = new TextBox();
        counterLabel = new Label();
        contactsDataGridView = new DataGridView();
        firstNameColumn = new DataGridViewTextBoxColumn();
        lastName1Column = new DataGridViewTextBoxColumn();
        lastName2Column = new DataGridViewTextBoxColumn();
        taxIdColumn = new DataGridViewTextBoxColumn();
        phoneColumn = new DataGridViewTextBoxColumn();
        emailColumn = new DataGridViewTextBoxColumn();
        addressColumn = new DataGridViewTextBoxColumn();
        postalCodeColumn = new DataGridViewTextBoxColumn();
        cityColumn = new DataGridViewTextBoxColumn();
        provinceColumn = new DataGridViewComboBoxColumn();
        statusColumn = new DataGridViewComboBoxColumn();
        registrationDateColumn = new DataGridViewTextBoxColumn();
        notesColumn = new DataGridViewTextBoxColumn();
        buttonPanel.SuspendLayout();
        filterPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)contactsDataGridView).BeginInit();
        SuspendLayout();
        // 
        // buttonPanel
        // 
        buttonPanel.BackColor = Color.FromArgb(243, 244, 246);
        buttonPanel.Controls.Add(newButton);
        buttonPanel.Controls.Add(modifyButton);
        buttonPanel.Controls.Add(deleteButton);
        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(convertButton);
        buttonPanel.Controls.Add(tasksButton);
        buttonPanel.Controls.Add(quotesButton);
        buttonPanel.Controls.Add(historyButton);
        buttonPanel.Dock = DockStyle.Top;
        buttonPanel.Location = new Point(0, 0);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Size = new Size(1200, 60);
        buttonPanel.TabIndex = 0;
        // 
        // newButton
        // 
        newButton.Location = new Point(12, 12);
        newButton.Name = "newButton";
        newButton.Size = new Size(120, 35);
        newButton.TabIndex = 0;
        newButton.Text = "Nuevo";
        newButton.UseVisualStyleBackColor = true;
        newButton.Click += NewButton_Click;
        // 
        // modifyButton
        // 
        modifyButton.Location = new Point(144, 12);
        modifyButton.Name = "modifyButton";
        modifyButton.Size = new Size(120, 35);
        modifyButton.TabIndex = 1;
        modifyButton.Text = "Modificar";
        modifyButton.UseVisualStyleBackColor = true;
        modifyButton.Click += ModifyButton_Click;
        // 
        // deleteButton
        // 
        deleteButton.Location = new Point(276, 12);
        deleteButton.Name = "deleteButton";
        deleteButton.Size = new Size(120, 35);
        deleteButton.TabIndex = 2;
        deleteButton.Text = "Eliminar";
        deleteButton.UseVisualStyleBackColor = true;
        deleteButton.Click += DeleteButton_Click;
        // 
        // saveButton
        // 
        saveButton.Location = new Point(408, 12);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(120, 35);
        saveButton.TabIndex = 3;
        saveButton.Text = "Guardar";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += SaveButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.Enabled = false;
        cancelButton.Location = new Point(540, 12);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(120, 35);
        cancelButton.TabIndex = 4;
        cancelButton.Text = "Cancelar";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += CancelButton_Click;
        // 
        // convertButton
        // 
        convertButton.Location = new Point(795, 12);
        convertButton.Name = "convertButton";
        convertButton.Size = new Size(150, 35);
        convertButton.TabIndex = 5;
        convertButton.Text = "Convertir en cliente";
        convertButton.UseVisualStyleBackColor = true;
        convertButton.Click += ConvertButton_Click;
        // 
        // tasksButton
        // 
        tasksButton.Location = new Point(667, 12);
        tasksButton.Name = "tasksButton";
        tasksButton.Size = new Size(120, 35);
        tasksButton.TabIndex = 6;
        tasksButton.Text = "Tareas";
        tasksButton.UseVisualStyleBackColor = true;
        tasksButton.Click += TasksButton_Click;
        //
        // quotesButton
        //
        // Corregido: estaba en x=802 y pisaba al botón «Convertir en cliente»
        // (795–945); pasa a continuación de él (specs/010 §4).
        quotesButton.Location = new Point(957, 12);
        quotesButton.Name = "quotesButton";
        quotesButton.Size = new Size(150, 35);
        quotesButton.TabIndex = 7;
        quotesButton.Text = "Presupuestos";
        quotesButton.UseVisualStyleBackColor = true;
        quotesButton.Click += QuotesButton_Click;
        //
        // historyButton
        //
        historyButton.Location = new Point(1119, 12);
        historyButton.Name = "historyButton";
        historyButton.Size = new Size(105, 35);
        historyButton.TabIndex = 8;
        historyButton.Text = "Historial";
        historyButton.UseVisualStyleBackColor = true;
        historyButton.Click += HistoryButton_Click;
        //
        // filterPanel
        //
        filterPanel.BackColor = Color.FromArgb(243, 244, 246);
        filterPanel.Controls.Add(searchLabel);
        filterPanel.Controls.Add(searchTextBox);
        filterPanel.Controls.Add(counterLabel);
        filterPanel.Dock = DockStyle.Top;
        filterPanel.Location = new Point(0, 60);
        filterPanel.Name = "filterPanel";
        filterPanel.Size = new Size(1200, 40);
        filterPanel.TabIndex = 6;
        // 
        // searchLabel
        // 
        searchLabel.Location = new Point(10, 9);
        searchLabel.Name = "searchLabel";
        searchLabel.Size = new Size(50, 20);
        searchLabel.Text = "Buscar:";
        // 
        // searchTextBox
        // 
        searchTextBox.Location = new Point(65, 6);
        searchTextBox.Name = "searchTextBox";
        searchTextBox.Size = new Size(240, 23);
        searchTextBox.TabIndex = 1;
        searchTextBox.TextChanged += SearchTextBox_TextChanged;
        // 
        // counterLabel
        // 
        counterLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        counterLabel.Location = new Point(1980, 9);
        counterLabel.Name = "counterLabel";
        counterLabel.Size = new Size(210, 20);
        counterLabel.TabIndex = 0;
        counterLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // contactsDataGridView
        // 
        contactsDataGridView.AllowUserToAddRows = false;
        contactsDataGridView.AllowUserToDeleteRows = false;
        contactsDataGridView.AllowUserToResizeRows = false;
        contactsDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        contactsDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        contactsDataGridView.Columns.AddRange(new DataGridViewColumn[] { firstNameColumn, lastName1Column, lastName2Column, taxIdColumn, phoneColumn, emailColumn, addressColumn, postalCodeColumn, cityColumn, provinceColumn, statusColumn, registrationDateColumn, notesColumn });
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = SystemColors.Window;
        dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyle1.SelectionBackColor = Color.White;
        dataGridViewCellStyle1.SelectionForeColor = Color.Black;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
        contactsDataGridView.DefaultCellStyle = dataGridViewCellStyle1;
        contactsDataGridView.Dock = DockStyle.Fill;
        contactsDataGridView.Location = new Point(0, 100);
        contactsDataGridView.MultiSelect = false;
        contactsDataGridView.Name = "contactsDataGridView";
        contactsDataGridView.RowHeadersVisible = false;
        contactsDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        contactsDataGridView.Size = new Size(1200, 550);
        contactsDataGridView.TabIndex = 1;
        contactsDataGridView.CellPainting += ContactsDataGridView_CellPainting;
        contactsDataGridView.CellValueChanged += ContactsDataGridView_CellValueChanged;
        contactsDataGridView.DataError += ContactsDataGridView_DataError;
        contactsDataGridView.RowPostPaint += ContactsDataGridView_RowPostPaint;
        contactsDataGridView.RowsAdded += ContactsDataGridView_RowsAdded;
        contactsDataGridView.SelectionChanged += ContactsDataGridView_SelectionChanged;
        // 
        // firstNameColumn
        // 
        firstNameColumn.HeaderText = "Nombre";
        firstNameColumn.Name = "firstNameColumn";
        firstNameColumn.Width = 76;
        // 
        // lastName1Column
        // 
        lastName1Column.HeaderText = "Primer apellido";
        lastName1Column.Name = "lastName1Column";
        lastName1Column.Width = 103;
        // 
        // lastName2Column
        // 
        lastName2Column.HeaderText = "Segundo apellido";
        lastName2Column.Name = "lastName2Column";
        lastName2Column.Width = 114;
        // 
        // taxIdColumn
        // 
        taxIdColumn.HeaderText = "Documento";
        taxIdColumn.Name = "taxIdColumn";
        taxIdColumn.Width = 95;
        // 
        // phoneColumn
        // 
        phoneColumn.HeaderText = "Teléfono";
        phoneColumn.Name = "phoneColumn";
        phoneColumn.Width = 78;
        // 
        // emailColumn
        // 
        emailColumn.HeaderText = "Correo electrónico";
        emailColumn.Name = "emailColumn";
        emailColumn.Width = 119;
        // 
        // addressColumn
        // 
        addressColumn.HeaderText = "Dirección";
        addressColumn.Name = "addressColumn";
        addressColumn.Width = 82;
        // 
        // postalCodeColumn
        // 
        postalCodeColumn.HeaderText = "Código postal";
        postalCodeColumn.Name = "postalCodeColumn";
        postalCodeColumn.Width = 97;
        // 
        // cityColumn
        // 
        cityColumn.HeaderText = "Ciudad";
        cityColumn.Name = "cityColumn";
        cityColumn.Width = 70;
        // 
        // provinceColumn
        // 
        provinceColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
        provinceColumn.HeaderText = "Provincia";
        provinceColumn.Name = "provinceColumn";
        provinceColumn.Width = 62;
        // 
        // statusColumn
        // 
        statusColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
        statusColumn.HeaderText = "Estado";
        statusColumn.Items.AddRange(new object[] { "Pendiente", "Descartado" });
        statusColumn.Name = "statusColumn";
        statusColumn.Width = 48;
        // 
        // registrationDateColumn
        // 
        registrationDateColumn.HeaderText = "Fecha de alta";
        registrationDateColumn.Name = "registrationDateColumn";
        registrationDateColumn.Width = 76;
        // 
        // notesColumn
        // 
        notesColumn.HeaderText = "Observaciones";
        notesColumn.Name = "notesColumn";
        notesColumn.Width = 109;
        // 
        // ContactForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 650);
        Controls.Add(contactsDataGridView);
        Controls.Add(filterPanel);
        Controls.Add(buttonPanel);
        Name = "ContactForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Futuros clientes";
        buttonPanel.ResumeLayout(false);
        filterPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)contactsDataGridView).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel buttonPanel;
    private System.Windows.Forms.Button newButton;
    private System.Windows.Forms.Button modifyButton;
    private System.Windows.Forms.Button deleteButton;
    private System.Windows.Forms.Button saveButton;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Button convertButton;
    private System.Windows.Forms.Button tasksButton;
    private System.Windows.Forms.Button quotesButton;
    private System.Windows.Forms.Button historyButton;
    private System.Windows.Forms.Panel filterPanel;
    private System.Windows.Forms.Label searchLabel;
    private System.Windows.Forms.TextBox searchTextBox;
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
