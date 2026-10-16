namespace Crm1.WindowsForms;

static class Program
{
    /// <summary>
    ///  Punto de entrada de la aplicación.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Configuración de DPI alto y fuente predeterminada
        ApplicationConfiguration.Initialize();
        Application.Run(new FormMain());
    }    
}