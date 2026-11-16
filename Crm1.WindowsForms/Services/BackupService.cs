namespace Crm1.WindowsForms.Services;

/// <summary>
/// Copia de seguridad diaria de la base de datos (specs/008): antes de abrir
/// la aplicación se copia Data/crm1.db a Data/backup/crm1-AAAA-MM-DD.db, una
/// copia por día (la del día se sobreescribe) conservando las últimas
/// <see cref="KeepCount"/>. Un fallo se comunica al usuario, pero nunca
/// impide trabajar (constitución, punto 5: visible, nunca silencioso).
/// </summary>
public static class BackupService
{
    /// <summary>Copias que se conservan, contando la del día.</summary>
    public const int KeepCount = 7;

    /// <summary>
    /// Hace la copia del día si la base existe y retira las copias más
    /// antiguas dejando KeepCount. Una base inexistente (primera ejecución)
    /// no es un error: no hay nada que copiar.
    /// </summary>
    /// <returns>Verdadero si se ha creado (o refrescado) la copia del día.</returns>
    public static bool RunDailyBackup(string databasePath, string backupDirectory, DateTime today)
    {
        if (!File.Exists(databasePath))
        {
            return false;
        }

        Directory.CreateDirectory(backupDirectory);
        string backupPath = Path.Combine(backupDirectory, $"crm1-{today:yyyy-MM-dd}.db");

        try
        {
            File.Copy(databasePath, backupPath, overwrite: true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"No se ha podido copiar la base de datos a '{backupPath}'.", ex);
        }

        DeleteOldBackups(backupDirectory);
        return true;
    }

    // Retira las copias más antiguas dejando KeepCount: los nombres llevan la
    // fecha (crm1-AAAA-MM-DD.db), así que el orden alfabético es el cronológico.
    private static void DeleteOldBackups(string backupDirectory)
    {
        List<string> backups = Directory.GetFiles(backupDirectory, "crm1-*.db").OrderBy(name => name).ToList();
        int excess = backups.Count - KeepCount;
        for (int i = 0; i < excess; i++)
        {
            try
            {
                File.Delete(backups[i]);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"No se ha podido retirar la copia antigua '{backups[i]}'.", ex);
            }
        }
    }
}
