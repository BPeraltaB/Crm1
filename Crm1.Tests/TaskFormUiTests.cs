using System.Text;
using Crm1.WindowsForms.Forms;
using Crm1.WindowsForms.Models;
using Xunit;
using Task = Crm1.WindowsForms.Models.Task;
using TaskStatus = Crm1.WindowsForms.Models.TaskStatus;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de interfaz del módulo de Tareas (specs/005): carga desde la base
/// SQL, nombres de vínculo resueltos, contador, botones, error visible con
/// base ilegible, recarga al volver a mostrarse y edición con Nuevo, Guardar
/// y Cancelar (skill «modificar»).
/// </summary>
[Collection("Formularios UI")]
public sealed class TaskFormUiTests
{
    private static Task TaskOf(string action, Guid relatedId,
        TaskRelatedType relatedType = TaskRelatedType.Client,
        TaskStatus status = TaskStatus.Pending,
        TaskPriority priority = TaskPriority.Medium) => new()
    {
        Id = Guid.NewGuid(),
        Action = action,
        Description = "Descripción de prueba",
        DueDate = new DateTime(2026, 12, 1),
        Status = status,
        Priority = priority,
        RelatedId = relatedId,
        RelatedType = relatedType
    };

    [Fact]
    public void Load_shows_tasks_with_resolved_names_and_counter()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Contact elena = Fixtures.Contact("Elena");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts(elena);
        UiTestEnvironment.WriteTasks(
            TaskOf("Llamar", ana.Id),
            TaskOf("Enviar correo", elena.Id, TaskRelatedType.Prospect,
                TaskStatus.InProgress, TaskPriority.High));

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            Assert.Equal(2, grid.Rows.Count);
            Assert.Equal("Llamar", FormAccess.CellText(grid.Rows[0], "actionColumn"));
            Assert.Equal("Descripción de prueba", FormAccess.CellText(grid.Rows[0], "descriptionColumn"));
            Assert.Equal("(Cliente) Ana García Pérez", FormAccess.CellText(grid.Rows[0], "relatedColumn"));
            Assert.Equal("(Futuro cliente) Elena", FormAccess.CellText(grid.Rows[1], "relatedColumn"));
            Assert.Equal("01/12/2026", FormAccess.CellText(grid.Rows[0], "dueDateColumn"));
            Assert.Equal("Pendiente", FormAccess.CellText(grid.Rows[0], "statusColumn"));
            Assert.Equal("En curso", FormAccess.CellText(grid.Rows[1], "statusColumn"));
            Assert.Equal("Alta", FormAccess.CellText(grid.Rows[1], "priorityColumn"));

            Assert.Equal("Mostrando 2 de 2", FormAccess.Label(form, "counterLabel").Text);

            // Botonera en estado normal (skill «modificar»): Nuevo, Modificar,
            // Eliminar y Guardar activos; Cancelar solo durante la edición.
            Assert.True(FormAccess.Button(form, "newButton").Enabled);
            Assert.True(FormAccess.Button(form, "saveButton").Enabled);
            Assert.True(FormAccess.Button(form, "modifyButton").Enabled);
            Assert.True(FormAccess.Button(form, "deleteButton").Enabled);
            Assert.False(FormAccess.Button(form, "cancelButton").Enabled);
        });
    }

    [Fact]
    public void Task_with_missing_related_record_shows_a_dash()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(
            TaskOf("Llamar", Guid.NewGuid()),
            TaskOf("Visitar", ana.Id));

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

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
            using var form = new TaskForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };

            // El constructor ya ha mostrado el error visible (constitución, punto 5).
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");
            Assert.Empty(grid.Rows);
            Assert.Equal("Mostrando 0 de 0", FormAccess.Label(form, "counterLabel").Text);
            Assert.Single(recorder.Messages);
            Assert.Contains("No se pudo leer la base de datos de tareas", recorder.Messages[0]);
            Assert.False(FormAccess.Button(form, "newButton").Enabled);
            Assert.False(FormAccess.Button(form, "saveButton").Enabled);
        });
    }

    [Fact]
    public void Showing_the_form_again_reloads_tasks_changed_on_disk()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id), TaskOf("Visitar", ana.Id));

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");
            Assert.Equal(2, grid.Rows.Count);

            form.Show();
            form.Visible = false; // como al cambiar a otro módulo

            // Otra parte de la aplicación guarda una tarea más en la base.
            UiTestEnvironment.WriteTasks(
                TaskOf("Llamar", ana.Id), TaskOf("Visitar", ana.Id), TaskOf("Reunión", ana.Id));

            form.Visible = true; // al volver, la tabla se recarga sola

            Assert.Equal(3, grid.Rows.Count);
            Assert.Equal("Mostrando 3 de 3", FormAccess.Label(form, "counterLabel").Text);
        });
    }

    [Fact]
    public void New_button_creates_row_with_defaults_and_locks_the_rest()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id));

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");
            Assert.Equal(1, grid.Rows.Count);

            FormAccess.Click(form, "NewButton_Click");

            // Fila nueva arriba con los valores por defecto (specs/005 §4).
            Assert.Equal(2, grid.Rows.Count);
            Assert.Equal("Pendiente", FormAccess.CellText(grid.Rows[0], "statusColumn"));
            Assert.Equal("Media", FormAccess.CellText(grid.Rows[0], "priorityColumn"));
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
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id));

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            grid.Rows[0].Cells["descriptionColumn"].Value = "Lo que fuera";
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
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id));
        PromptRecorder recorder = new();

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            FormAccess.Click(form, "NewButton_Click");

            // La fila debe tener algo más allá de los valores por defecto para
            // que no se trate como un Nuevo por accidente y se valide.
            grid.Rows[0].Cells["descriptionColumn"].Value = "Falta casi todo";
            FormAccess.Click(form, "SaveButton_Click");

            // Un error por fila, el primero encontrado (mismo criterio que
            // ClientForm): aquí el vínculo ausente.
            Assert.Equal(2, grid.Rows.Count);
            Assert.Single(recorder.Messages);
            Assert.Equal("Debes elegir un cliente o futuro cliente.", recorder.Messages[0]);

            // Con errores la edición sigue abierta (decisión del 29/10).
            Assert.True(FormAccess.Button(form, "cancelButton").Enabled);
            Assert.True(form.HasUnsavedChanges);

            // La base no se ha tocado.
            Assert.Single(UiTestEnvironment.ReadTasks());
        });
    }

    [Fact]
    public void Save_persists_the_new_task_and_closes_the_edition()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id));
        PromptRecorder recorder = new();

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            DataGridViewRow row = grid.Rows[0];
            row.Cells["actionColumn"].Value = "Visitar";
            row.Cells["descriptionColumn"].Value = "Presentar muebles";
            row.Cells["relatedColumn"].Value = "(Cliente) Ana García Pérez";
            row.Cells["dueDateColumn"].Value = "15/12/2026";
            row.Cells["priorityColumn"].Value = "Alta";

            FormAccess.Click(form, "SaveButton_Click");

            // Confirmación de guardado correcto.
            Assert.Single(recorder.Messages);
            Assert.Contains("2 tarea(s) correctamente", recorder.Messages[0]);
            Assert.Equal(2, grid.Rows.Count);
            Assert.False(form.HasUnsavedChanges);
            Assert.True(FormAccess.Button(form, "newButton").Enabled);
            Assert.False(FormAccess.Button(form, "cancelButton").Enabled);
            Assert.True(grid.Rows[0].ReadOnly);

            // La base contiene la tarea nueva con todos sus campos.
            List<Task> saved = UiTestEnvironment.ReadTasks();
            Task? created = saved.FirstOrDefault(t => t.Action == "Visitar");
            Assert.NotNull(created);
            Assert.Equal("Presentar muebles", created!.Description);
            Assert.Equal(new DateTime(2026, 12, 15), created.DueDate);
            Assert.Equal(TaskPriority.High, created.Priority);
            Assert.Equal(ana.Id, created.RelatedId);
            Assert.Equal(TaskRelatedType.Client, created.RelatedType);
        });
    }

    [Fact]
    public void Selecting_a_row_marks_the_current_task()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Task first = TaskOf("Llamar", ana.Id);
        Task second = TaskOf("Visitar", ana.Id);
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(first, second);

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            // Tras la carga no hay marca hasta que el usuario marca una fila.
            Assert.Null(form.CurrentTaskId);

            grid.CurrentCell = grid.Rows[1].Cells["actionColumn"];
            Assert.Equal(second.Id, form.CurrentTaskId);
            Assert.Equal("Visitar", form.CurrentTask!.Action);

            // Sin selección no hay tarea actual, nunca un valor inventado.
            grid.ClearSelection();
            Assert.Null(form.CurrentTaskId);
        });
    }

    [Fact]
    public void Modify_without_selection_shows_a_warning()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id));
        PromptRecorder recorder = new();

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            FormAccess.Click(form, "ModifyButton_Click");

            Assert.Single(recorder.Messages);
            Assert.Equal("Selecciona una tarea de la tabla.", recorder.Messages[0]);
            Assert.True(grid.Rows[0].ReadOnly);
        });
    }

    [Fact]
    public void Modify_opens_the_marked_row_and_cancel_restores_it()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Task first = TaskOf("Llamar", ana.Id);
        Task second = TaskOf("Visitar", ana.Id);
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(first, second);

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            grid.CurrentCell = grid.Rows[1].Cells["actionColumn"];
            FormAccess.Click(form, "ModifyButton_Click");

            // Solo la fila marcada se edita; la otra queda bloqueada y atenuada.
            Assert.False(grid.Rows[1].ReadOnly);
            Assert.True(grid.Rows[0].ReadOnly);
            Assert.False(FormAccess.Button(form, "newButton").Enabled);
            Assert.False(FormAccess.Button(form, "modifyButton").Enabled);
            Assert.False(FormAccess.Button(form, "deleteButton").Enabled);
            Assert.True(FormAccess.Button(form, "cancelButton").Enabled);

            grid.Rows[1].Cells["actionColumn"].Value = "Reunión";
            grid.Rows[1].Cells["dueDateColumn"].Value = "20/12/2026";

            FormAccess.Click(form, "CancelButton_Click");

            // Cancelar restaura la instantánea y mantiene la marca (regla 4).
            Assert.Equal("Visitar", FormAccess.CellText(grid.Rows[1], "actionColumn"));
            Assert.Equal("01/12/2026", FormAccess.CellText(grid.Rows[1], "dueDateColumn"));
            Assert.True(grid.Rows[1].ReadOnly);
            Assert.False(form.HasUnsavedChanges);
            Assert.Equal(second.Id, form.CurrentTaskId);

            // La base sigue intacta.
            Assert.Equal(2, UiTestEnvironment.ReadTasks().Count);
        });
    }

    [Fact]
    public void Modify_and_save_persists_the_change()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Task second = TaskOf("Visitar", ana.Id);
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id), second);
        PromptRecorder recorder = new();

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            grid.CurrentCell = grid.Rows[1].Cells["actionColumn"];
            FormAccess.Click(form, "ModifyButton_Click");
            grid.Rows[1].Cells["actionColumn"].Value = "Reunión";
            grid.Rows[1].Cells["dueDateColumn"].Value = "20/12/2026";

            FormAccess.Click(form, "SaveButton_Click");

            Task saved = UiTestEnvironment.ReadTasks().Single(t => t.Id == second.Id);
            Assert.Equal("Reunión", saved.Action);
            Assert.Equal(new DateTime(2026, 12, 20), saved.DueDate);
            Assert.False(form.HasUnsavedChanges);
            Assert.Equal(second.Id, form.CurrentTaskId);
        });
    }

    [Fact]
    public void Delete_without_selection_shows_a_warning()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id));
        PromptRecorder recorder = new();

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            FormAccess.Click(form, "DeleteButton_Click");

            Assert.Single(recorder.Messages);
            Assert.Equal("Selecciona una tarea de la tabla.", recorder.Messages[0]);
            Assert.Single(grid.Rows);
        });
    }

    [Fact]
    public void Delete_cancelled_keeps_the_row()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id), TaskOf("Visitar", ana.Id));
        PromptRecorder recorder = new(DialogResult.No);

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["actionColumn"];
            FormAccess.Click(form, "DeleteButton_Click");

            // La confirmación identifica la tarea por su acción y su registro.
            Assert.Single(recorder.Messages);
            Assert.Contains("¿Seguro que quieres eliminar la tarea Llamar de (Cliente) Ana García Pérez?", recorder.Messages[0]);

            // Rechazada: no se toca ni la tabla ni la base.
            Assert.Equal(2, grid.Rows.Count);
            Assert.False(form.HasUnsavedChanges);
            Assert.Equal(2, UiTestEnvironment.ReadTasks().Count);
        });
    }

    [Fact]
    public void Delete_confirmed_removes_the_row_only_after_save()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Task first = TaskOf("Llamar", ana.Id);
        Task second = TaskOf("Visitar", ana.Id);
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(first, second);
        PromptRecorder recorder = new(DialogResult.Yes);

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["actionColumn"];
            FormAccess.Click(form, "DeleteButton_Click");

            // Eliminación en memoria: la fila desaparece, la marca nunca queda
            // huérfana (sigue a la selección restante) y queda un cambio sin
            // guardar; la base sigue intacta (skill «borrar»).
            Assert.Single(grid.Rows);
            Assert.NotEqual(first.Id, form.CurrentTaskId);
            Assert.True(form.HasUnsavedChanges);
            Assert.Equal(2, UiTestEnvironment.ReadTasks().Count);

            FormAccess.Click(form, "SaveButton_Click");

            // Solo Guardar hace la eliminación permanente.
            List<Task> saved = UiTestEnvironment.ReadTasks();
            Assert.Single(saved);
            Assert.Equal(second.Id, saved[0].Id);
            Assert.False(form.HasUnsavedChanges);
            Assert.Equal("Mostrando 1 de 1", FormAccess.Label(form, "counterLabel").Text);
        });
    }

    // Pruebas del filtro por cliente/futuro cliente (botón Tareas de otros
    // formularios, specs/005 §7).

    [Fact]
    public void Filter_shows_only_the_tasks_of_the_record()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Client luis = Fixtures.Client("Luis", "23456781R");
        UiTestEnvironment.WriteClients(ana, luis);
        UiTestEnvironment.WriteTasks(
            TaskOf("Llamar", ana.Id),
            TaskOf("Escribir", luis.Id),
            TaskOf("Visitar", ana.Id));

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(null);
            form.Prompt = new PromptRecorder().Answer;
            form.ShowTasksOf(TaskRelatedType.Client, ana.Id, "Ana García Pérez");
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            Assert.Equal(2, grid.Rows.Count);
            Assert.All(grid.Rows.Cast<DataGridViewRow>(),
                row => Assert.Equal("(Cliente) Ana García Pérez",
                    FormAccess.CellText(row, "relatedColumn")));
            Assert.Equal("Mostrando 2 de 2", FormAccess.Label(form, "counterLabel").Text);
            Assert.Equal("Tareas de: (Cliente) Ana García Pérez",
                FormAccess.Label(form, "filterLabel").Text);
        });
    }

    [Fact]
    public void Show_all_restores_every_task()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Client luis = Fixtures.Client("Luis", "23456781R");
        UiTestEnvironment.WriteClients(ana, luis);
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id), TaskOf("Escribir", luis.Id));

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(null);
            form.Prompt = new PromptRecorder().Answer;
            form.ShowTasksOf(TaskRelatedType.Client, ana.Id, "Ana García Pérez");
            form.ShowAllTasks();
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            // El filtro se retira: vuelven a verse todas las tareas.
            Assert.Equal(2, grid.Rows.Count);
        });
    }

    [Fact]
    public void Saving_with_filter_keeps_the_hidden_tasks()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Client luis = Fixtures.Client("Luis", "23456781R");
        Task anaCall = TaskOf("Llamar", ana.Id);
        Task anaVisit = TaskOf("Visitar", ana.Id);
        Task luisMail = TaskOf("Escribir", luis.Id);
        UiTestEnvironment.WriteClients(ana, luis);
        UiTestEnvironment.WriteTasks(anaCall, anaVisit, luisMail);

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(null);
            // Sí a la confirmación de eliminar; Aceptar al guardar.
            form.Prompt = new PromptRecorder(DialogResult.Yes, DialogResult.OK).Answer;
            form.ShowTasksOf(TaskRelatedType.Client, ana.Id, "Ana García Pérez");
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");
            Assert.Equal(2, grid.Rows.Count);

            // Elimina la primera tarea de Ana y guarda con el filtro activo.
            grid.CurrentCell = grid.Rows[0].Cells["actionColumn"];
            FormAccess.Click(form, "DeleteButton_Click");
            Assert.Single(grid.Rows);
            FormAccess.Click(form, "SaveButton_Click");

            // Las tareas ocultas por el filtro se conservan (constitución, 5).
            List<Task> saved = UiTestEnvironment.ReadTasks();
            Assert.Equal(2, saved.Count);
            Assert.Contains(saved, task => task.Id == luisMail.Id);
            Assert.Contains(saved, task => task.Id == anaVisit.Id);
            Assert.DoesNotContain(saved, task => task.Id == anaCall.Id);
        });
    }

    [Fact]
    public void New_with_filter_is_prelinked_and_the_link_stays_locked()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(TaskOf("Llamar", ana.Id));

        WinFormsSta.Run(() =>
        {
            using var form = new TaskForm(null);
            form.Prompt = new PromptRecorder().Answer;
            form.ShowTasksOf(TaskRelatedType.Client, ana.Id, "Ana García Pérez");
            DataGridView grid = FormAccess.Grid(form, "tasksDataGridView");

            FormAccess.Click(form, "NewButton_Click");

            // La tarea nueva nace vinculada al registro filtrado y su celda
            // queda bloqueada: solo se crean tareas de ese registro.
            Assert.Equal("(Cliente) Ana García Pérez",
                FormAccess.CellText(grid.Rows[0], "relatedColumn"));
            Assert.True(grid.Rows[0].Cells["relatedColumn"].ReadOnly);
            Assert.False(grid.Rows[0].Cells["actionColumn"].ReadOnly);
        });
    }
}
