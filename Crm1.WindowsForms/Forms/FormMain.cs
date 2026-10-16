using System.Windows.Forms;
using Crm1.WindowsForms.Forms;

namespace Crm1.WindowsForms
{
    // Formulario principal con la barra de menús de la aplicación
    public class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "CRM1 - Gestión de clientes";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new System.Drawing.Size(1000, 650);
            MinimumSize = new System.Drawing.Size(800, 500);

            // Barra de menús principal
            var menu = new MenuStrip();

            var clientsMenu = new ToolStripMenuItem("&Clientes");
            clientsMenu.DropDownItems.Add("Gestionar &clientes...", null, (s, e) => OpenClients());
            menu.Items.Add(clientsMenu);

            var exitMenu = new ToolStripMenuItem("&Salir");
            exitMenu.DropDownItems.Add("&Salir de CRM1", null, (s, e) => Close());
            menu.Items.Add(exitMenu);

            MainMenuStrip = menu;
            Controls.Add(menu);

            // Mensaje de bienvenida
            var welcome = new Label
            {
                Text = "Bienvenido a CRM1",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 16F),
                ForeColor = System.Drawing.Color.FromArgb(100, 116, 139)
            };
            Controls.Add(welcome);
            welcome.BringToFront();
        }

        // Abre el formulario de gestión de clientes
        private void OpenClients()
        {
            using var form = new FormClients();
            form.ShowDialog(this);
        }
    }
}
