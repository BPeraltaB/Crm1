using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Formulario principal de la aplicación.
/// </summary>
public partial class MainForm : System.Windows.Forms.Form
{
    // Instancias únicas de los formularios de módulo embebidos en el área central.
    private ClientForm? clientForm;
    private ContactForm? contactForm;
    private SearchForm? searchForm;
    private TaskForm? taskForm;
    private ReminderForm? reminderForm;

    public MainForm()
    {
        // Inicializa los componentes definidos en el diseñador.
        InitializeComponent();

        // Aplica el tema oscuro al menú lateral.
        menuStrip.Renderer = new DarkMenuRenderer();

        // Muestra la fecha y hora iniciales en la barra de estado.
        UpdateClock();
    }

    /// <summary>
    /// Muestra el formulario de clientes embebido en el área central.
    /// </summary>
    private void ClientsMenuItem_Click(object? sender, EventArgs e)
    {
        // Crea el formulario de clientes una sola vez y atiende su botón
        // Tareas: abre el módulo de tareas filtrado al cliente marcado.
        if (clientForm is null)
        {
            clientForm = new ClientForm();
            clientForm.TasksRequested += (id, name) =>
                ShowFilteredTasks(Crm1.WindowsForms.Models.TaskRelatedType.Client, id, name);
        }

        ShowModule(clientForm);
    }

    /// <summary>
    /// Muestra el formulario de futuros clientes embebido en el área central.
    /// </summary>
    private void ProspectsMenuItem_Click(object? sender, EventArgs e)
    {
        // Crea el formulario una sola vez y atiende su botón Tareas: abre el
        // módulo de tareas filtrado al futuro cliente marcado.
        if (contactForm is null)
        {
            contactForm = new ContactForm();
            contactForm.TasksRequested += (id, name) =>
                ShowFilteredTasks(Crm1.WindowsForms.Models.TaskRelatedType.Prospect, id, name);
        }

        ShowModule(contactForm);
    }

    /// <summary>
    /// Muestra el formulario de tareas embebido en el área central. Desde el
    /// menú siempre se ven todas (sin filtro).
    /// </summary>
    private void TasksMenuItem_Click(object? sender, EventArgs e)
    {
        // Crea el formulario de tareas una sola vez.
        taskForm ??= new TaskForm();
        taskForm.ShowAllTasks();
        ShowModule(taskForm);
    }

    /// <summary>
    /// Muestra el formulario de recordatorios embebido en el área central.
    /// Por ahora el formulario está en blanco: los recordatorios pasados y
    /// futuros se listarán cuando el módulo tenga su especificación.
    /// </summary>
    private void RemindersMenuItem_Click(object? sender, EventArgs e)
    {
        // Crea el formulario de recordatorios una sola vez.
        reminderForm ??= new ReminderForm();
        ShowModule(reminderForm);
    }

    /// <summary>
    /// Muestra el módulo de Tareas filtrado al cliente o futuro cliente
    /// indicado (botón Tareas de ClientForm/ContactForm, specs/005 §7).
    /// </summary>
    private void ShowFilteredTasks(Crm1.WindowsForms.Models.TaskRelatedType relatedType, Guid relatedId, string displayName)
    {
        taskForm ??= new TaskForm();
        taskForm.ShowTasksOf(relatedType, relatedId, displayName);
        ShowModule(taskForm);
    }

    /// <summary>
    /// Muestra el formulario de búsqueda global embebido en el área central.
    /// </summary>
    private void SearchMenuItem_Click(object? sender, EventArgs e)
    {
        // Crea el formulario de búsqueda una sola vez y atiende su navegación.
        if (searchForm is null)
        {
            searchForm = new SearchForm();
            searchForm.NavigateRequested += SearchForm_NavigateRequested;
        }

        ShowModule(searchForm);
    }

    /// <summary>
    /// Muestra el módulo del resultado elegido y marca su registro. Si el
    /// módulo tiene una edición en curso o el registro no está en la tabla,
    /// se avisa y no se navega (specs/004).
    /// </summary>
    private void SearchForm_NavigateRequested(SearchModule module, Guid id)
    {
        if (module == SearchModule.Client)
        {
            clientForm ??= new ClientForm();
            if (clientForm.IsEditing)
            {
                WarnNavigationBlocked("Clientes", "Hay una edición en curso en Clientes.\n\nCancela o guarda los cambios antes de ir al registro encontrado.");
                return;
            }

            ShowModule(clientForm);
            if (!clientForm.SelectClientById(id))
            {
                WarnNavigationBlocked("Clientes", "El registro encontrado no está en la tabla de Clientes; puede haber cambios sin guardar.");
            }
        }
        else
        {
            contactForm ??= new ContactForm();
            if (contactForm.IsEditing)
            {
                WarnNavigationBlocked("Futuros clientes", "Hay una edición en curso en Futuros clientes.\n\nCancela o guarda los cambios antes de ir al registro encontrado.");
                return;
            }

            ShowModule(contactForm);
            if (!contactForm.SelectContactById(id))
            {
                WarnNavigationBlocked("Futuros clientes", "El registro encontrado no está en la tabla de Futuros clientes; puede haber cambios sin guardar.");
            }
        }
    }

    /// <summary>
    /// Avisa de que la navegación desde la búsqueda no ha podido completarse.
    /// </summary>
    private void WarnNavigationBlocked(string moduleName, string message)
    {
        MessageBox.Show(this,
            message,
            $"CRM1 — Búsqueda ({moduleName})",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Prepara un formulario de módulo para incrustarse (sin nivel superior,
    /// sin borde y ocupando todo el espacio) y lo muestra en el área central,
    /// reemplazando al módulo visible anteriormente.
    /// </summary>
    private void ShowModule(Form moduleForm)
    {
        moduleForm.TopLevel = false;
        moduleForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
        moduleForm.Dock = System.Windows.Forms.DockStyle.Fill;

        // Reemplaza el contenido del área central por el módulo solicitado.
        panelContent.Controls.Clear();
        panelContent.Controls.Add(moduleForm);
        moduleForm.Show();
    }

    /// <summary>
    /// Nada más mostrar la ventana, avisa de los recordatorios activos
    /// vencidos o de hoy (specs/007 §7); sin nada que avisar no muestra
    /// diálogo. La regla vive en ReminderService (constitución, punto 3).
    /// </summary>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ShowRemindersDueOnOpen();
    }

    /// <summary>
    /// Muestra el aviso de recordatorios de apertura: los Activos vencidos o
    /// de hoy, con su asunto, fecha y hora y registro vinculado. Solo
    /// informa: no descarta ni modifica nada.
    /// </summary>
    private void ShowRemindersDueOnOpen()
    {
        try
        {
            SqlDataStore store = new();
            List<Reminder> due = ReminderService.GetDueOnOpen(store.LoadReminders(), DateTime.Now);
            if (due.Count == 0)
            {
                return;
            }

            MessageBox.Show(this,
                $"Tienes {due.Count} recordatorio(s) vencidos o para hoy:\n\n" +
                BuildReminderLines(due, store.LoadClients(), store.LoadContacts()),
                "CRM1 — Recordatorios",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            // Fallo de lectura: error visible, la aplicación sigue
            // (constitución, punto 5).
            MessageBox.Show(this,
                "No se pudieron leer los recordatorios para el aviso de apertura.\n\n" + ex.Message,
                "CRM1 — Error de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Muestra el aviso de recordatorios de cierre: los Activos vencidos
    /// —incluidos los de hoy— y los del día siguiente. Solo informa y nunca
    /// bloquea la salida (specs/007 §7).
    /// </summary>
    private void ShowRemindersDueOnClose()
    {
        try
        {
            SqlDataStore store = new();
            List<Reminder> due = ReminderService.GetDueOnClose(store.LoadReminders(), DateTime.Now);
            if (due.Count == 0)
            {
                return;
            }

            MessageBox.Show(this,
                $"Antes de cerrar: {due.Count} recordatorio(s) vencidos, de hoy o de mañana:\n\n" +
                BuildReminderLines(due, store.LoadClients(), store.LoadContacts()),
                "CRM1 — Recordatorios",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            // Error visible y cierre igualmente: el aviso nunca bloquea.
            MessageBox.Show(this,
                "No se pudieron leer los recordatorios para el aviso de cierre.\n\n" + ex.Message,
                "CRM1 — Error de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Una línea por recordatorio: asunto entrecomillado, fecha y hora y el
    /// registro vinculado con el mismo prefijo que en la tabla del módulo.
    /// </summary>
    private static string BuildReminderLines(List<Reminder> reminders, List<Client> clients, List<Contact> contacts)
    {
        var names = new Dictionary<Guid, string>();
        foreach (Client client in clients)
        {
            names[client.Id] = $"(Cliente) {JoinDisplayName(client.FirstName, client.LastName1, client.LastName2)}";
        }
        foreach (Contact contact in contacts)
        {
            names[contact.Id] = $"(Futuro cliente) {JoinDisplayName(contact.FirstName, contact.LastName1, contact.LastName2)}";
        }

        return string.Join("\n", reminders.Select(reminder =>
            $"• \"{reminder.Subject}\" — {reminder.RemindAt:dd/MM/yyyy HH:mm} — " +
            (names.TryGetValue(reminder.RelatedId, out string? name) ? name : "—")));
    }

    // Nombre para mostrar: nombre y apellidos no vacíos (razón social en
    // empresas, apellidos ausentes en muchos futuros clientes).
    private static string JoinDisplayName(string firstName, string lastName1, string lastName2) =>
        string.Join(" ", new[] { firstName, lastName1, lastName2 }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>
    /// Cierra la aplicación al pulsar el menú Salir.
    /// </summary>
    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Pregunta antes de cerrar la aplicación si algún módulo embebido (Clientes,
    /// Futuros clientes, Tareas o Recordatorios) tiene cambios sin guardar
    /// (protección de specs/002, specs/003, specs/005 y specs/007).
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        // Módulos embebidos con cambios sin guardar.
        var unsavedModules = new List<string>();
        if (clientForm?.HasUnsavedChanges == true)
        {
            unsavedModules.Add("Clientes");
        }
        if (contactForm?.HasUnsavedChanges == true)
        {
            unsavedModules.Add("Futuros clientes");
        }
        if (taskForm?.HasUnsavedChanges == true)
        {
            unsavedModules.Add("Tareas");
        }
        if (reminderForm?.HasUnsavedChanges == true)
        {
            unsavedModules.Add("Recordatorios");
        }

        if (e.Cancel)
        {
            return;
        }

        if (unsavedModules.Count > 0)
        {
            DialogResult result = MessageBox.Show(this,
                $"Hay cambios sin guardar en {string.Join(" y ", unsavedModules)}. " +
                "¿Seguro que quieres salir sin guardar?\n\n" +
                "Si sales ahora, los archivos conservan los datos de la última vez que guardaste.",
                "CRM1 — Cambios sin guardar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
        }

        // Aviso informativo de recordatorios justo antes de cerrar (specs/007
        // §7), ya confirmada la salida: nunca bloquea el cierre.
        ShowRemindersDueOnClose();
    }

    /// <summary>
    /// Actualiza el reloj de la barra de estado en cada intervalo del temporizador.
    /// </summary>
    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
    }

    /// <summary>
    /// Actualiza la fecha y la hora mostradas en la barra de estado.
    /// </summary>
    private void UpdateClock()
    {
        statusLabelClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
    }

    /// <summary>
    /// Renderer con la paleta de colores del tema oscuro para el menú lateral.
    /// </summary>
    private sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        /// <summary>
        /// Inicializa el renderer con la tabla de colores del tema oscuro.
        /// </summary>
        public DarkMenuRenderer()
            : base(new DarkColorTable())
        {
        }

        /// <summary>
        /// Paleta de colores del menú lateral oscuro.
        /// </summary>
        private sealed class DarkColorTable : ProfessionalColorTable
        {
            // Colores del tema oscuro del menú lateral.
            private static readonly Color Background = Color.FromArgb(45, 55, 72);   // #2D3748
            private static readonly Color Highlight = Color.FromArgb(59, 74, 95);    // #3B4A5F
            private static readonly Color Separator = Color.FromArgb(74, 85, 105);   // #4A5568

            public override Color MenuStripGradientBegin => Background;

            public override Color MenuStripGradientEnd => Background;

            public override Color MenuItemSelected => Highlight;

            public override Color MenuItemSelectedGradientBegin => Highlight;

            public override Color MenuItemSelectedGradientEnd => Highlight;

            public override Color MenuItemBorder => Highlight;

            public override Color SeparatorDark => Separator;

            public override Color SeparatorLight => Separator;

            public override Color MenuBorder => Background;

            public override Color ToolStripDropDownBackground => Background;

            public override Color ImageMarginGradientBegin => Background;

            public override Color ImageMarginGradientMiddle => Background;

            public override Color ImageMarginGradientEnd => Background;
        }
    }
}
