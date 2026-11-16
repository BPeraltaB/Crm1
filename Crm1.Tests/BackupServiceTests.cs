using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de la copia de seguridad diaria de la base de datos (specs/008):
/// copia con nombre del día, sin base no hay copia (ni error), la copia del
/// día se refresca y la rotación conserva solo las últimas 7.
/// </summary>
public sealed class BackupServiceTests : IDisposable
{
    private readonly string _directory;

    public BackupServiceTests()
    {
        _directory = Path.Combine(
            Path.GetTempPath(), "opencode", "crm1-tests", $"backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string DatabasePath => Path.Combine(_directory, "crm1.db");

    private string BackupDirectory => Path.Combine(_directory, "backup");

    private void WriteDatabase()
    {
        File.WriteAllText(DatabasePath, "base de prueba");
    }

    [Fact]
    public void Creates_todays_backup_when_the_database_exists()
    {
        WriteDatabase();

        bool created = BackupService.RunDailyBackup(DatabasePath, BackupDirectory, new DateTime(2026, 11, 16));

        Assert.True(created);
        Assert.True(File.Exists(Path.Combine(BackupDirectory, "crm1-2026-11-16.db")));
        Assert.Equal("base de prueba", File.ReadAllText(Path.Combine(BackupDirectory, "crm1-2026-11-16.db")));
    }

    [Fact]
    public void Without_database_nothing_is_copied_and_nothing_fails()
    {
        // Primera ejecución: sin base no hay copia (no es un error).
        bool created = BackupService.RunDailyBackup(DatabasePath, BackupDirectory, new DateTime(2026, 11, 16));

        Assert.False(created);
        Assert.False(Directory.Exists(BackupDirectory));
    }

    [Fact]
    public void Running_twice_the_same_day_refreshes_the_single_copy()
    {
        WriteDatabase();
        DateTime today = new(2026, 11, 16);

        BackupService.RunDailyBackup(DatabasePath, BackupDirectory, today);
        File.WriteAllText(DatabasePath, "base modificada");
        BackupService.RunDailyBackup(DatabasePath, BackupDirectory, today);

        string[] backups = Directory.GetFiles(BackupDirectory, "crm1-*.db");
        Assert.Single(backups);
        Assert.Equal("base modificada", File.ReadAllText(backups[0]));
    }

    [Fact]
    public void Rotation_keeps_only_the_last_seven_copies()
    {
        WriteDatabase();

        // Seis días de copias previas (del 10 al 15) y la de hoy (16).
        for (int day = 10; day <= 15; day++)
        {
            BackupService.RunDailyBackup(DatabasePath, BackupDirectory, new DateTime(2026, 11, day));
        }
        BackupService.RunDailyBackup(DatabasePath, BackupDirectory, new DateTime(2026, 11, 16));

        // Día 17: al hacer la copia nueva, la del 10 se retira (7 en total).
        BackupService.RunDailyBackup(DatabasePath, BackupDirectory, new DateTime(2026, 11, 17));

        string[] backups = Directory.GetFiles(BackupDirectory, "crm1-*.db").OrderBy(name => name).ToArray();
        Assert.Equal(BackupService.KeepCount, backups.Length);
        Assert.DoesNotContain(backups, name => name.Contains("2026-11-10"));
        Assert.Contains(backups, name => name.Contains("2026-11-11"));
        Assert.Contains(backups, name => name.Contains("2026-11-17"));
    }

    [Fact]
    public void Unrelated_files_in_the_backup_folder_are_untouched()
    {
        WriteDatabase();
        Directory.CreateDirectory(BackupDirectory);
        string unrelated = Path.Combine(BackupDirectory, "notas.txt");
        File.WriteAllText(unrelated, "no es una copia");

        BackupService.RunDailyBackup(DatabasePath, BackupDirectory, new DateTime(2026, 11, 16));

        Assert.True(File.Exists(unrelated));
        Assert.Single(Directory.GetFiles(BackupDirectory, "crm1-*.db"));
    }
}
