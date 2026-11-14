namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Código generado por el Diseñador de Windows Forms para el formulario de
/// recordatorios. Carga en solo lectura (specs/007): la botonera está
/// inactiva hasta que exista la edición.
/// </summary>
partial class ReminderForm
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
        this.filterPanel = new System.Windows.Forms.Panel();
        this.counterLabel = new System.Windows.Forms.Label();
        this.remindersDataGridView = new System.Windows.Forms.DataGridView();
        this.subjectColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.relatedColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this.remindAtColumn = new Crm1.WindowsForms.Forms.CalendarColumn("dd/MM/yyyy HH:mm");
        this.statusColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this.buttonPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.remindersDataGridView)).BeginInit();
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
        this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.buttonPanel.Location = new System.Drawing.Point(0, 0);
        this.buttonPanel.Name = "buttonPanel";
        this.buttonPanel.Size = new System.Drawing.Size(1200, 60);
        this.buttonPanel.TabIndex = 0;
        //
        // newButton
        // Activo tras una carga correcta; sin edición abierta (skill «modificar»).
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
        // Activo en estado normal; sin edición abierta (skill «modificar»).
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
        // Activo en estado normal; sin edición abierta (skill «borrar»).
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
        // Solo activo durante una edición (skill «modificar»).
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
        // filterPanel
        //
        this.filterPanel.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
        this.filterPanel.Controls.Add(this.counterLabel);
        this.filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.filterPanel.Location = new System.Drawing.Point(0, 60);
        this.filterPanel.Name = "filterPanel";
        this.filterPanel.Size = new System.Drawing.Size(1200, 40);
        this.filterPanel.TabIndex = 5;
        //
        // counterLabel
        // El texto lo actualiza la lógica del contador «Mostrando X de Y».
        //
        this.counterLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.counterLabel.Location = new System.Drawing.Point(980, 9);
        this.counterLabel.Name = "counterLabel";
        this.counterLabel.Size = new System.Drawing.Size(210, 20);
        this.counterLabel.TabIndex = 0;
        this.counterLabel.Text = "Mostrando 0 de 0";
        this.counterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        //
        // remindersDataGridView
        // Tabla en solo lectura; el borde de selección sí está activo.
        //
        this.remindersDataGridView.AllowUserToAddRows = false;
        this.remindersDataGridView.AllowUserToDeleteRows = false;
        this.remindersDataGridView.AllowUserToResizeRows = false;
        this.remindersDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
        this.remindersDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.remindersDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
        this.subjectColumn,
        this.relatedColumn,
        this.remindAtColumn,
        this.statusColumn});
        this.remindersDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.remindersDataGridView.Location = new System.Drawing.Point(0, 100);
        this.remindersDataGridView.MultiSelect = false;
        this.remindersDataGridView.Name = "remindersDataGridView";
        this.remindersDataGridView.RowHeadersVisible = false;
        // Las celdas se ven siempre blancas: la selección no se resalta en azul.
        this.remindersDataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.White;
        this.remindersDataGridView.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;
        this.remindersDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.remindersDataGridView.Size = new System.Drawing.Size(1200, 590);
        this.remindersDataGridView.TabIndex = 6;
        this.remindersDataGridView.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.RemindersDataGridView_RowPostPaint);
        this.remindersDataGridView.SelectionChanged += new System.EventHandler(this.RemindersDataGridView_SelectionChanged);
        this.remindersDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.RemindersDataGridView_CellValueChanged);
        this.remindersDataGridView.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.RemindersDataGridView_RowsAdded);
        this.remindersDataGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.RemindersDataGridView_DataError);
        this.remindersDataGridView.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.RemindersDataGridView_CellPainting);
        //
        // subjectColumn
        //
        this.subjectColumn.HeaderText = "Asunto";
        this.subjectColumn.Name = "subjectColumn";
        //
        // relatedColumn
        // Desplegable con la unión de clientes y futuros clientes; se carga
        // al abrir el formulario (prefijo «(Cliente) …» / «(Futuro cliente) …»).
        //
        this.relatedColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
        this.relatedColumn.HeaderText = "Cliente/Futuro cliente";
        this.relatedColumn.Name = "relatedColumn";
        //
        // remindAtColumn
        // Columna de calendario con hora: al editar se despliega un control
        // combinado — calendario dd/MM/aaaa + selector de hora HH:mm con
        // flechas (specs/007). El ancho mínimo deja sitio a los dos selectores.
        //
        this.remindAtColumn.HeaderText = "Fecha y hora";
        this.remindAtColumn.MinimumWidth = 190;
        this.remindAtColumn.Name = "remindAtColumn";
        //
        // statusColumn
        //
        this.statusColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
        this.statusColumn.HeaderText = "Estado";
        this.statusColumn.Items.AddRange(new object[] {
        "Activo",
        "Descartado"});
        this.statusColumn.Name = "statusColumn";
        //
        // ReminderForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 650);
        this.Controls.Add(this.remindersDataGridView);
        this.Controls.Add(this.filterPanel);
        this.Controls.Add(this.buttonPanel);
        this.Name = "ReminderForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Recordatorios";
        this.buttonPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.remindersDataGridView)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel buttonPanel;
    private System.Windows.Forms.Button newButton;
    private System.Windows.Forms.Button modifyButton;
    private System.Windows.Forms.Button deleteButton;
    private System.Windows.Forms.Button saveButton;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Panel filterPanel;
    private System.Windows.Forms.Label counterLabel;
    private System.Windows.Forms.DataGridView remindersDataGridView;
    private System.Windows.Forms.DataGridViewTextBoxColumn subjectColumn;
    private System.Windows.Forms.DataGridViewComboBoxColumn relatedColumn;
    private Crm1.WindowsForms.Forms.CalendarColumn remindAtColumn;
    private System.Windows.Forms.DataGridViewComboBoxColumn statusColumn;
}
