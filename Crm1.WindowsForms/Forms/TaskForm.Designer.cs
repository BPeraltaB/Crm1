namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Código generado por el Diseñador de Windows Forms para el formulario de tareas.
/// </summary>
partial class TaskForm
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
        this.filterLabel = new System.Windows.Forms.Label();
        this.showAllButton = new System.Windows.Forms.Button();
        this.counterLabel = new System.Windows.Forms.Label();
        this.tasksDataGridView = new System.Windows.Forms.DataGridView();
        this.actionColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this.descriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.relatedColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this.dueDateColumn = new Crm1.WindowsForms.Forms.CalendarColumn();
        this.statusColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this.priorityColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
        this.buttonPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.tasksDataGridView)).BeginInit();
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
        // Solo diseño: sin evento Click hasta implementar la lógica.
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
        // filterPanel
        //
        this.filterPanel.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
        this.filterPanel.Controls.Add(this.filterLabel);
        this.filterPanel.Controls.Add(this.showAllButton);
        this.filterPanel.Controls.Add(this.counterLabel);
        this.filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.filterPanel.Location = new System.Drawing.Point(0, 60);
        this.filterPanel.Name = "filterPanel";
        this.filterPanel.Size = new System.Drawing.Size(1200, 40);
        this.filterPanel.TabIndex = 5;
        //
        // filterLabel
        //
        // Aviso «Tareas de: (Cliente) Ana»; visible solo con filtro activo.
        this.filterLabel.Location = new System.Drawing.Point(110, 9);
        this.filterLabel.Name = "filterLabel";
        this.filterLabel.Size = new System.Drawing.Size(500, 20);
        this.filterLabel.TabIndex = 2;
        this.filterLabel.Visible = false;
        //
        // showAllButton
        //
        // Quita el filtro y vuelve a mostrar todas las tareas.
        this.showAllButton.Location = new System.Drawing.Point(10, 6);
        this.showAllButton.Name = "showAllButton";
        this.showAllButton.Size = new System.Drawing.Size(90, 27);
        this.showAllButton.TabIndex = 1;
        this.showAllButton.Text = "Ver todas";
        this.showAllButton.Visible = false;
        this.showAllButton.Click += new System.EventHandler(this.ShowAllButton_Click);
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
        // tasksDataGridView
        // Tabla en solo lectura por ahora; el borde de selección sí está activo.
        //
        this.tasksDataGridView.AllowUserToAddRows = false;
        this.tasksDataGridView.AllowUserToDeleteRows = false;
        this.tasksDataGridView.AllowUserToResizeRows = false;
        this.tasksDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
        this.tasksDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.tasksDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
        this.actionColumn,
        this.descriptionColumn,
        this.relatedColumn,
        this.dueDateColumn,
        this.statusColumn,
        this.priorityColumn});
        this.tasksDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tasksDataGridView.Location = new System.Drawing.Point(0, 100);
        this.tasksDataGridView.MultiSelect = false;
        this.tasksDataGridView.Name = "tasksDataGridView";
        this.tasksDataGridView.RowHeadersVisible = false;
        // Las celdas se ven siempre blancas: la selección no se resalta en azul.
        this.tasksDataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.White;
        this.tasksDataGridView.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;
        this.tasksDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.tasksDataGridView.Size = new System.Drawing.Size(1200, 590);
        this.tasksDataGridView.TabIndex = 6;
        this.tasksDataGridView.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.TasksDataGridView_RowPostPaint);
        this.tasksDataGridView.SelectionChanged += new System.EventHandler(this.TasksDataGridView_SelectionChanged);
        this.tasksDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.TasksDataGridView_CellValueChanged);
        this.tasksDataGridView.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.TasksDataGridView_RowsAdded);
        this.tasksDataGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.TasksDataGridView_DataError);
        this.tasksDataGridView.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.TasksDataGridView_CellPainting);
        //
        // actionColumn
        // Desplegable con las acciones de la skill (specs/005, sección 3).
        //
        this.actionColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
        this.actionColumn.HeaderText = "Acción";
        this.actionColumn.Items.AddRange(new object[] {
        "Llamar",
        "Visitar",
        "Enviar correo",
        "Reunión",
        "Otro"});
        this.actionColumn.Name = "actionColumn";
        //
        // descriptionColumn
        //
        this.descriptionColumn.HeaderText = "Descripción";
        this.descriptionColumn.Name = "descriptionColumn";
        //
        // relatedColumn
        // Desplegable con la unión de clientes y futuros clientes; se carga
        // al abrir el formulario (prefijo «(Cliente) …» / «(Futuro cliente) …»).
        //
        this.relatedColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
        this.relatedColumn.HeaderText = "Cliente/Futuro cliente";
        this.relatedColumn.Name = "relatedColumn";
        //
        // dueDateColumn
        // Columna de calendario: al editar se despliega un DateTimePicker.
        //
        this.dueDateColumn.HeaderText = "Fecha límite";
        this.dueDateColumn.Name = "dueDateColumn";
        //
        // statusColumn
        //
        this.statusColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
        this.statusColumn.HeaderText = "Estado";
        this.statusColumn.Items.AddRange(new object[] {
        "Pendiente",
        "En curso",
        "Completada"});
        this.statusColumn.Name = "statusColumn";
        //
        // priorityColumn
        //
        this.priorityColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
        this.priorityColumn.HeaderText = "Prioridad";
        this.priorityColumn.Items.AddRange(new object[] {
        "Baja",
        "Media",
        "Alta"});
        this.priorityColumn.Name = "priorityColumn";
        //
        // TaskForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 650);
        this.Controls.Add(this.tasksDataGridView);
        this.Controls.Add(this.filterPanel);
        this.Controls.Add(this.buttonPanel);
        this.Name = "TaskForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Tareas";
        this.buttonPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.tasksDataGridView)).EndInit();
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
    private System.Windows.Forms.Label filterLabel;
    private System.Windows.Forms.Button showAllButton;
    private System.Windows.Forms.Label counterLabel;
    private System.Windows.Forms.DataGridView tasksDataGridView;
    private System.Windows.Forms.DataGridViewTextBoxColumn descriptionColumn;
    private System.Windows.Forms.DataGridViewComboBoxColumn relatedColumn;
    private Crm1.WindowsForms.Forms.CalendarColumn dueDateColumn;
    private System.Windows.Forms.DataGridViewComboBoxColumn statusColumn;
    private System.Windows.Forms.DataGridViewComboBoxColumn priorityColumn;
    private System.Windows.Forms.DataGridViewComboBoxColumn actionColumn;
}
