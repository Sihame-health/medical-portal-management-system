using System.Windows;
using System.Windows.Input;
using MedicalSystem.Database;
using MedicalSystem.Models;

namespace MedicalSystem
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (RoleComboBox != null && RoleComboBox.SelectedIndex < 0)
                RoleComboBox.SelectedIndex = 0;
            IdTextBox?.Focus();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string id = IdTextBox.Text.Trim();
            string password = PasswordBox.Password;
            int roleIndex = RoleComboBox.SelectedIndex;

            if (string.IsNullOrEmpty(id))
            {
                ShowError("Veuillez entrer votre identifiant.");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Veuillez entrer votre mot de passe.");
                return;
            }

            string role = roleIndex switch
            {
                0 => "Doctor",
                1 => "Nurse",
                2 => "Pharmacy",
                3 => "Admin",
                4 => "Reception",
                _ => ""
            };

            var user = DatabaseHelper.Authenticate(id, password, role);

            if (user != null)
            {
                // Mettre à jour la dernière connexion
                DatabaseHelper.UpdateUserLastLogin(id, role);

                ErrorMessage.Visibility = Visibility.Collapsed;

                Window? nextWindow = role switch
                {
                    "Doctor" => new DoctorWindow(user),
                    "Nurse" => new NurseWindow(user),
                    "Pharmacy" => new PharmacyWindow(),
                    "Admin" => new AdminWindow(),
                    "Reception" => new ReceptionWindow(),
                    _ => null
                };

                if (nextWindow != null)
                {
                    nextWindow.Show();
                    this.Close();
                }
            }
            else
            {
                ShowError("Identifiant, mot de passe ou rôle incorrect.");
            }
        }
        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }
    }
}