using Crm1.WindowsForms.Forms;
using Crm1.WindowsForms.Models;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de interfaz de ContactForm (specs/003) con el formulario REAL
/// instanciado en un hilo STA: Nuevo/Modificar/Cancelar, filtro del listado,
/// Guardar y la conversión en cliente con doble confirmación. Los diálogos se
/// responden con PromptRecorder en lugar de MessageBox.
/// </summary>
[Collection("Formularios UI")]
public sealed class ContactFormUiTests
{
    public ContactFormUiTests()
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
            Fixtures.Client("Luis", "23456781R"));
        UiTestEnvironment.WriteContacts(
            Fixtures.Contact("Elena"),
            Fixtures.Contact("Iker"));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            var recorder = new PromptRecorder();
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "contactsDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            Assert.Equal(3, grid.Rows.Count);
            grid.Rows[0].Cells["firstNameColumn"].Value = "Prueba";

            FormAccess.Click(form, "CancelButton_Click");

            Assert.Equal(2, grid.Rows.Count);
            Assert.False(form.HasUnsavedChanges);
            Assert.Empty(recorder.Messages);
        });
    }

    // El mismo flujo con un filtro activo (el caso que disparaba el error).
    [Fact]
    public void New_row_cancelled_with_an_active_filter_does_not_fail()
    {
        UiTestEnvironment.WriteClients(Fixtures.Client("Ana", "12345678Z"));
        UiTestEnvironment.WriteContacts(
            Fixtures.Contact("Elena"),
            Fixtures.Contact("Iker"));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            var recorder = new PromptRecorder();
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "contactsDataGridView");
            TextBox search = FormAccess.TextBox(form, "searchTextBox");

            search.Text = "elen";
            Assert.Equal("Mostrando 1 de 2", FormAccess.Label(form, "counterLabel").Text);

            FormAccess.Click(form, "NewButton_Click");
            grid.Rows[0].Cells["firstNameColumn"].Value = "Prueba";

            FormAccess.Click(form, "CancelButton_Click");

            Assert.Equal(2, grid.Rows.Count);
            Assert.Equal("Mostrando 1 de 2", FormAccess.Label(form, "counterLabel").Text);
            Assert.Empty(recorder.Messages);
        });
    }

    [Fact]
    public void Modify_then_cancel_restores_the_previous_values()
    {
        Contact elena = Fixtures.Contact("Elena");
        UiTestEnvironment.WriteClients(Fixtures.Client("Ana", "12345678Z"));
        UiTestEnvironment.WriteContacts(elena, Fixtures.Contact("Iker"));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "contactsDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["firstNameColumn"];
            Assert.Equal(elena.Id, form.CurrentContactId);

            FormAccess.Click(form, "ModifyButton_Click");
            Assert.True(form.IsEditing);
            grid.Rows[0].Cells["firstNameColumn"].Value = "Cambiado";
            grid.Rows[0].Cells["phoneColumn"].Value = "600999888";

            FormAccess.Click(form, "CancelButton_Click");

            Assert.False(form.IsEditing);
            Assert.Equal("Elena", FormAccess.CellText(grid.Rows[0], "firstNameColumn"));
            Assert.Equal(elena.Phone, FormAccess.CellText(grid.Rows[0], "phoneColumn"));
            Assert.False(form.HasUnsavedChanges);
        });
    }

    [Fact]
    public void Filter_by_status_and_text_counts_only_the_visible()
    {
        UiTestEnvironment.WriteClients(Fixtures.Client("Ana", "12345678Z"));
        UiTestEnvironment.WriteContacts(
            Fixtures.Contact("Elena", status: ContactStatus.Pending),
            Fixtures.Contact("Iker", status: ContactStatus.Discarded));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            form.Prompt = new PromptRecorder().Answer;
            DataGridView grid = FormAccess.Grid(form, "contactsDataGridView");
            ComboBox statusFilter = FormAccess.ComboBox(form, "statusFilterComboBox");
            Label counter = FormAccess.Label(form, "counterLabel");

            statusFilter.SelectedIndex = 2; // Descartado
            Assert.Equal("Mostrando 1 de 2", counter.Text);
            Assert.True(grid.Rows[1].Visible);
            Assert.False(grid.Rows[0].Visible);

            statusFilter.SelectedIndex = 1; // Pendiente
            Assert.Equal("Mostrando 1 de 2", counter.Text);

            statusFilter.SelectedIndex = 0; // Todos + texto
            FormAccess.TextBox(form, "searchTextBox").Text = "iker";
            Assert.Equal("Mostrando 1 de 2", counter.Text);
        });
    }

    // specs/003, sección 7: tras los dos «Sí», la conversión queda en memoria
    // y al Guardar se da el alta en clients.json y la baja en contacts.json.
    [Fact]
    public void Convert_with_double_confirmation_moves_the_contact_to_clients_on_save()
    {
        Contact convertible = Fixtures.ConvertibleContact();
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"));
        UiTestEnvironment.WriteContacts(convertible, Fixtures.Contact("Iker"));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            var recorder = new PromptRecorder(DialogResult.Yes, DialogResult.Yes, DialogResult.OK);
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "contactsDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["firstNameColumn"];
            FormAccess.Click(form, "ConvertButton_Click");

            // Dos diálogos: resumen del alta y advertencia final.
            Assert.Equal("CRM1 — Convertir en cliente", recorder.Captions[0]);
            Assert.Equal("CRM1 — Confirmar conversión", recorder.Captions[1]);
            Assert.Equal(1, grid.Rows.Count); // la fila se retira de la vista
            Assert.True(form.HasUnsavedChanges);

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Contains("dado de alta 1 cliente(s)", recorder.Messages[2]);
            Assert.False(form.HasUnsavedChanges);

            Assert.Equal(1, UiTestEnvironment.ReadContacts().Count);
            List<Client> clients = UiTestEnvironment.ReadClients();
            Assert.Equal(3, clients.Count);
            Client converted = clients.Single(client => client.TaxId == "11111111H");
            Assert.Equal("Clara", converted.FirstName);
            Assert.Equal(ClientStatus.Active, converted.Status);
            Assert.Equal(new DateTime(2026, 2, 10), converted.RegistrationDate);
        });
    }

    [Fact]
    public void Convert_refused_in_the_second_confirmation_keeps_everything()
    {
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"));
        UiTestEnvironment.WriteContacts(Fixtures.ConvertibleContact(), Fixtures.Contact("Iker"));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            var recorder = new PromptRecorder(DialogResult.Yes, DialogResult.No, DialogResult.OK);
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "contactsDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["firstNameColumn"];
            FormAccess.Click(form, "ConvertButton_Click");

            Assert.Equal(2, grid.Rows.Count);
            Assert.False(form.HasUnsavedChanges);

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Equal(2, UiTestEnvironment.ReadContacts().Count);
            Assert.Equal(2, UiTestEnvironment.ReadClients().Count);
        });
    }

    [Fact]
    public void Convert_incomplete_contact_lists_what_is_missing_and_keeps_the_row()
    {
        // Teléfono fijo: vale para un futuro cliente, pero el cliente exige móvil.
        Contact incomplete = Fixtures.Contact("Elena", phone: "912345678");
        UiTestEnvironment.WriteClients(Fixtures.Client("Ana", "12345678Z"));
        UiTestEnvironment.WriteContacts(incomplete, Fixtures.Contact("Iker"));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            var recorder = new PromptRecorder(DialogResult.OK);
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "contactsDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["firstNameColumn"];
            FormAccess.Click(form, "ConvertButton_Click");

            Assert.Single(recorder.Messages);
            Assert.Equal("CRM1 — Convertir en cliente", recorder.Captions[0]);
            Assert.Contains("Teléfono", recorder.Messages[0]);
            Assert.Contains("6 o 7", recorder.Messages[0]);
            Assert.Equal(2, grid.Rows.Count);
            Assert.False(form.HasUnsavedChanges);
            Assert.Equal(2, UiTestEnvironment.ReadContacts().Count);
        });
    }

    // Tercera capa de seguridad: Cancelar sin edición deshace las conversiones.
    [Fact]
    public void Convert_then_cancel_returns_the_contact_to_the_table()
    {
        UiTestEnvironment.WriteClients(
            Fixtures.Client("Ana", "12345678Z"),
            Fixtures.Client("Luis", "23456781R"));
        UiTestEnvironment.WriteContacts(Fixtures.ConvertibleContact(), Fixtures.Contact("Iker"));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            // Sí, Sí (conversión) + Sí (deshacer) + OK (guardar posterior).
            var recorder = new PromptRecorder(DialogResult.Yes, DialogResult.Yes, DialogResult.Yes, DialogResult.OK);
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "contactsDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["firstNameColumn"];
            FormAccess.Click(form, "ConvertButton_Click");
            Assert.Equal(1, grid.Rows.Count);

            FormAccess.Click(form, "CancelButton_Click");

            Assert.Equal("CRM1 — Cancelar conversiones", recorder.Captions[2]);
            Assert.Equal(2, grid.Rows.Count); // la fila vuelve a la tabla
            Assert.False(form.HasUnsavedChanges);

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Equal(2, UiTestEnvironment.ReadContacts().Count);
            Assert.Equal(2, UiTestEnvironment.ReadClients().Count);
        });
    }

    [Fact]
    public void Cancel_without_edits_or_conversions_does_nothing()
    {
        UiTestEnvironment.WriteClients(Fixtures.Client("Ana", "12345678Z"));
        UiTestEnvironment.WriteContacts(Fixtures.Contact("Elena"), Fixtures.Contact("Iker"));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            var recorder = new PromptRecorder();
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "contactsDataGridView");

            FormAccess.Click(form, "CancelButton_Click");

            Assert.Equal(2, grid.Rows.Count);
            Assert.Empty(recorder.Messages);
            Assert.False(form.HasUnsavedChanges);
        });
    }

    [Fact]
    public void Save_with_a_valid_new_contact_persists_it()
    {
        UiTestEnvironment.WriteClients(Fixtures.Client("Ana", "12345678Z"));
        UiTestEnvironment.WriteContacts(Fixtures.Contact("Elena"), Fixtures.Contact("Iker"));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            var recorder = new PromptRecorder(DialogResult.OK);
            form.Prompt = recorder.Answer;
            DataGridView grid = FormAccess.Grid(form, "contactsDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            grid.Rows[0].Cells["firstNameColumn"].Value = "Noa";
            grid.Rows[0].Cells["phoneColumn"].Value = "600777888";

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Contains("3 futuro(s) cliente(s)", recorder.Messages[0]);
            Assert.False(form.HasUnsavedChanges);

            List<Contact> saved = UiTestEnvironment.ReadContacts();
            Assert.Equal(3, saved.Count);
            Contact noa = saved.Single(contact => contact.FirstName == "Noa");
            Assert.Equal("600777888", noa.Phone);
            Assert.Equal(ContactStatus.Pending, noa.Status); // estado vacío → Pendiente
        });
    }

    // Navegación usada por la búsqueda global (specs/004) sobre este módulo.
    [Fact]
    public void SelectContactById_marks_the_contact_as_current()
    {
        Contact elena = Fixtures.Contact("Elena");
        UiTestEnvironment.WriteClients(Fixtures.Client("Ana", "12345678Z"));
        UiTestEnvironment.WriteContacts(elena, Fixtures.Contact("Iker"));

        WinFormsSta.Run(() =>
        {
            using var form = new ContactForm();
            form.Prompt = new PromptRecorder().Answer;

            Assert.True(form.SelectContactById(elena.Id));
            Assert.Equal(elena.Id, form.CurrentContactId);
            Assert.False(form.SelectContactById(Guid.NewGuid()));
        });
    }
}
