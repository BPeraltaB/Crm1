using System.Globalization;
using Crm1.WindowsForms.Models;
using Microsoft.Data.Sqlite;
using Task = Crm1.WindowsForms.Models.Task;
using TaskStatus = Crm1.WindowsForms.Models.TaskStatus;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Almacenamiento SQL local (SQLite): base de datos Data/crm1.db junto al
/// ejecutable, con tablas Clients, Contacts, Tasks y Reminders independientes
/// (specs/006, specs/005 y specs/007). Al abrirse por primera vez migra los
/// antiguos JSON (una sola vez, sin borrarlos) y queda como almacén activo de
/// la aplicación.
/// </summary>
public sealed class SqlDataStore : IDataStore
{
    // Marca de la migración única desde los JSON (specs/006, sección 4).
    private const string MigrationKey = "json_migration";

    private readonly string _databasePath;
    private readonly JsonDataStore _legacyJsonStore;

    /// <summary>
    /// Crea el almacén. Sin ruta explícita usa Data/crm1.db junto al ejecutable
    /// y migra (si hace falta) los JSON por defecto de Data/.
    /// </summary>
    public SqlDataStore(string? databasePath = null, JsonDataStore? legacyJsonStore = null)
    {
        _databasePath = databasePath ?? Path.Combine(AppContext.BaseDirectory, "Data", "crm1.db");
        _legacyJsonStore = legacyJsonStore ?? new JsonDataStore();

        // Crea la base (con su esquema) ya: la primera apertura migra los JSON.
        Execute<object?>(connection =>
        {
            EnsureSchemaAndMigrate(connection);
            return null;
        });
    }

    /// <summary>
    /// Ruta del archivo de base de datos.
    /// </summary>
    public string DatabasePath => _databasePath;

    /// <inheritdoc/>
    public List<Client> LoadClients() => Execute(connection =>
    {
        EnsureSchemaAndMigrate(connection);
        return LoadTable(connection, "Clients", MapClient);
    });

    /// <inheritdoc/>
    public List<Contact> LoadContacts() => Execute(connection =>
    {
        EnsureSchemaAndMigrate(connection);
        return LoadTable(connection, "Contacts", MapContact);
    });

    /// <inheritdoc/>
    public void SaveClients(List<Client> clients) => Execute<object?>(connection =>
    {
        EnsureSchemaAndMigrate(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            ExecuteNonQuery(connection, transaction, "DELETE FROM Clients");
            foreach (Client client in clients)
            {
                InsertRow(connection, transaction, "Clients", client.Id, client.FirstName, client.LastName1,
                    client.LastName2, client.TaxId, client.Phone, client.Email, client.Address, client.PostalCode,
                    client.City, client.Province, client.Notes, client.RegistrationDate,
                    client.Status == ClientStatus.Inactive ? nameof(ClientStatus.Inactive) : nameof(ClientStatus.Active));
            }
            transaction.Commit();
            return null;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    });

    /// <inheritdoc/>
    public void SaveContacts(List<Contact> contacts) => Execute<object?>(connection =>
    {
        EnsureSchemaAndMigrate(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            ExecuteNonQuery(connection, transaction, "DELETE FROM Contacts");
            foreach (Contact contact in contacts)
            {
                InsertRow(connection, transaction, "Contacts", contact.Id, contact.FirstName, contact.LastName1,
                    contact.LastName2, contact.TaxId, contact.Phone, contact.Email, contact.Address, contact.PostalCode,
                    contact.City, contact.Province, contact.Notes, contact.RegistrationDate,
                    contact.Status == ContactStatus.Discarded ? nameof(ContactStatus.Discarded) : nameof(ContactStatus.Pending));
            }
            transaction.Commit();
            return null;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    });

    // Crea las tablas si no existen y ejecuta la migración única de los JSON.
    // Se llama en cada operación: si el archivo se renueva en disco, cualquier
    // instancia antigua vuelve a dejarlo listo en vez de fallar.
    private void EnsureSchemaAndMigrate(SqliteConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
                command.CommandText = """
                    CREATE TABLE IF NOT EXISTS Clients (
                        Id TEXT PRIMARY KEY,
                        FirstName TEXT NOT NULL,
                        LastName1 TEXT NOT NULL,
                        LastName2 TEXT NOT NULL,
                        TaxId TEXT NOT NULL,
                        Phone TEXT NOT NULL,
                        Email TEXT NOT NULL,
                        Address TEXT NOT NULL,
                        PostalCode TEXT NOT NULL,
                        City TEXT NOT NULL,
                        Province TEXT NOT NULL,
                        Notes TEXT NOT NULL,
                        RegistrationDate TEXT NOT NULL,
                        Status TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Contacts (
                        Id TEXT PRIMARY KEY,
                        FirstName TEXT NOT NULL,
                        LastName1 TEXT NOT NULL,
                        LastName2 TEXT NOT NULL,
                        TaxId TEXT NOT NULL,
                        Phone TEXT NOT NULL,
                        Email TEXT NOT NULL,
                        Address TEXT NOT NULL,
                        PostalCode TEXT NOT NULL,
                        City TEXT NOT NULL,
                        Province TEXT NOT NULL,
                        Notes TEXT NOT NULL,
                        RegistrationDate TEXT NOT NULL,
                        Status TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Tasks (
                        Id TEXT PRIMARY KEY,
                        Action TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        DueDate TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        Priority TEXT NOT NULL,
                        RelatedId TEXT NOT NULL,
                        RelatedType TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Reminders (
                        Id TEXT PRIMARY KEY,
                        Subject TEXT NOT NULL,
                        RemindAt TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        RelatedId TEXT NOT NULL,
                        RelatedType TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS SchemaInfo (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL
                    );
                    """;
                command.ExecuteNonQuery();
        }

        // Migración de esquema de tareas: la columna Title pasó a llamarse
        // Action (specs/005). Si existe la antigua, se renombra sin perder
        // datos; en bases nuevas el CREATE TABLE ya usa el nombre definitivo.
        if (HasColumn(connection, null, "Tasks", "Title"))
        {
            ExecuteNonQuery(connection, null, "ALTER TABLE Tasks RENAME COLUMN Title TO Action");
        }

            // Migración única: si no está marcada, se importan los JSON en una
            // transacción y se asienta la marca (idempotente). Los JSON no se
            // borran nunca (specs/006, sección 4).
            if (HasSchemaValue(connection, null, MigrationKey))
            {
                return;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                List<Client> clients = _legacyJsonStore.LoadClients();
                ExecuteNonQuery(connection, transaction, "DELETE FROM Clients");
                foreach (Client client in clients)
                {
                    InsertRow(connection, transaction, "Clients", client.Id, client.FirstName, client.LastName1,
                        client.LastName2, client.TaxId, client.Phone, client.Email, client.Address, client.PostalCode,
                        client.City, client.Province, client.Notes, client.RegistrationDate,
                        client.Status == ClientStatus.Inactive ? nameof(ClientStatus.Inactive) : nameof(ClientStatus.Active));
                }

                List<Contact> contacts = _legacyJsonStore.LoadContacts();
                ExecuteNonQuery(connection, transaction, "DELETE FROM Contacts");
                foreach (Contact contact in contacts)
                {
                    InsertRow(connection, transaction, "Contacts", contact.Id, contact.FirstName, contact.LastName1,
                        contact.LastName2, contact.TaxId, contact.Phone, contact.Email, contact.Address, contact.PostalCode,
                        contact.City, contact.Province, contact.Notes, contact.RegistrationDate,
                        contact.Status == ContactStatus.Discarded ? nameof(ContactStatus.Discarded) : nameof(ContactStatus.Pending));
                }

                SetSchemaValue(connection, transaction, MigrationKey, "done");
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
    }

    /// <summary>
    /// Carga las tareas guardadas (specs/005). Una base sin tareas devuelve
    /// una lista vacía (primera ejecución); una base ilegible lanza una
    /// excepción visible (constitución, punto 5).
    /// </summary>
    public List<Task> LoadTasks() => Execute(connection =>
    {
        EnsureSchemaAndMigrate(connection);
        var tasks = new List<Task>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Action, Description, DueDate, Status, Priority, RelatedId, RelatedType FROM Tasks";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            tasks.Add(MapTask(reader));
        }
        return tasks;
    });

    /// <summary>
    /// Guarda las tareas reemplazando el contenido de la tabla Tasks en una
    /// transacción (borrado e inserciones): un fallo no deja cambios a medias
    /// (specs/005 y specs/006). La tabla es independiente de Clients y Contacts.
    /// </summary>
    public void SaveTasks(List<Task> tasks) => Execute<object?>(connection =>
    {
        EnsureSchemaAndMigrate(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            ExecuteNonQuery(connection, transaction, "DELETE FROM Tasks");
            foreach (Task task in tasks)
            {
                InsertTaskRow(connection, transaction, task);
            }
            transaction.Commit();
            return null;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    });

    /// <summary>
    /// Carga los recordatorios guardados (specs/007). Una base sin
    /// recordatorios devuelve una lista vacía; una base ilegible lanza una
    /// excepción visible (constitución, punto 5).
    /// </summary>
    public List<Reminder> LoadReminders() => Execute(connection =>
    {
        EnsureSchemaAndMigrate(connection);
        var reminders = new List<Reminder>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Subject, RemindAt, Status, RelatedId, RelatedType FROM Reminders";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            reminders.Add(MapReminder(reader));
        }
        return reminders;
    });

    /// <summary>
    /// Guarda los recordatorios reemplazando el contenido de la tabla
    /// Reminders en una transacción (specs/007 y specs/006). La tabla es
    /// independiente de las demás.
    /// </summary>
    public void SaveReminders(List<Reminder> reminders) => Execute<object?>(connection =>
    {
        EnsureSchemaAndMigrate(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            ExecuteNonQuery(connection, transaction, "DELETE FROM Reminders");
            foreach (Reminder reminder in reminders)
            {
                InsertReminderRow(connection, transaction, reminder);
            }
            transaction.Commit();
            return null;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    });

    // Abre una conexión (creando antes la carpeta) y ejecuta la acción. Cualquier
    // fallo se convierte en un error visible (constitución, punto 5).
    private T Execute<T>(Func<SqliteConnection, T> action)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Sin agrupación de conexiones: el archivo no queda bloqueado.
            using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
            connection.Open();
            return action(connection);
        }
        catch (InvalidOperationException)
        {
            // Errores ya visibles (p. ej. un JSON ilegible durante la migración).
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo acceder a la base de datos '{_databasePath}'.", ex);
        }
    }

    // Carga todas las filas de una tabla y las convierte con el mapeador.
    private static List<T> LoadTable<T>(SqliteConnection connection, string table, Func<SqliteDataReader, T> map)
    {
        var items = new List<T>();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, FirstName, LastName1, LastName2, TaxId, Phone, Email, Address, PostalCode, City, Province, Notes, RegistrationDate, Status FROM " + table;

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(map(reader));
        }
        return items;
    }

    private static Client MapClient(SqliteDataReader reader) => new()
    {
        Id = Guid.Parse(Text(reader, 0)),
        FirstName = Text(reader, 1),
        LastName1 = Text(reader, 2),
        LastName2 = Text(reader, 3),
        TaxId = Text(reader, 4),
        Phone = Text(reader, 5),
        Email = Text(reader, 6),
        Address = Text(reader, 7),
        PostalCode = Text(reader, 8),
        City = Text(reader, 9),
        Province = Text(reader, 10),
        Notes = Text(reader, 11),
        RegistrationDate = ParseDate(Text(reader, 12)),
        Status = Text(reader, 13) == nameof(ClientStatus.Inactive) ? ClientStatus.Inactive : ClientStatus.Active
    };

    private static Contact MapContact(SqliteDataReader reader) => new()
    {
        Id = Guid.Parse(Text(reader, 0)),
        FirstName = Text(reader, 1),
        LastName1 = Text(reader, 2),
        LastName2 = Text(reader, 3),
        TaxId = Text(reader, 4),
        Phone = Text(reader, 5),
        Email = Text(reader, 6),
        Address = Text(reader, 7),
        PostalCode = Text(reader, 8),
        City = Text(reader, 9),
        Province = Text(reader, 10),
        Notes = Text(reader, 11),
        RegistrationDate = ParseDate(Text(reader, 12)),
        Status = Text(reader, 13) == nameof(ContactStatus.Discarded) ? ContactStatus.Discarded : ContactStatus.Pending
    };

    // Convierte una fila de la tabla Tasks (specs/005): los enums viajan como
    // los nombres de sus valores, escritos siempre por esta misma clase.
    private static Task MapTask(SqliteDataReader reader) => new()
    {
        Id = Guid.Parse(Text(reader, 0)),
        Action = Text(reader, 1),
        Description = Text(reader, 2),
        DueDate = ParseDate(Text(reader, 3)),
        Status = Enum.Parse<TaskStatus>(Text(reader, 4)),
        Priority = Enum.Parse<TaskPriority>(Text(reader, 5)),
        RelatedId = Guid.Parse(Text(reader, 6)),
        RelatedType = Enum.Parse<TaskRelatedType>(Text(reader, 7))
    };

    // Convierte una fila de la tabla Reminders (specs/007): los enums viajan
    // como los nombres de sus valores, escritos siempre por esta misma clase.
    private static Reminder MapReminder(SqliteDataReader reader) => new()
    {
        Id = Guid.Parse(Text(reader, 0)),
        Subject = Text(reader, 1),
        RemindAt = ParseDateTime(Text(reader, 2)),
        Status = Enum.Parse<ReminderStatus>(Text(reader, 3)),
        RelatedId = Guid.Parse(Text(reader, 4)),
        RelatedType = Enum.Parse<TaskRelatedType>(Text(reader, 5))
    };

    // Inserta una fila parametrizada en la tabla indicada.
    private static void InsertRow(SqliteConnection connection, SqliteTransaction transaction, string table,
        Guid id, string firstName, string lastName1, string lastName2, string taxId, string phone, string email,
        string address, string postalCode, string city, string province, string notes, DateTime registrationDate,
        string status)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO " + table + " (Id, FirstName, LastName1, LastName2, TaxId, Phone, Email, Address, PostalCode, City, Province, Notes, RegistrationDate, Status) " +
            "VALUES (@id, @firstName, @lastName1, @lastName2, @taxId, @phone, @email, @address, @postalCode, @city, @province, @notes, @registrationDate, @status)";
        command.Parameters.AddWithValue("@id", id.ToString());
        command.Parameters.AddWithValue("@firstName", firstName ?? string.Empty);
        command.Parameters.AddWithValue("@lastName1", lastName1 ?? string.Empty);
        command.Parameters.AddWithValue("@lastName2", lastName2 ?? string.Empty);
        command.Parameters.AddWithValue("@taxId", taxId ?? string.Empty);
        command.Parameters.AddWithValue("@phone", phone ?? string.Empty);
        command.Parameters.AddWithValue("@email", email ?? string.Empty);
        command.Parameters.AddWithValue("@address", address ?? string.Empty);
        command.Parameters.AddWithValue("@postalCode", postalCode ?? string.Empty);
        command.Parameters.AddWithValue("@city", city ?? string.Empty);
        command.Parameters.AddWithValue("@province", province ?? string.Empty);
        command.Parameters.AddWithValue("@notes", notes ?? string.Empty);
        command.Parameters.AddWithValue("@registrationDate", FormatDate(registrationDate));
        command.Parameters.AddWithValue("@status", status);
        command.ExecuteNonQuery();
    }

    // Inserta una fila parametrizada de la tabla Tasks (specs/005). Los enums
    // se guardan como el nombre de su valor (ToString), igual que en Clients
    // y Contacts.
    private static void InsertTaskRow(SqliteConnection connection, SqliteTransaction transaction, Task task)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO Tasks (Id, Action, Description, DueDate, Status, Priority, RelatedId, RelatedType) " +
            "VALUES (@id, @action, @description, @dueDate, @status, @priority, @relatedId, @relatedType)";
        command.Parameters.AddWithValue("@id", task.Id.ToString());
        command.Parameters.AddWithValue("@action", task.Action ?? string.Empty);
        command.Parameters.AddWithValue("@description", task.Description ?? string.Empty);
        command.Parameters.AddWithValue("@dueDate", FormatDate(task.DueDate));
        command.Parameters.AddWithValue("@status", task.Status.ToString());
        command.Parameters.AddWithValue("@priority", task.Priority.ToString());
        command.Parameters.AddWithValue("@relatedId", task.RelatedId.ToString());
        command.Parameters.AddWithValue("@relatedType", task.RelatedType.ToString());
        command.ExecuteNonQuery();
    }

    // Inserta una fila parametrizada de la tabla Reminders (specs/007). Los
    // enums se guardan como el nombre de su valor, igual que en las demás
    // tablas; la fecha y hora viaja en ISO con la hora.
    private static void InsertReminderRow(SqliteConnection connection, SqliteTransaction transaction, Reminder reminder)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO Reminders (Id, Subject, RemindAt, Status, RelatedId, RelatedType) " +
            "VALUES (@id, @subject, @remindAt, @status, @relatedId, @relatedType)";
        command.Parameters.AddWithValue("@id", reminder.Id.ToString());
        command.Parameters.AddWithValue("@subject", reminder.Subject ?? string.Empty);
        command.Parameters.AddWithValue("@remindAt", FormatDateTime(reminder.RemindAt));
        command.Parameters.AddWithValue("@status", reminder.Status.ToString());
        command.Parameters.AddWithValue("@relatedId", reminder.RelatedId.ToString());
        command.Parameters.AddWithValue("@relatedType", reminder.RelatedType.ToString());
        command.ExecuteNonQuery();
    }

    private static void ExecuteNonQuery(SqliteConnection connection, SqliteTransaction? transaction, string commandText)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    private static bool HasSchemaValue(SqliteConnection connection, SqliteTransaction? transaction, string key)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM SchemaInfo WHERE Key = @key";
        command.Parameters.AddWithValue("@key", key);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    // Indica si una tabla tiene una columna con ese nombre (PRAGMA table_info).
    private static bool HasColumn(SqliteConnection connection, SqliteTransaction? transaction, string table, string column)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info(@table) WHERE name = @column";
        command.Parameters.AddWithValue("@table", table);
        command.Parameters.AddWithValue("@column", column);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private static void SetSchemaValue(SqliteConnection connection, SqliteTransaction transaction, string key, string value)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT OR REPLACE INTO SchemaInfo (Key, Value) VALUES (@key, @value)";
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);
        command.ExecuteNonQuery();
    }

    // Las fechas van en ISO yyyy-MM-dd; la cadena vacía significa «sin fecha»
    // (futuro cliente dado de alta sin fecha).
    private static string FormatDate(DateTime date) =>
        date == default ? string.Empty : date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTime ParseDate(string value) =>
        value.Length == 0 ? default : DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);

    // Las fechas con hora (recordatorios, specs/007) van en ISO
    // yyyy-MM-dd HH:mm:ss; la cadena vacía significa «sin fecha».
    private static string FormatDateTime(DateTime date) =>
        date == default ? string.Empty : date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static DateTime ParseDateTime(string value) =>
        value.Length == 0 ? default : DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None);

    private static string Text(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
}
