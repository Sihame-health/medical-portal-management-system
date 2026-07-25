using System;
using System.Windows;

namespace MedicalSystem
{
    public partial class AddEditMedicationDialog : Window
    {
        public string Code { get; private set; } = "";
        public string Name { get; private set; } = "";
        public string Description { get; private set; } = "";
        public int Quantity { get; private set; }
        public int MinThreshold { get; private set; }
        public DateTime ExpirationDate { get; private set; }

        public AddEditMedicationDialog()
        {
            InitializeComponent();
            ExpirationDatePicker.SelectedDate = DateTime.Now.AddMonths(6);
        }

        public AddEditMedicationDialog(Models.Medication medication) : this()
        {
            Title = "Modifier Médicament";
            CodeTextBox.Text = medication.Code;
            NameTextBox.Text = medication.Name;
            DescriptionTextBox.Text = medication.Description;
            QuantityTextBox.Text = medication.Quantity.ToString();
            MinThresholdTextBox.Text = medication.MinThreshold.ToString();
            ExpirationDatePicker.SelectedDate = medication.ExpirationDate;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CodeTextBox.Text) ||
                string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                string.IsNullOrWhiteSpace(QuantityTextBox.Text) ||
                string.IsNullOrWhiteSpace(MinThresholdTextBox.Text) ||
                ExpirationDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Veuillez remplir tous les champs obligatoires.",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty < 0)
            {
                MessageBox.Show("Quantité invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(MinThresholdTextBox.Text, out int threshold) || threshold < 0)
            {
                MessageBox.Show("Seuil minimum invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Code = CodeTextBox.Text;
            Name = NameTextBox.Text;
            Description = DescriptionTextBox.Text;
            Quantity = qty;
            MinThreshold = threshold;
            ExpirationDate = ExpirationDatePicker.SelectedDate ?? DateTime.Now.AddMonths(6);

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