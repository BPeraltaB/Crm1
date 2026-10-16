using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;

namespace Crm1.WindowsForms.Forms
{
    // Formulario de gestión de clientes: lista, alta, edición, baja, búsqueda y filtro
    public class FormClients : Form
    {
        private readonly ClientService _service;

        private DataGridView _grid = null!;
        private TextBox _searchBox = null!;
        private ComboBox _statusFilter = null!;
        private Button _editButton = null!;
        private Button _deleteButton = null!;
        private Panel _editorPanel = null!;
        private TextBox _nameBox = null!;
        private TextBox _taxIdBox = null!;
        private TextBox _addressBox = null!;
        private TextBox _postalCodeBox = null!;
        private TextBox _cityBox = null!;
        private TextBox _provinceBox = null!;
        private TextBox _phoneBox = null!;
        private TextBox _emailBox = null!;
        private TextBox _notesBox = null!;
        private DateTimePicker _registrationPicker = null!;
        private ComboBox _statusBox = null!;

        private bool _editing;
        private Guid _editingId;

        public FormClients()
        {
            _service = new ClientService(new JsonDataStore());
            InitializeComponents();
            ReloadGrid();
        }

        private void InitializeComponents()
        {
            Text = "Gestión de clientes";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(1050, 620);
            MinimumSize = new System.Drawing.Size(900, 500);

            CreateToolbar();
            CreateGrid();
            CreateEditorPanel();

            ResumeLayout(false);
        }

        // Panel superior con búsqueda, filtro y acciones
        private void CreateToolbar()
        {
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10, 10, 10, 0)
            };

            var searchLabel = new Label
            {
                Text = "Buscar:",
                Location = new System.Drawing.Point(10, 14),
                AutoSize = true
            };

            _searchBox = new TextBox
            {
                Location = new System.Drawing.Point(65, 11),
                Size = new System.Drawing.Size(240, 25)
            };
            _searchBox.TextChanged += (s, e) => ReloadGrid();

            var filterLabel = new Label
            {
                Text = "Estado:",
                Location = new System.Drawing.Point(320, 14),
                AutoSize = true
            };

            _statusFilter = new ComboBox
            {
                Location = new System.Drawing.Point(372, 11),
                Size = new System.Drawing.Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _statusFilter.Items.AddRange(new object[] { "Todos", "Activo", "Inactivo" });
            _statusFilter.SelectedIndex = 0;
            _statusFilter.SelectedIndexChanged += (s, e) => ReloadGrid();

            var newButton = new Button
            {
                Text = "Nuevo",
                Location = new System.Drawing.Point(520, 10),
                Size = new System.Drawing.Size(85, 30),
                BackColor = System.Drawing.Color.FromArgb(59, 130, 246),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            newButton.FlatAppearance.BorderSize = 0;
            newButton.Click += (s, e) => StartNewClient();

            _editButton = new Button
            {
                Text = "Editar",
                Location = new System.Drawing.Point(615, 10),
                Size = new System.Drawing.Size(85, 30),
                BackColor = System.Drawing.Color.FromArgb(234, 179, 8),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _editButton.FlatAppearance.BorderSize = 0;
            _editButton.Click += (s, e) => StartEditClient();

            _deleteButton = new Button
            {
                Text = "Eliminar",
                Location = new System.Drawing.Point(710, 10),
                Size = new System.Drawing.Size(85, 30),
                BackColor = System.Drawing.Color.FromArgb(239, 68, 68),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _deleteButton.FlatAppearance.BorderSize = 0;
            _deleteButton.Click += (s, e) => DeleteSelectedClient();

            toolbar.Controls.AddRange(new Control[] { searchLabel, _searchBox, filterLabel, _statusFilter, newButton, _editButton, _deleteButton });
            Controls.Add(toolbar);
        }

        // Tabla de clientes con encabezados en español
        private void CreateGrid()
        {
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                BackgroundColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Nombre", DataPropertyName = "Name", FillWeight = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaxId", HeaderText = "CIF/NIF", DataPropertyName = "TaxId", FillWeight = 80 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "City", HeaderText = "Ciudad", DataPropertyName = "City", FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Province", HeaderText = "Provincia", DataPropertyName = "Province", FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Teléfono", DataPropertyName = "Phone", FillWeight = 80 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", DataPropertyName = "Email", FillWeight = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Estado", DataPropertyName = "Status", FillWeight = 60 });

            // Traduce el estado a texto en español para mostrarlo
            _grid.CellFormatting += (s, e) =>
            {
                if (_grid.Columns[e.ColumnIndex].Name == "Status" && e.Value is ClientStatus status)
                    e.Value = status == ClientStatus.Active ? "Activo" : "Inactivo";
            };

            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    StartEditClient();
            };

            Controls.Add(_grid);
            _grid.BringToFront();
        }

        // Panel de alta/edición (oculto por defecto)
        private void CreateEditorPanel()
        {
            _editorPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = System.Drawing.Color.White,
                Padding = new Padding(20)
            };

            var title = new Label
            {
                Text = "Datos del cliente",
                Location = new System.Drawing.Point(20, 15),
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                AutoSize = true
            };
            _editorPanel.Controls.Add(title);

            int y = 60;
            const int spacing = 38;
            const int labelX = 20;
            const int inputX = 150;

            AddEditorField("Nombre *:", ref y, spacing, labelX, inputX, out _nameBox, 300);
            AddEditorField("CIF/NIF:", ref y, spacing, labelX, inputX, out _taxIdBox, 200);
            AddEditorField("Dirección:", ref y, spacing, labelX, inputX, out _addressBox, 400);
            AddEditorField("Cód. Postal:", ref y, spacing, labelX, inputX, out _postalCodeBox, 100);
            AddEditorField("Ciudad:", ref y, spacing, labelX, inputX, out _cityBox, 200);
            AddEditorField("Provincia:", ref y, spacing, labelX, inputX, out _provinceBox, 200);
            AddEditorField("Teléfono:", ref y, spacing, labelX, inputX, out _phoneBox, 200);
            AddEditorField("Email:", ref y, spacing, labelX, inputX, out _emailBox, 300);

            // Fecha de alta
            var registrationLabel = new Label { Text = "Fecha de alta:", Location = new System.Drawing.Point(labelX, y + 3), AutoSize = true };
            _registrationPicker = new DateTimePicker { Location = new System.Drawing.Point(inputX, y), Size = new System.Drawing.Size(200, 25), Format = DateTimePickerFormat.Short };
            _editorPanel.Controls.Add(registrationLabel);
            _editorPanel.Controls.Add(_registrationPicker);
            y += spacing;

            // Estado
            var statusLabel = new Label { Text = "Estado:", Location = new System.Drawing.Point(labelX, y + 3), AutoSize = true };
            _statusBox = new ComboBox
            {
                Location = new System.Drawing.Point(inputX, y),
                Size = new System.Drawing.Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _statusBox.Items.AddRange(new object[] { "Activo", "Inactivo" });
            _statusBox.SelectedIndex = 0;
            _editorPanel.Controls.Add(statusLabel);
            _editorPanel.Controls.Add(_statusBox);
            y += spacing + 10;

            // Observaciones
            var notesLabel = new Label { Text = "Observaciones:", Location = new System.Drawing.Point(labelX, y + 3), AutoSize = true };
            _notesBox = new TextBox
            {
                Location = new System.Drawing.Point(inputX, y),
                Size = new System.Drawing.Size(400, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            _editorPanel.Controls.Add(notesLabel);
            _editorPanel.Controls.Add(_notesBox);
            y += 100;

            // Botones de guardado y cancelación
            var saveButton = new Button
            {
                Text = "Guardar",
                Location = new System.Drawing.Point(inputX, y),
                Size = new System.Drawing.Size(100, 35),
                BackColor = System.Drawing.Color.FromArgb(34, 197, 94),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Click += (s, e) => SaveClient();

            var cancelButton = new Button
            {
                Text = "Cancelar",
                Location = new System.Drawing.Point(inputX + 110, y),
                Size = new System.Drawing.Size(100, 35),
                BackColor = System.Drawing.Color.FromArgb(156, 163, 175),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) => CloseEditor();

            _editorPanel.Controls.Add(saveButton);
            _editorPanel.Controls.Add(cancelButton);

            Controls.Add(_editorPanel);
            _editorPanel.BringToFront();
        }

        // Añade una etiqueta y su cuadro de texto al panel de edición
        private void AddEditorField(string label, ref int y, int spacing, int labelX, int inputX, out TextBox box, int width)
        {
            var tag = new Label { Text = label, Location = new System.Drawing.Point(labelX, y + 3), AutoSize = true };
            box = new TextBox { Location = new System.Drawing.Point(inputX, y), Size = new System.Drawing.Size(width, 25) };
            _editorPanel.Controls.Add(tag);
            _editorPanel.Controls.Add(box);
            y += spacing;
        }

        // Recarga la tabla aplicando búsqueda y filtro de estado
        private void ReloadGrid()
        {
            ClientStatus? status = _statusFilter.SelectedIndex switch
            {
                1 => ClientStatus.Active,
                2 => ClientStatus.Inactive,
                _ => null
            };

            List<Client> clients = _service.Search(_searchBox.Text, status);
            _grid.DataSource = clients;
        }

        // Prepara el formulario para dar de alta un cliente
        private void StartNewClient()
        {
            _editing = false;
            _editingId = Guid.Empty;
            ClearEditor();
            ShowEditor();
            _nameBox.Focus();
        }

        // Carga el cliente seleccionado en el panel de edición
        private void StartEditClient()
        {
            if (_grid.CurrentRow == null)
            {
                MessageBox.Show("Seleccione un cliente para editar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = (Guid)_grid.CurrentRow.Cells["Id"].Value;
            Client? client = _service.GetAll().Find(c => c.Id == id);
            if (client == null)
                return;

            _editing = true;
            _editingId = client.Id;

            _nameBox.Text = client.Name;
            _taxIdBox.Text = client.TaxId;
            _addressBox.Text = client.Address;
            _postalCodeBox.Text = client.PostalCode;
            _cityBox.Text = client.City;
            _provinceBox.Text = client.Province;
            _phoneBox.Text = client.Phone;
            _emailBox.Text = client.Email;
            _registrationPicker.Value = client.RegistrationDate;
            _statusBox.SelectedIndex = client.Status == ClientStatus.Active ? 0 : 1;
            _notesBox.Text = client.Notes;

            ShowEditor();
            _nameBox.Focus();
        }

        // Guarda el cliente del panel de edición (alta o edición)
        private void SaveClient()
        {
            Client client = BuildClientFromEditor();

            ValidationResult result = _editing ? _service.Update(client) : _service.Add(client);
            if (!result.IsValid)
            {
                MessageBox.Show(string.Join("\n", result.Errors), "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Cliente guardado correctamente.", "CRM1", MessageBoxButtons.OK, MessageBoxIcon.Information);
            CloseEditor();
            ReloadGrid();
        }

        // Elimina el cliente seleccionado tras pedir confirmación
        private void DeleteSelectedClient()
        {
            if (_grid.CurrentRow == null)
            {
                MessageBox.Show("Seleccione un cliente para eliminar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = (Guid)_grid.CurrentRow.Cells["Id"].Value;
            Client? client = _service.GetAll().Find(c => c.Id == id);
            if (client == null)
                return;

            DialogResult confirmation = MessageBox.Show(
                $"¿Está seguro de eliminar al cliente '{client.Name}'?",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
                return;

            if (_service.Delete(id))
            {
                MessageBox.Show("Cliente eliminado correctamente.", "CRM1", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReloadGrid();
            }
            else
            {
                MessageBox.Show("No se encontró el cliente a eliminar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Construye la entidad a partir de los datos del panel de edición
        private Client BuildClientFromEditor()
        {
            return new Client
            {
                Id = _editing ? _editingId : Guid.NewGuid(),
                Name = _nameBox.Text.Trim(),
                TaxId = _taxIdBox.Text.Trim(),
                Address = _addressBox.Text.Trim(),
                PostalCode = _postalCodeBox.Text.Trim(),
                City = _cityBox.Text.Trim(),
                Province = _provinceBox.Text.Trim(),
                Phone = _phoneBox.Text.Trim(),
                Email = _emailBox.Text.Trim(),
                RegistrationDate = _registrationPicker.Value,
                Notes = _notesBox.Text.Trim(),
                Status = _statusBox.SelectedIndex == 0 ? ClientStatus.Active : ClientStatus.Inactive
            };
        }

        // Muestra el panel de edición y oculta la tabla
        private void ShowEditor()
        {
            _grid.Visible = false;
            _editorPanel.Visible = true;
        }

        // Oculta el panel de edición y vuelve a la tabla
        private void CloseEditor()
        {
            _editorPanel.Visible = false;
            _grid.Visible = true;
            _editing = false;
            _editingId = Guid.Empty;
            ClearEditor();
        }

        // Vacía los campos del panel de edición
        private void ClearEditor()
        {
            _nameBox.Text = string.Empty;
            _taxIdBox.Text = string.Empty;
            _addressBox.Text = string.Empty;
            _postalCodeBox.Text = string.Empty;
            _cityBox.Text = string.Empty;
            _provinceBox.Text = string.Empty;
            _phoneBox.Text = string.Empty;
            _emailBox.Text = string.Empty;
            _notesBox.Text = string.Empty;
            _registrationPicker.Value = DateTime.Now;
            _statusBox.SelectedIndex = 0;
        }
    }
}
