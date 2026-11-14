using System.Text;
using Crm1.WindowsForms.Forms;
using Crm1.WindowsForms.Models;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de interfaz del módulo de Recordatorios (specs/007): carga desde
/// la base SQL en solo lectura, nombres de vínculo resueltos, orden por
/// fecha y hora, contador, botonera inactiva, error visible con base
/// ilegible y recarga al volver a mostrarse.
/// </summary>
[Collection("Formularios UI")]
public sealed class ReminderFormUiTests
{
    private static Reminder ReminderOf(string subject, Guid relatedId, DateTime remindAt,
        TaskRelatedType relatedType = TaskRelatedType.Client,
        ReminderStatus status = ReminderStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        Subject = subject,
        RemindAt = remindAt,
        Status = status,
        RelatedId = relatedId,
        RelatedType = relatedType
    };

    [Fact]
    public void Load_shows_reminders_sorted_by_date_with_resolved_names_and_counter()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Contact elena = Fixtures.Contact("Elena");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts(elena);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Revisar propuesta", ana.Id, new DateTime(2026, 12, 1, 9, 15, 0)),
            ReminderOf("Llamada de Elena", elena.Id, new DateTime(2026, 11, 18, 18, 0, 0),
                TaskRelatedType.Prospect, ReminderStatus.Dismissed));

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            // Orden por fecha y hora ascendente (specs/007 §3): la de antes, arriba.
            Assert.Equal(2, grid.Rows.Count);
            Assert.Equal("Llamada de Elena", FormAccess.CellText(grid.Rows[0], "subjectColumn"));
            Assert.Equal("(Futuro cliente) Elena", FormAccess.CellText(grid.Rows[0], "relatedColumn"));
            Assert.Equal("18/11/2026 18:00", FormAccess.CellText(grid.Rows[0], "remindAtColumn"));
            Assert.Equal("Descartado", FormAccess.CellText(grid.Rows[0], "statusColumn"));
            Assert.Equal("Revisar propuesta", FormAccess.CellText(grid.Rows[1], "subjectColumn"));
            Assert.Equal("(Cliente) Ana García Pérez", FormAccess.CellText(grid.Rows[1], "relatedColumn"));
            Assert.Equal("01/12/2026 09:15", FormAccess.CellText(grid.Rows[1], "remindAtColumn"));
            Assert.Equal("Activo", FormAccess.CellText(grid.Rows[1], "statusColumn"));

            Assert.Equal("Mostrando 2 de 2", FormAccess.Label(form, "counterLabel").Text);

            // Botonera en estado normal (skills «modificar» y «borrar»):
            // Nuevo, Modificar, Eliminar y Guardar activos; Cancelar solo
            // durante la edición.
            Assert.True(FormAccess.Button(form, "newButton").Enabled);
            Assert.True(FormAccess.Button(form, "saveButton").Enabled);
            Assert.True(FormAccess.Button(form, "modifyButton").Enabled);
            Assert.True(FormAccess.Button(form, "deleteButton").Enabled);
            Assert.False(FormAccess.Button(form, "cancelButton").Enabled);
        });
    }

    [Fact]
    public void Reminder_with_missing_related_record_shows_a_dash()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Vínculo roto", Guid.NewGuid(), new DateTime(2026, 11, 18, 10, 0, 0)),
            ReminderOf("Llamada de Ana", ana.Id, new DateTime(2026, 11, 19, 10, 0, 0)));

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            Assert.Equal(2, grid.Rows.Count);
            Assert.Equal("—", FormAccess.CellText(grid.Rows[0], "relatedColumn"));
            Assert.Equal("(Cliente) Ana García Pérez", FormAccess.CellText(grid.Rows[1], "relatedColumn"));
        });
    }

    [Fact]
    public void Corrupt_database_shows_error_and_empty_table()
    {
        UiTestEnvironment.WriteClients(Fixtures.Client("Ana", "12345678Z"));
        File.WriteAllText(UiTestEnvironment.DatabasePath, "esto no es una base de datos", Encoding.UTF8);

        WinFormsSta.Run(() =>
        {
            PromptRecorder recorder = new();

            // El constructor carga los datos: el delegado se inyecta antes
            // para capturar el error sin mostrar un diálogo real.
            using var form = new ReminderForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };

            // El constructor ya ha mostrado el error visible (constitución, punto 5).
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");
            Assert.Empty(grid.Rows);
            Assert.Equal("Mostrando 0 de 0", FormAccess.Label(form, "counterLabel").Text);
            Assert.Single(recorder.Messages);
            Assert.Contains("No se pudo leer la base de datos de recordatorios", recorder.Messages[0]);
            Assert.False(FormAccess.Button(form, "newButton").Enabled);
            Assert.False(FormAccess.Button(form, "saveButton").Enabled);
        });
    }

    [Fact]
    public void Showing_the_form_again_reloads_reminders_changed_on_disk()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Primero", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0)));

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");
            Assert.Single(grid.Rows.Cast<DataGridViewRow>());

            form.Show();
            form.Visible = false; // como al cambiar a otro módulo

            // Otra parte de la aplicación guarda un recordatorio más.
            UiTestEnvironment.WriteReminders(
                ReminderOf("Primero", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0)),
                ReminderOf("Segundo", ana.Id, new DateTime(2026, 11, 25, 12, 30, 0)));

            form.Visible = true; // al volver, la tabla se recarga sola

            Assert.Equal(2, grid.Rows.Count);
            Assert.Equal("Mostrando 2 de 2", FormAccess.Label(form, "counterLabel").Text);
        });
    }

    [Fact]
    public void New_button_creates_row_with_default_status_and_locks_the_rest()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Llamada de Ana", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0)));

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");
            Assert.Single(grid.Rows);

            FormAccess.Click(form, "NewButton_Click");

            // Fila nueva arriba con el valor por defecto (specs/007 §3).
            Assert.Equal(2, grid.Rows.Count);
            Assert.Equal("Activo", FormAccess.CellText(grid.Rows[0], "statusColumn"));
            Assert.True(grid.Rows[0].ReadOnly == false);
            Assert.True(grid.Rows[1].ReadOnly);

            // Botonera en edición (skill «modificar»).
            Assert.False(FormAccess.Button(form, "newButton").Enabled);
            Assert.True(FormAccess.Button(form, "cancelButton").Enabled);
            Assert.True(FormAccess.Button(form, "saveButton").Enabled);
            Assert.True(form.HasUnsavedChanges);
        });
    }

    [Fact]
    public void Cancel_removes_the_new_row_and_clears_the_dirty_flag()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Llamada de Ana", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0)));

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            grid.Rows[0].Cells["subjectColumn"].Value = "Lo que fuera";
            Assert.Equal(2, grid.Rows.Count);

            FormAccess.Click(form, "CancelButton_Click");

            Assert.Single(grid.Rows);
            Assert.False(form.HasUnsavedChanges);
            Assert.True(FormAccess.Button(form, "newButton").Enabled);
            Assert.False(FormAccess.Button(form, "cancelButton").Enabled);
        });
    }

    [Fact]
    public void Save_with_incomplete_row_marks_errors_and_keeps_editing()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Llamada de Ana", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0)));
        PromptRecorder recorder = new();

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            FormAccess.Click(form, "NewButton_Click");

            // La fila debe tener algo más allá del valor por defecto para que
            // no se trate como un Nuevo por accidente y se valide.
            grid.Rows[0].Cells["subjectColumn"].Value = "Falta casi todo";
            FormAccess.Click(form, "SaveButton_Click");

            // Un error por fila, el primero encontrado (mismo criterio que
            // TaskForm): aquí el vínculo ausente.
            Assert.Equal(2, grid.Rows.Count);
            Assert.Single(recorder.Messages);
            Assert.Equal("Debes elegir un cliente o futuro cliente.", recorder.Messages[0]);

            // Con errores la edición sigue abierta (decisión del 29/10).
            Assert.True(FormAccess.Button(form, "cancelButton").Enabled);
            Assert.True(form.HasUnsavedChanges);

            // La base no se ha tocado.
            Assert.Single(UiTestEnvironment.ReadReminders());
        });
    }

    [Fact]
    public void Save_persists_new_reminder_reorders_by_date_and_confirms()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Reunión de diciembre", ana.Id, new DateTime(2026, 12, 20, 12, 0, 0)));
        PromptRecorder recorder = new();

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            DataGridViewRow row = grid.Rows[0];
            row.Cells["subjectColumn"].Value = "Llamada de noviembre";
            row.Cells["relatedColumn"].Value = "(Cliente) Ana García Pérez";
            row.Cells["remindAtColumn"].Value = "18/11/2026 09:45";
            row.Cells["statusColumn"].Value = "Activo";

            FormAccess.Click(form, "SaveButton_Click");

            // Confirmación de guardado correcto.
            Assert.Single(recorder.Messages);
            Assert.Contains("2 recordatorio(s) correctamente", recorder.Messages[0]);
            Assert.Equal(2, grid.Rows.Count);
            Assert.False(form.HasUnsavedChanges);
            Assert.True(FormAccess.Button(form, "newButton").Enabled);
            Assert.False(FormAccess.Button(form, "cancelButton").Enabled);
            Assert.True(grid.Rows[0].ReadOnly);

            // Reordenado por fecha y hora ascendente tras guardar (decisión
            // del usuario): el de noviembre queda arriba del de diciembre.
            Assert.Equal("Llamada de noviembre", FormAccess.CellText(grid.Rows[0], "subjectColumn"));
            Assert.Equal("18/11/2026 09:45", FormAccess.CellText(grid.Rows[0], "remindAtColumn"));
            Assert.Equal("Reunión de diciembre", FormAccess.CellText(grid.Rows[1], "subjectColumn"));

            // La base contiene el recordatorio nuevo con todos sus campos.
            List<Reminder> saved = UiTestEnvironment.ReadReminders();
            Reminder? created = saved.FirstOrDefault(r => r.Subject == "Llamada de noviembre");
            Assert.NotNull(created);
            Assert.Equal(new DateTime(2026, 11, 18, 9, 45, 0), created!.RemindAt);
            Assert.Equal(ReminderStatus.Active, created.Status);
            Assert.Equal(ana.Id, created.RelatedId);
            Assert.Equal(TaskRelatedType.Client, created.RelatedType);
        });
    }

    [Fact]
    public void Selecting_a_row_marks_the_current_reminder()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Reminder early = ReminderOf("Temprano", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0));
        Reminder late = ReminderOf("Tardío", ana.Id, new DateTime(2026, 11, 25, 12, 0, 0));
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(early, late);

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            // Tras la carga no hay marca hasta que el usuario marca una fila.
            Assert.Null(form.CurrentReminderId);

            grid.Rows[0].Selected = true; // clic simulado en la fila de «Temprano»
            Assert.Equal(early.Id, form.CurrentReminderId);

            grid.ClearSelection();
            Assert.Null(form.CurrentReminderId);
        });
    }

    [Fact]
    public void Modify_without_selection_shows_a_warning()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Llamada de Ana", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0)));
        PromptRecorder recorder = new();

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };

            FormAccess.Click(form, "ModifyButton_Click");

            Assert.Single(recorder.Messages);
            Assert.Equal("Selecciona un recordatorio de la tabla.", recorder.Messages[0]);
            Assert.True(FormAccess.Button(form, "newButton").Enabled);
        });
    }

    [Fact]
    public void Modify_opens_the_marked_row_and_cancel_restores_it()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Reminder reminder = ReminderOf("Llamada de Ana", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0));
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(reminder);

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            grid.Rows[0].Selected = true;
            FormAccess.Click(form, "ModifyButton_Click");

            // La fila marcada se activa para edición.
            Assert.True(grid.Rows[0].ReadOnly == false);
            grid.Rows[0].Cells["subjectColumn"].Value = "Cambiado";
            grid.Rows[0].Cells["remindAtColumn"].Value = "20/11/2026 19:30";

            FormAccess.Click(form, "CancelButton_Click");

            // Cancelar restaura la instantánea y la marca persiste.
            Assert.Equal("Llamada de Ana", FormAccess.CellText(grid.Rows[0], "subjectColumn"));
            Assert.Equal("18/11/2026 10:00", FormAccess.CellText(grid.Rows[0], "remindAtColumn"));
            Assert.True(grid.Rows[0].ReadOnly);
            Assert.False(form.HasUnsavedChanges);
            Assert.Equal(reminder.Id, form.CurrentReminderId);
        });
    }

    [Fact]
    public void Modify_and_save_persists_the_change()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Llamada de Ana", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0)));
        PromptRecorder recorder = new();

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            grid.Rows[0].Selected = true;
            FormAccess.Click(form, "ModifyButton_Click");
            grid.Rows[0].Cells["subjectColumn"].Value = "Llamada aplazada";
            grid.Rows[0].Cells["remindAtColumn"].Value = "18/11/2026 17:15";

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Contains("1 recordatorio(s) correctamente", recorder.Messages[^1]);
            Assert.False(form.HasUnsavedChanges);

            Reminder saved = Assert.Single(UiTestEnvironment.ReadReminders());
            Assert.Equal("Llamada aplazada", saved.Subject);
            Assert.Equal(new DateTime(2026, 11, 18, 17, 15, 0), saved.RemindAt);
        });
    }

    [Fact]
    public void Delete_without_selection_shows_a_warning()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Llamada de Ana", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0)));
        PromptRecorder recorder = new();

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            FormAccess.Click(form, "DeleteButton_Click");

            Assert.Single(recorder.Messages);
            Assert.Equal("Selecciona un recordatorio de la tabla.", recorder.Messages[0]);
            Assert.Single(grid.Rows);
            Assert.Single(UiTestEnvironment.ReadReminders());
        });
    }

    [Fact]
    public void Delete_cancelled_keeps_the_row()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Llamada de Ana", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0)));
        PromptRecorder recorder = new(DialogResult.No);

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            grid.Rows[0].Selected = true;
            FormAccess.Click(form, "DeleteButton_Click");

            // Confirmación identificando el recordatorio; al rechazarla no
            // cambia nada.
            Assert.Single(recorder.Messages);
            Assert.Contains("\"Llamada de Ana\"", recorder.Messages[0]);
            Assert.Contains("(Cliente) Ana García Pérez", recorder.Messages[0]);
            Assert.Single(grid.Rows);
            Assert.False(form.HasUnsavedChanges);
            Assert.Single(UiTestEnvironment.ReadReminders());
        });
    }

    [Fact]
    public void Delete_confirmed_removes_the_row_only_after_save()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteReminders(
            ReminderOf("Llamada de Ana", ana.Id, new DateTime(2026, 11, 18, 10, 0, 0)),
            ReminderOf("Reunión", ana.Id, new DateTime(2026, 11, 25, 12, 0, 0)));

        WinFormsSta.Run(() =>
        {
            using var form = new ReminderForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder(DialogResult.Yes).Answer;
            DataGridView grid = FormAccess.Grid(form, "remindersDataGridView");

            grid.Rows[0].Selected = true;
            FormAccess.Click(form, "DeleteButton_Click");

            // Fuera de la tabla y del contador, con cambio pendiente, pero la
            // base conserva el registro hasta Guardar (skill «borrar»).
            Assert.Single(grid.Rows);
            Assert.Equal("Mostrando 1 de 1", FormAccess.Label(form, "counterLabel").Text);
            Assert.True(form.HasUnsavedChanges);
            Assert.Equal(2, UiTestEnvironment.ReadReminders().Count);

            FormAccess.Click(form, "SaveButton_Click");

            Reminder remaining = Assert.Single(UiTestEnvironment.ReadReminders());
            Assert.Equal("Reunión", remaining.Subject);
            Assert.False(form.HasUnsavedChanges);
        });
    }
}
