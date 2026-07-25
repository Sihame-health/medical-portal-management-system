using System.Windows;
using System.Windows.Controls;

namespace MedicalSystem
{
    public partial class AddEditServiceDialog : Window
    {
        public string ServiceName { get; private set; } = "";
        public string Description { get; private set; } = "";
        public string Status { get; private set; } = "Actif";

        public AddEditServiceDialog()
        {
            InitializeComponent();
            StatusComboBox.SelectedIndex = 0;
        }

        public AddEditServiceDialog(Models.Service service) : this()
        {
            Title = "Modifier Service";
            NameTextBox.Text = service.Name;
            DescriptionTextBox.Text = service.Description;
            StatusComboBox.SelectedIndex = service.Status == "Actif" ? 0 : 1;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Veuillez entrer le nom du service.",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ServiceName = NameTextBox.Text;
            Description = DescriptionTextBox.Text;
            Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}