using Crm1.WindowsForms.Forms;
using Crm1.WindowsForms.Models;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de interfaz de ClientForm (specs/002) con el formulario REAL
/// instanciado en un hilo STA: Nuevo/Modificar/Cancelar, contador del listado,
/// Guardar, Eliminar y la fusión con clientes añadidos desde fuera. Los
/// diálogos se responden con PromptRecorder en lugar de MessageBox.
/// </summary>
[Collection("Formularios UI")]
public sealed class ClientFormUiTests
{
    public ClientFormUiTests()
    {
        UiTestEnvironment.EnsureCatalog();
    }

    // Regresión del error «La celda proporcionada no pertenece a este control
    // DataGridView»: Cancelar sobre una fila nueva debe limpiarla sin errores.
    [Fact]
    public void New_row_cancelled_disappears_without_errors()
    {
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"),
            Fixtures.Client("Zulema", "34567812P"));

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            var recorder = new PromptRecorder();
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            Assert.Equal(4, grid.Rows.Count);
            grid.Rows[0].Cells["firstNameColumn"].Value = "Prueba";

            FormAccess.Click(form, "CancelButton_Click");

            Assert.Equal(3, grid.Rows.Count);
            Assert.False(form.HasUnsavedChanges);
            Assert.Empty(recorder.Messages);
        });
    }

    [Fact]
    public void New_row_cancelled_three_times_in_a_row()
    {
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"));

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");

            for (int round = 1; round <= 3; round++)
            {
                FormAccess.Click(form, "NewButton_Click");
                grid.Rows[0].Cells["firstNameColumn"].Value = $"Prueba {round}";
                FormAccess.Click(form, "CancelButton_Click");
                Assert.Equal(2, grid.Rows.Count);
            }
        });
    }

    [Fact]
    public void Modify_then_cancel_restores_the_previous_values()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Client luis = Fixtures.Client("Luis", "23456781R");
        UiTestEnvironment.WriteClients(ana, luis);

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");

            grid.CurrentCell = grid.Rows[1].Cells["firstNameColumn"];
            Assert.Equal(luis.Id, form.CurrentClientId);

            FormAccess.Click(form, "ModifyButton_Click");
            Assert.True(form.IsEditing);
            grid.Rows[1].Cells["firstNameColumn"].Value = "Cambiado";
            grid.Rows[1].Cells["emailColumn"].Value = "otro@example.com";

            FormAccess.Click(form, "CancelButton_Click");

            Assert.False(form.IsEditing);
            Assert.Equal("Luis", FormAccess.CellText(grid.Rows[1], "firstNameColumn"));
            Assert.Equal(luis.Email, FormAccess.CellText(grid.Rows[1], "emailColumn"));
            Assert.False(form.HasUnsavedChanges);
        });
    }

    // Sin filtro local (specs/002: la búsqueda vive en el formulario
    // principal, specs/004), el contador refleja cuántas filas con datos hay
    // en la tabla; las filas vacías de un Nuevo en curso no cuentan.
    [Fact]
    public void Counter_counts_rows_with_data_and_ignores_empty_new_rows()
    {
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"),
            Fixtures.Client("Zulema", "34567812P"));

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");
            Label counter = FormAccess.Label(form, "counterLabel");

            Assert.Equal("Mostrando 3 de 3", counter.Text);

            // La fila vacía del Nuevo y su Cancelar no alteran el contador.
            FormAccess.Click(form, "NewButton_Click");
            Assert.Equal(4, grid.Rows.Count);
            FormAccess.Click(form, "CancelButton_Click");
            Assert.Equal(3, grid.Rows.Count);
            Assert.Equal("Mostrando 3 de 3", counter.Text);
        });
    }

    [Fact]
    public void Save_with_an_invalid_row_shows_the_errors_and_writes_nothing()
    {
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"));

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            var recorder = new PromptRecorder(DialogResult.OK);
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            grid.Rows[0].Cells["firstNameColumn"].Value = "Parcial";
            grid.Rows[0].Cells["phoneColumn"].Value = "912345678"; // fijo: inválido para clientes

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Single(recorder.Messages);
            Assert.Equal("CRM1 — Errores de validación", recorder.Captions[0]);
            Assert.Equal(3, grid.Rows.Count); // la fila inválida sigue en la tabla
            Assert.Equal(2, UiTestEnvironment.ReadClients().Count);
            Assert.True(form.HasUnsavedChanges);

            // Tras el fallo, Cancelar también debe limpiar la fila sin errores
            // (mismo riesgo de celda actual que el error corregido).
            FormAccess.Click(form, "CancelButton_Click");
            Assert.Equal(2, grid.Rows.Count);
            Assert.False(form.HasUnsavedChanges);
        });
    }

    [Fact]
    public void Save_with_a_valid_new_client_persists_it_normalized()
    {
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"),
            Fixtures.Client("Zulema", "34567812P"));

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            var recorder = new PromptRecorder(DialogResult.OK);
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            DataGridViewRow row = grid.Rows[0];
            row.Cells["firstNameColumn"].Value = "eva";            // se corrige al guardar
            row.Cells["lastName1Column"].Value = "marín";
            row.Cells["lastName2Column"].Value = "sáez";
            row.Cells["taxIdColumn"].Value = "87654321X";
            row.Cells["phoneColumn"].Value = "699887766";
            row.Cells["emailColumn"].Value = "eva@example.com";
            row.Cells["addressColumn"].Value = "gran vía 5";
            row.Cells["postalCodeColumn"].Value = "28001";
            row.Cells["cityColumn"].Value = "madrid";
            row.Cells["provinceColumn"].Value = "Madrid";
            row.Cells["statusColumn"].Value = "Activo";
            row.Cells["registrationDateColumn"].Value = "05/04/2026";
            row.Cells["notesColumn"].Value = "venida de la feria";

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Contains("4 cliente(s)", recorder.Messages[0]);
            Assert.False(form.HasUnsavedChanges);
            Assert.False(form.IsEditing);

            List<Client> saved = UiTestEnvironment.ReadClients();
            Assert.Equal(4, saved.Count);
            Client eva = saved.Single(client => client.TaxId == "87654321X");
            Assert.Equal("Eva", eva.FirstName);
            Assert.Equal("Marín", eva.LastName1);
            Assert.Equal("Gran Vía 5", eva.Address);
            Assert.Equal("Madrid", eva.City);
            Assert.Equal("Venida de la feria", eva.Notes);
            Assert.Equal(new DateTime(2026, 4, 5), eva.RegistrationDate);
            Assert.NotEqual(Guid.Empty, eva.Id);
        });
    }

    [Fact]
    public void Delete_confirmed_then_save_removes_the_client_from_disk()
    {
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"),
            Fixtures.Client("Zulema", "34567812P"));

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            var recorder = new PromptRecorder(DialogResult.Yes, DialogResult.OK);
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");

            grid.CurrentCell = grid.Rows[1].Cells["firstNameColumn"];
            FormAccess.Click(form, "DeleteButton_Click");

            Assert.Equal("CRM1 — Eliminar", recorder.Captions[0]);
            Assert.Equal(2, grid.Rows.Count);
            Assert.True(form.HasUnsavedChanges);

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Contains("2 cliente(s)", recorder.Messages[1]);
            List<Client> saved = UiTestEnvironment.ReadClients();
            Assert.Equal(2, saved.Count);
            Assert.DoesNotContain(saved, client => client.FirstName == "Luis");
        });
    }

    [Fact]
    public void Delete_refused_keeps_the_client_and_stays_clean()
    {
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"));

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            var recorder = new PromptRecorder(DialogResult.No);
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");

            grid.CurrentCell = grid.Rows[1].Cells["firstNameColumn"];
            FormAccess.Click(form, "DeleteButton_Click");

            Assert.Equal(2, grid.Rows.Count);
            Assert.False(form.HasUnsavedChanges);
            Assert.Equal(2, UiTestEnvironment.ReadClients().Count);
        });
    }

    // La conversión de un futuro cliente escribe en clients.json desde fuera:
    // al guardar aquí, ese registro debe conservarse (nunca sobrescribirse).
    [Fact]
    public void Save_keeps_clients_added_from_outside_while_the_form_was_open()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Client luis = Fixtures.Client("Luis", "23456781R");
        Client zulema = Fixtures.Client("Zulema", "34567812P");
        Client external = Fixtures.Client("Nuria", "87654321X");

        UiTestEnvironment.WriteClients(ana, luis, zulema);

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            form.Prompt = new PromptRecorder(DialogResult.OK).Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");
            Assert.Equal(3, grid.Rows.Count);

            // Otro módulo da de alta a Nuria mientras este formulario está abierto.
            UiTestEnvironment.WriteClients(ana, luis, zulema, external);
            UiTestEnvironment.TouchClientsFile();

            // Aquí se modifica a Ana y se guarda: Nuria debe conservarse.
            grid.CurrentCell = grid.Rows[0].Cells["firstNameColumn"];
            FormAccess.Click(form, "ModifyButton_Click");
            grid.Rows[0].Cells["notesColumn"].Value = "Nota nueva";
            FormAccess.Click(form, "SaveButton_Click");

            List<Client> saved = UiTestEnvironment.ReadClients();
            Assert.Equal(4, saved.Count);
            Assert.Contains(saved, client => client.FirstName == "Nuria");
            Assert.Contains(saved, client => client.Notes == "Nota nueva");
        });
    }

    // Al volver al módulo de Clientes tras una conversión, la tabla debe
    // recargar sola el archivo si cambió y aquí no hay cambios pendientes.
    [Fact]
    public void Showing_the_form_again_reloads_clients_changed_on_disk()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Client luis = Fixtures.Client("Luis", "23456781R");
        Client zulema = Fixtures.Client("Zulema", "34567812P");

        UiTestEnvironment.WriteClients(ana, luis, zulema);

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");
            Assert.Equal(3, grid.Rows.Count);

            form.Show();
            form.Visible = false; // como al cambiar a otro módulo

            UiTestEnvironment.WriteClients(ana, luis, zulema, Fixtures.Client("Nuria", "87654321X"));
            UiTestEnvironment.TouchClientsFile();

            form.Visible = true; // al volver, la tabla se recarga sola

            Assert.Equal(4, grid.Rows.Count);
            Assert.False(form.HasUnsavedChanges);
        });
    }

    // Navegación usada por la búsqueda global (specs/004) sobre este módulo.
    [Fact]
    public void SelectClientById_marks_the_client_as_current()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Client luis = Fixtures.Client("Luis", "23456781R");
        UiTestEnvironment.WriteClients(ana, luis);

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            form.Prompt = new PromptRecorder().Answer;

            Assert.True(form.SelectClientById(luis.Id));
            Assert.Equal(luis.Id, form.CurrentClientId);

            Assert.False(form.SelectClientById(Guid.NewGuid()));
            Assert.Equal(luis.Id, form.CurrentClientId);

            Assert.True(form.SelectClientById(ana.Id));
            Assert.Equal(ana.Id, form.CurrentClientId);
        });
    }

    [Fact]
    public void Sorting_by_name_reorders_the_rows_and_keeps_them_all_visible()
    {
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Zulema", "34567812P"),
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"));

        WinFormsSta.Run(() =>
        {
            using var form = new ClientForm();
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "clientsDataGridView");
            Label counter = FormAccess.Label(form, "counterLabel");

            grid.Sort(grid.Columns["firstNameColumn"], System.ComponentModel.ListSortDirection.Ascending);

            Assert.Equal("Ana", FormAccess.CellText(grid.Rows[0], "firstNameColumn"));
            Assert.Equal("Luis", FormAccess.CellText(grid.Rows[1], "firstNameColumn"));
            Assert.Equal("Zulema", FormAccess.CellText(grid.Rows[2], "firstNameColumn"));
            Assert.Equal("Mostrando 3 de 3", counter.Text);
            Assert.All(grid.Rows.Cast<DataGridViewRow>(), row => Assert.True(row.Visible));
        });
    }
}
