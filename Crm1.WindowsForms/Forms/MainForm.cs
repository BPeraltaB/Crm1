namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Formulario principal de la aplicación.
/// </summary>
public partial class MainForm : System.Windows.Forms.Form
{
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
    /// Cierra la aplicación al pulsar el menú Salir.
    /// </summary>
    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
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
