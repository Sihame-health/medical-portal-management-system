using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MedicalSystem.Database;
using MedicalSystem.Models;

namespace MedicalSystem
{
    public partial class AddEditUserDialog : Window
    {
        public string FirstName { get; private set; } = "";
        public string LastName { get; private set; } = "";
        public string Email { get; private set; } = "";
        public string Username { get; private set; } = "";
        public string Password { get; private set; } = "";
        public string Role { get; private set; } = "";
        public string Status { get; private set; } = "Actif";
        public int? ServiceId { get; private set; }
        public string? ServiceName { get; private set; }

        public AddEditUserDialog()
        {
            InitializeComponent();
            RoleComboBox.SelectedIndex = 0;
            StatusComboBox.SelectedIndex = 0;
            LoadServices();
            ServiceComboBox.SelectedIndex = 0;

            // Ajouter la validation en temps réel pour l'email
            EmailTextBox.TextChanged += EmailTextBox_TextChanged;
        }

        public AddEditUserDialog(Models.User user) : this()
        {
            Title = "Modifier Utilisateur";
            FirstNameTextBox.Text = user.FirstName;
            LastNameTextBox.Text = user.LastName;
            EmailTextBox.Text = user.Email;
            UsernameTextBox.Text = user.Username;
            PasswordTextBox.Text = user.Password;

            // Sélectionner le rôle
            for (int i = 0; i < RoleComboBox.Items.Count; i++)
            {
                if ((RoleComboBox.Items[i] as ComboBoxItem)?.Content?.ToString() == user.Role)
                {
                    RoleComboBox.SelectedIndex = i;
                    break;
                }
            }

            StatusComboBox.SelectedIndex = user.Status == "Actif" ? 0 : 1;

            // Sélectionner le service si le rôle est Doctor
            if (user.Role == "Doctor" && user.ServiceId.HasValue)
            {
                for (int i = 0; i < ServiceComboBox.Items.Count; i++)
                {
                    if (ServiceComboBox.Items[i] is ComboBoxItem comboItem &&
                        comboItem.Tag is int serviceId &&
                        serviceId == user.ServiceId.Value)
                    {
                        ServiceComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void LoadServices()
        {
            var services = DatabaseHelper.GetServices()
                .Where(s => s.Status == "Actif")
                .OrderBy(s => s.Name)
                .ToList();

            ServiceComboBox.Items.Clear();

            // Ajouter une option vide pour les rôles non-médecins
            ServiceComboBox.Items.Add(new ComboBoxItem
            {
                Content = "(Non applicable)",
                Tag = -1,
                IsEnabled = true
            });

            foreach (var service in services)
            {
                ServiceComboBox.Items.Add(new ComboBoxItem
                {
                    Content = $"{service.Name}",
                    Tag = service.Id,
                    ToolTip = service.Description
                });
            }
        }

        private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Activer/désactiver la sélection de service selon le rôle
            bool isDoctor = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Doctor";
            ServiceComboBox.IsEnabled = isDoctor;
            ServiceLabelTextBlock.IsEnabled = isDoctor;

            if (!isDoctor)
            {
                ServiceComboBox.SelectedIndex = 0; // Sélectionner "(Non applicable)"
            }
        }

        // Validation en temps réel de l'email
        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            UpdateEmailValidation(email);
        }

        private void UpdateEmailValidation(string email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                if (IsValidEmail(email))
                {
                    EmailValidationTextBlock.Text = "✅ Format d'email valide";
                    EmailValidationTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    EmailValidationTextBlock.Text = "❌ Format d'email invalide. Exemple: nom.prenom@hospital.com";
                    EmailValidationTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            else
            {
                EmailValidationTextBlock.Text = "Saisissez l'email de l'utilisateur";
                EmailValidationTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                // Expression régulière simple pour valider le format d'email
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation des champs obligatoires
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Veuillez saisir le prénom et le nom de l'utilisateur.",
                    "Champs manquants", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validation de l'email
            string email = EmailTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("L'adresse email est obligatoire.",
                    "Email manquant", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Format d'email invalide.\n\n" +
                               "Exemples acceptés:\n" +
                               "• jean.dupont@hospital.com\n" +
                               "• marie.durand@chu-paris.fr\n" +
                               "• admin@clinique-medicale.fr",
                    "Email invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validation du nom d'utilisateur
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                MessageBox.Show("Le nom d'utilisateur est obligatoire.",
                    "Nom d'utilisateur manquant", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validation du mot de passe
            if (string.IsNullOrWhiteSpace(PasswordTextBox.Text))
            {
                MessageBox.Show("Le mot de passe est obligatoire.",
                    "Mot de passe manquant", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validation de la force du mot de passe
            if (PasswordTextBox.Text.Length < 6)
            {
                MessageBox.Show("Le mot de passe doit contenir au moins 6 caractères.",
                    "Mot de passe trop court", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Vérifier si l'email existe déjà dans la base (sauf pour la modification)
            var users = DatabaseHelper.GetUsers();
            var existingUserWithEmail = users.FirstOrDefault(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                (this.Title != "Modifier Utilisateur" || // Si c'est un nouvel utilisateur
                 u.Email != EmailTextBox.Text)); // Ou si l'email a été modifié

            if (existingUserWithEmail != null)
            {
                MessageBox.Show($"L'email '{email}' est déjà utilisé par l'utilisateur:\n" +
                               $"{existingUserWithEmail.FirstName} {existingUserWithEmail.LastName}",
                    "Email déjà utilisé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Vérifier si le nom d'utilisateur existe déjà
            var username = UsernameTextBox.Text.Trim();
            var existingUserWithUsername = users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                (this.Title != "Modifier Utilisateur" ||
                 u.Username != UsernameTextBox.Text));

            if (existingUserWithUsername != null)
            {
                MessageBox.Show($"Le nom d'utilisateur '{username}' est déjà utilisé.",
                    "Nom d'utilisateur déjà utilisé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Si toutes les validations passent, enregistrer les données
            FirstName = FirstNameTextBox.Text.Trim();
            LastName = LastNameTextBox.Text.Trim();
            Email = email;
            Username = username;
            Password = PasswordTextBox.Text;
            Role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            // Gérer l'assignation au service pour les médecins
            if (Role == "Doctor")
            {
                if (ServiceComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    if (selectedItem.Tag is int serviceId && serviceId != -1)
                    {
                        ServiceId = serviceId;
                        ServiceName = selectedItem.Content.ToString();
                    }
                    else
                    {
                        MessageBox.Show("Veuillez sélectionner un service pour le médecin.",
                            "Service manquant", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }
            else
            {
                ServiceId = null;
                ServiceName = null;
            }

            // Confirmation avant enregistrement
            if (this.Title == "Modifier Utilisateur")
            {
                if (MessageBox.Show($"Confirmez-vous la modification de l'utilisateur ?\n\n" +
                                   $"Nom: {FirstName} {LastName}\n" +
                                   $"Email: {Email}\n" +
                                   $"Rôle: {Role}\n" +
                                   (ServiceName != null ? $"Service: {ServiceName}" : ""),
                                   "Confirmation de modification",
                                   MessageBoxButton.YesNo,
                                   MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Générer automatiquement le nom d'utilisateur à partir du nom et prénom
        private void GenerateUsernameButton_Click(object sender, RoutedEventArgs e)
        {
            string firstName = FirstNameTextBox.Text.Trim().ToLower();
            string lastName = LastNameTextBox.Text.Trim().ToLower();

            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
            {
                // Créer un nom d'utilisateur simple : prenom.nom
                string generatedUsername = $"{firstName}.{lastName}";

                // Vérifier si ce nom d'utilisateur existe déjà
                var users = DatabaseHelper.GetUsers();
                int counter = 1;
                string baseUsername = generatedUsername;

                while (users.Any(u => u.Username.Equals(generatedUsername, StringComparison.OrdinalIgnoreCase)))
                {
                    generatedUsername = $"{baseUsername}{counter}";
                    counter++;
                }

                UsernameTextBox.Text = generatedUsername;
            }
            else
            {
                MessageBox.Show("Veuillez d'abord saisir le prénom et le nom.",
                    "Informations manquantes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Générer un mot de passe sécurisé
        private void GeneratePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string generatedPassword = GenerateSecurePassword();
            PasswordTextBox.Text = generatedPassword;
            PasswordTextBox.Focus();
            PasswordTextBox.SelectAll();

            MessageBox.Show($"Mot de passe généré: {generatedPassword}\n\n" +
                           "Pensez à le communiquer à l'utilisateur.",
                           "Mot de passe généré", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GenerateSecurePassword()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}