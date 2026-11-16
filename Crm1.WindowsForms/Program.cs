using Crm1.WindowsForms.Services;

namespace Crm1.WindowsForms;

static class Program
{
    /// <summary>
    ///  Punto de entrada de la aplicación.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Copia de seguridad diaria de la base (specs/008), antes de abrirla:
        // un fallo se comunica pero no impide trabajar (constitución, punto 5).
        try
        {
            BackupService.RunDailyBackup(
                Path.Combine(AppContext.BaseDirectory, "Data", "crm1.db"),
                Path.Combine(AppContext.BaseDirectory, "Data", "backup"),
                DateTime.Today);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se ha podido hacer la copia de seguridad diaria de la base de datos.\n\n" +
                ex.Message,
                "CRM1 — Copia de seguridad",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        Application.Run(new Forms.MainForm());
    }
}
