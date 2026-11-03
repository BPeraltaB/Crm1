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
        // Crea el formulario de clientes una sola vez.
        clientForm ??= new ClientForm();
        ShowModule(clientForm);
    }

    /// <summary>
    /// Muestra el formulario de futuros clientes embebido en el área central.
    /// </summary>
    private void ProspectsMenuItem_Click(object? sender, EventArgs e)
    {
        // Crea el formulario de futuros clientes una sola vez.
        contactForm ??= new ContactForm();
        ShowModule(contactForm);
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
    /// Cierra la aplicación al pulsar el menú Salir.
    /// </summary>
    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Pregunta antes de cerrar la aplicación si algún módulo embebido (Clientes
    /// o Futuros clientes) tiene cambios sin guardar (protección de specs/002
    /// y specs/003, sección de cierre).
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

        if (e.Cancel || unsavedModules.Count == 0)
        {
            return;
        }

        DialogResult result = MessageBox.Show(this,
            $"Hay cambios sin guardar en {string.Join(" y ", unsavedModules)}. " +
            "¿Seguro que quieres salir sin guardar?\n\n" +
            "Si sales ahora, los archivos conservan los datos de la última vez que guardaste.",
            "CRM1 — Cambios sin guardar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (result == DialogResult.No)
        {
            e.Cancel = true;
        }
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
