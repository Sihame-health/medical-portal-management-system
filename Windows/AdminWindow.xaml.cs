using MedicalSystem.Database;
using MedicalSystem.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MedicalSystem
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            this.KeyDown += AdminWindow_KeyDown;
            this.PreviewKeyDown += AdminWindow_PreviewKeyDown;
            LoadDashboardData();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainTabControl?.Focus();
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || MainTabControl == null) return;
            if (e.Source != MainTabControl) return;
            ScrollCurrentViewToTop();
        }

        private void ScrollCurrentViewToTop()
        {
            try
            {
                int idx = MainTabControl.SelectedIndex;
                if (idx == 0 && UsersDataGrid != null && UsersDataGrid.Items.Count > 0)
                    UsersDataGrid.ScrollIntoView(UsersDataGrid.Items[0]);
                else if (idx == 1 && ServicesDataGrid != null && ServicesDataGrid.Items.Count > 0)
                    ServicesDataGrid.ScrollIntoView(ServicesDataGrid.Items[0]);
                else if (idx == 2 && MedicationsDataGrid != null && MedicationsDataGrid.Items.Count > 0)
                    MedicationsDataGrid.ScrollIntoView(MedicationsDataGrid.Items[0]);
                else if (idx == 3 && DashboardScrollViewer != null)
                    DashboardScrollViewer.ScrollToTop();
            }
            catch { /* keep UX smooth */ }
        }

        private void AdminWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                LogoutButton_Click(sender, e);
                e.Handled = true;
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.D1 || e.Key == Key.NumPad1)
                {
                    MainTabControl.SelectedIndex = 0;
                    ScrollCurrentViewToTop();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                {
                    MainTabControl.SelectedIndex = 1;
                    ScrollCurrentViewToTop();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                {
                    MainTabControl.SelectedIndex = 2;
                    ScrollCurrentViewToTop();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D4 || e.Key == Key.NumPad4)
                {
                    MainTabControl.SelectedIndex = 3;
                    ScrollCurrentViewToTop();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.R) { RefreshUsersButton_Click(sender, e); e.Handled = true; return; }
                if (e.Key == Key.S) { ExportUsersButton_Click(sender, e); e.Handled = true; return; }
                if (e.Key == Key.I) { ImportUsersButton_Click(sender, e); e.Handled = true; return; }
                if (e.Key == Key.N) { AddUserButton_Click(sender, e); e.Handled = true; return; }
            }
        }

        private void AdminWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab) return;
            if (IsVisualDescendantOf<DataGrid>(Keyboard.FocusedElement as DependencyObject))
                return;

            if (Keyboard.FocusedElement is UIElement focused)
            {
                var direction = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift
                    ? FocusNavigationDirection.Previous
                    : FocusNavigationDirection.Next;

                if (focused.MoveFocus(new TraversalRequest(direction)))
                    e.Handled = true;
            }
        }

        private static bool IsVisualDescendantOf<T>(DependencyObject? element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T) return true;
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        private void LoadDashboardData()
        {
            LoadUsersData();
            LoadServicesData();
            LoadMedicationsData();
            LoadStatistics();
            LoadActivities();

            LastUpdateText.Text = $"Dernière mise à jour: {DateTime.Now:dd/MM/yyyy HH:mm}";
        }

        private void LoadUsersData()
        {
            var users = DatabaseHelper.GetUsers();
            UsersDataGrid.ItemsSource = null;
            UsersDataGrid.ItemsSource = users;
        }

        private void LoadServicesData()
        {
            var services = DatabaseHelper.GetServices();
            ServicesDataGrid.ItemsSource = null;
            ServicesDataGrid.ItemsSource = services;
        }

        private void LoadMedicationsData()
        {
            var medications = DatabaseHelper.GetMedications();
            MedicationsDataGrid.ItemsSource = null;
            MedicationsDataGrid.ItemsSource = medications;
        }

        private void LoadStatistics()
        {
            var users = DatabaseHelper.GetUsers();
            var services = DatabaseHelper.GetServices();
            var medications = DatabaseHelper.GetMedications();
            var patients = DatabaseHelper.GetPatients();

            UsersCountText.Text = users.Count.ToString();
            ServicesCountText.Text = services.Count.ToString();
            MedicationsCountText.Text = medications.Count.ToString();

            int lowStockCount = medications.Count(m => m.Quantity < m.MinThreshold);
            int expiringSoonCount = medications.Count(m => (m.ExpirationDate - DateTime.Now).TotalDays < 30);
            AlertsCountText.Text = (lowStockCount + expiringSoonCount).ToString();
        }

        private void LoadActivities()
        {
            var activities = DatabaseHelper.GetActivities()
                .OrderByDescending(a => a.DateTime)
                .ToList();
            ActivitiesDataGrid.ItemsSource = null;
            ActivitiesDataGrid.ItemsSource = activities;
        }

        // ============ USERS ============
        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditUserDialog();
            if (dialog.ShowDialog() == true)
            {
                var users = DatabaseHelper.GetUsers();
                int newId = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;

                var newUser = new User
                {
                    Id = newId,
                    FirstName = dialog.FirstName,
                    LastName = dialog.LastName,
                    Email = dialog.Email,
                    Username = dialog.Username,
                    Password = dialog.Password,
                    Role = dialog.Role,
                    Status = dialog.Status,
                    LastLogin = "Jamais",
                    ServiceId = dialog.ServiceId,
                    ServiceName = dialog.ServiceName
                };

                users.Add(newUser);
                DatabaseHelper.SaveUsers(users);

                // Mettre à jour le compteur de médecins dans le service
                if (dialog.Role == "Doctor" && dialog.ServiceId.HasValue)
                {
                    var services = DatabaseHelper.GetServices();
                    var service = services.FirstOrDefault(s => s.Id == dialog.ServiceId.Value);
                    if (service != null)
                    {
                        service.DoctorCount++;
                        DatabaseHelper.SaveServices(services);
                    }
                }

                LoadUsersData();
                LoadServicesData(); // Recharger pour voir les comptes mis à jour
                LoadStatistics();

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = "Admin System",
                    Action = "Ajout utilisateur",
                    Details = $"Nouvel utilisateur: {dialog.FirstName} {dialog.LastName} ({dialog.Role})" +
                             (dialog.ServiceName != null ? $" - Service: {dialog.ServiceName}" : "")
                });

                MessageBox.Show($"Utilisateur {dialog.FirstName} {dialog.LastName} ajouté avec succès!" +
                               (dialog.ServiceName != null ? $"\nService: {dialog.ServiceName}" : ""),
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                var users = DatabaseHelper.GetUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    // Stocker l'ancien service pour mettre à jour le compteur
                    int? oldServiceId = user.ServiceId;

                    var dialog = new AddEditUserDialog(user);
                    if (dialog.ShowDialog() == true)
                    {
                        // Mettre à jour le compteur de médecins si le service a changé
                        if (user.Role == "Doctor")
                        {
                            var services = DatabaseHelper.GetServices();

                            // Décrémenter l'ancien service
                            if (oldServiceId.HasValue && oldServiceId != dialog.ServiceId)
                            {
                                var oldService = services.FirstOrDefault(s => s.Id == oldServiceId.Value);
                                if (oldService != null && oldService.DoctorCount > 0)
                                {
                                    oldService.DoctorCount--;
                                }
                            }

                            // Incrémenter le nouveau service
                            if (dialog.ServiceId.HasValue && oldServiceId != dialog.ServiceId)
                            {
                                var newService = services.FirstOrDefault(s => s.Id == dialog.ServiceId.Value);
                                if (newService != null)
                                {
                                    newService.DoctorCount++;
                                }
                            }

                            DatabaseHelper.SaveServices(services);
                        }

                        user.FirstName = dialog.FirstName;
                        user.LastName = dialog.LastName;
                        user.Email = dialog.Email;
                        user.Username = dialog.Username;
                        user.Password = dialog.Password;
                        user.Role = dialog.Role;
                        user.Status = dialog.Status;
                        user.ServiceId = dialog.ServiceId;
                        user.ServiceName = dialog.ServiceName;

                        DatabaseHelper.SaveUsers(users);
                        LoadUsersData();
                        LoadServicesData(); // Recharger les services pour voir les comptes mis à jour

                        DatabaseHelper.AddActivity(new Activity
                        {
                            DateTime = DateTime.Now,
                            User = "Admin System",
                            Action = "Modification utilisateur",
                            Details = $"Utilisateur modifié: {user.FirstName} {user.LastName}" +
                                     (user.ServiceName != null ? $" - Service: {user.ServiceName}" : "")
                        });

                        MessageBox.Show("Utilisateur modifié avec succès!" +
                                      (user.ServiceName != null ? $"\nService: {user.ServiceName}" : ""),
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                var users = DatabaseHelper.GetUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    if (MessageBox.Show($"Voulez-vous réinitialiser le mot de passe de {user.FirstName} {user.LastName}?\n\nLe nouveau mot de passe sera: Pass1234",
                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        user.Password = "Pass1234";
                        DatabaseHelper.SaveUsers(users);

                        DatabaseHelper.AddActivity(new Activity
                        {
                            DateTime = DateTime.Now,
                            User = "Admin System",
                            Action = "Réinitialisation mot de passe",
                            Details = $"Utilisateur: {user.FirstName} {user.LastName}"
                        });

                        MessageBox.Show("Mot de passe réinitialisé avec succès!\nNouveau mot de passe: Pass1234",
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

      /*  private void ActivateUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                var users = DatabaseHelper.GetUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                if (user != null && user.Status != "Actif")
                {
                    user.Status = "Actif";
                    DatabaseHelper.SaveUsers(users);
                    LoadUsersData();

                    DatabaseHelper.AddActivity(new Activity
                    {
                        DateTime = DateTime.Now,
                        User = "Admin System",
                        Action = "Activation utilisateur",
                        Details = $"Utilisateur activé: {user.FirstName} {user.LastName}"
                    });

                    MessageBox.Show($"Utilisateur {user.FirstName} {user.LastName} activé avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DeactivateUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                var users = DatabaseHelper.GetUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                if (user != null && user.Status != "Inactif")
                {
                    user.Status = "Inactif";
                    DatabaseHelper.SaveUsers(users);
                    LoadUsersData();
                    LoadStatistics();

                    DatabaseHelper.AddActivity(new Activity
                    {
                        DateTime = DateTime.Now,
                        User = "Admin System",
                        Action = "Désactivation utilisateur",
                        Details = $"Utilisateur désactivé: {user.FirstName} {user.LastName}"
                    });

                    MessageBox.Show($"Utilisateur {user.FirstName} {user.LastName} désactivé avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }*/

        private void RefreshUsersButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUsersData();
            DatabaseHelper.AddActivity(new Activity
            {
                DateTime = DateTime.Now,
                User = "Admin System",
                Action = "Actualisation",
                Details = "Liste des utilisateurs actualisée"
            });

            MessageBox.Show("Liste des utilisateurs actualisée",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImportUsersButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
                Title = "Importer des utilisateurs depuis CSV"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string[] lines = File.ReadAllLines(openFileDialog.FileName);
                    var users = DatabaseHelper.GetUsers();
                    int importedCount = 0;
                    int skippedCount = 0;
                    var errors = new List<string>();

                    foreach (string line in lines.Skip(1))
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length >= 4)
                        {
                            string firstName = parts[0].Trim();
                            string lastName = parts[1].Trim();
                            string email = parts[2].Trim();
                            string role = parts[3].Trim();

                            // Validation de l'email
                            if (!IsValidEmail(email))
                            {
                                errors.Add($"Ligne {importedCount + skippedCount + 2}: Email invalide '{email}' pour {firstName} {lastName}");
                                skippedCount++;
                                continue;
                            }

                            // Vérifier si l'email existe déjà
                            if (users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                            {
                                errors.Add($"Ligne {importedCount + skippedCount + 2}: Email '{email}' déjà utilisé");
                                skippedCount++;
                                continue;
                            }

                            int newId = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;

                            users.Add(new User
                            {
                                Id = newId,
                                FirstName = firstName,
                                LastName = lastName,
                                Email = email,
                                Username = GenerateUsername(firstName, lastName, users),
                                Password = "Pass1234",
                                Role = role,
                                Status = "Actif",
                                LastLogin = "Jamais"
                            });

                            importedCount++;
                        }
                    }

                    if (importedCount > 0)
                    {
                        DatabaseHelper.SaveUsers(users);
                        LoadUsersData();
                        LoadStatistics();

                        DatabaseHelper.AddActivity(new Activity
                        {
                            DateTime = DateTime.Now,
                            User = "Admin System",
                            Action = "Importation CSV",
                            Details = $"{importedCount} utilisateurs importés, {skippedCount} ignorés"
                        });

                        string message = $"{importedCount} utilisateurs importés avec succès!";
                        if (skippedCount > 0)
                            message += $"\n{skippedCount} utilisateurs ignorés (erreurs de validation).";

                        if (errors.Any())
                        {
                            message += "\n\nErreurs détectées:\n" + string.Join("\n", errors.Take(5));
                            if (errors.Count > 5)
                                message += $"\n... et {errors.Count - 5} autres erreurs";
                        }

                        MessageBox.Show(message,
                            "Importation terminée", MessageBoxButton.OK,
                            skippedCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Aucun utilisateur valide trouvé dans le fichier.",
                            "Importation échouée", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'importation: {ex.Message}\n\n" +
                                   "Format CSV attendu: Prénom,Nom,Email,Rôle\n" +
                                   "Exemple: Jean,Dupont,jean.dupont@hospital.com,Médecin",
                                   "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Méthode helper pour valider les emails
        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        // Méthode helper pour générer un nom d'utilisateur unique
        private string GenerateUsername(string firstName, string lastName, List<User> existingUsers)
        {
            string baseUsername = $"{firstName.ToLower()}.{lastName.ToLower()}";
            string username = baseUsername;
            int counter = 1;

            while (existingUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username;
        }
        private void ExportUsersButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Fichiers CSV (*.csv)|*.csv",
                Title = "Exporter les utilisateurs",
                FileName = $"utilisateurs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var users = DatabaseHelper.GetUsers();
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("Prénom,Nom,Email,Rôle,Service,Statut,DernièreConnexion");

                        foreach (var user in users)
                            writer.WriteLine($"{user.FirstName},{user.LastName},{user.Email},{user.Role},{user.ServiceName ?? ""},{user.Status},{user.LastLogin}");
                    }

                    DatabaseHelper.AddActivity(new Activity
                    {
                        DateTime = DateTime.Now,
                        User = "Admin System",
                        Action = "Exportation CSV",
                        Details = $"Utilisateurs exportés: {users.Count}"
                    });

                    MessageBox.Show($"{users.Count} utilisateurs exportés avec succès dans:\n{saveFileDialog.FileName}",
                        "Exportation réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'exportation: {ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ============ SERVICES ============
        private void AddServiceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditServiceDialog();
            if (dialog.ShowDialog() == true)
            {
                var services = DatabaseHelper.GetServices();
                int newId = services.Count > 0 ? services.Max(s => s.Id) + 1 : 1;

                services.Add(new Service
                {
                    Id = newId,
                    Name = dialog.ServiceName,
                    Description = dialog.Description,
                    DoctorCount = 0,
                    Status = dialog.Status
                });

                DatabaseHelper.SaveServices(services);
                LoadServicesData();
                LoadStatistics();

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = "Admin System",
                    Action = "Ajout service",
                    Details = $"Nouveau service: {dialog.ServiceName}"
                });

                MessageBox.Show($"Service {dialog.ServiceName} ajouté avec succès!",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ============ MEDICATIONS ============
        private void AddMedicationButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditMedicationDialog();
            if (dialog.ShowDialog() == true)
            {
                var medications = DatabaseHelper.GetMedications();

                medications.Add(new Medication
                {
                    Code = dialog.Code,
                    Name = dialog.Name,
                    Description = dialog.Description,
                    Quantity = dialog.Quantity,
                    MinThreshold = dialog.MinThreshold,
                    ExpirationDate = dialog.ExpirationDate,
                    Status = dialog.Quantity < dialog.MinThreshold ? "Faible stock" : "Disponible"
                });

                DatabaseHelper.SaveMedications(medications);
                LoadMedicationsData();
                LoadStatistics();

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = "Admin System",
                    Action = "Ajout médicament",
                    Details = $"Nouveau médicament: {dialog.Name}"
                });

                MessageBox.Show($"Médicament {dialog.Name} ajouté avec succès!",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImportMedicationsButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers CSV (*.csv)|*.csv",
                Title = "Importer des médicaments depuis CSV"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string[] lines = File.ReadAllLines(openFileDialog.FileName);
                    var medications = DatabaseHelper.GetMedications();
                    int importedCount = 0;

                    foreach (string line in lines.Skip(1))
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length >= 6)
                        {
                            int qty = int.Parse(parts[3].Trim());
                            int thr = int.Parse(parts[4].Trim());

                            medications.Add(new Medication
                            {
                                Code = parts[0].Trim(),
                                Name = parts[1].Trim(),
                                Description = parts[2].Trim(),
                                Quantity = qty,
                                MinThreshold = thr,
                                ExpirationDate = DateTime.Parse(parts[5].Trim()),
                                Status = qty < thr ? "Faible stock" : "Disponible"
                            });

                            importedCount++;
                        }
                    }

                    DatabaseHelper.SaveMedications(medications);
                    LoadMedicationsData();
                    LoadStatistics();

                    DatabaseHelper.AddActivity(new Activity
                    {
                        DateTime = DateTime.Now,
                        User = "Admin System",
                        Action = "Importation médicaments",
                        Details = $"{importedCount} médicaments importés"
                    });

                    MessageBox.Show($"{importedCount} médicaments importés avec succès!",
                        "Importation réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'importation: {ex.Message}\n\n" +
                                   "Format CSV attendu: Code,Nom,Description,Quantité,Seuil,DateExpiration\n" +
                                   "Exemple: PARA500,Paracétamol,Antidouleur,100,50,2025-12-31",
                                   "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ============ LOGOUT ============
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Voulez-vous vraiment vous déconnecter?", "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        // In Windows/AdminWindow.xaml.cs, inside the AdminWindow class

        private void ResetTestDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Êtes-vous sûr de vouloir supprimer TOUTES les données de test?\n\n" +
                               "Cela supprimera:\n" +
                               "• Tous les patients\n" +
                               "• Toutes les prescriptions\n" +
                               "• Toutes les notifications\n" +
                               "• Toutes les activités\n\n" +
                               "Les utilisateurs, services et médicaments seront conservés.",
                               "⚠️ CONFIRMATION DE RÉINITIALISATION",
                               MessageBoxButton.YesNo,
                               MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                // Réinitialiser les patients
                File.WriteAllText(DatabaseHelper.GetPatientsFilePath(), "[]");

                // Réinitialiser les prescriptions
                File.WriteAllText(DatabaseHelper.GetPrescriptionsFilePath(), "[]");

                // Réinitialiser les notifications
                File.WriteAllText(DatabaseHelper.GetNotificationsFilePath(), "[]");

                // Réinitialiser les activités
                File.WriteAllText(DatabaseHelper.GetActivitiesFilePath(), "[]");

                // Recharger les données
                LoadDashboardData();

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = "Admin System",
                    Action = "Réinitialisation données test",
                    Details = "Toutes les données de test ont été supprimées (patients, prescriptions, notifications, activités)"
                });

                MessageBox.Show("✅ Données de test réinitialisées avec succès!\n\n" +
                               "Le système a été nettoyé et est prêt pour une nouvelle démonstration.",
                               "Réinitialisation réussie",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la réinitialisation: {ex.Message}",
                               "Erreur",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void ResetAllDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("🚨 RÉINITIALISATION COMPLÈTE DU SYSTÈME!\n\n" +
                               "Cette action va:\n" +
                               "1. Supprimer TOUTES les données (patients, prescriptions, etc.)\n" +
                               "2. Réinitialiser les médicaments aux valeurs par défaut\n" +
                               "3. Réinitialiser les services aux valeurs par défaut\n" +
                               "4. Garder SEULEMENT l'utilisateur admin\n\n" +
                               "Êtes-vous ABSOLUMENT sûr?",
                               "🚨 CONFIRMATION CRITIQUE",
                               MessageBoxButton.YesNo,
                               MessageBoxImage.Error) != MessageBoxResult.Yes)
            {
                return;
            }

            // Demander une confirmation supplémentaire
            if (MessageBox.Show("Dernière chance! Tapez 'RESET' pour confirmer:",
                               "Confirmation finale",
                               MessageBoxButton.OKCancel,
                               MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                var inputDialog = new InputDialog("Confirmation", "Tapez 'RESET' pour confirmer la réinitialisation complète:");
                if (inputDialog.ShowDialog() == true && inputDialog.ResponseText == "RESET")
                {
                    PerformCompleteReset();
                }
            }
        }

        private void PerformCompleteReset()
        {
            try
            {
                // 1. Garder seulement l'admin dans users.json
                var defaultUsers = new List<User>
        {
            new User { Id = 1, FirstName = "Admin", LastName = "System", Username = "admin",
                       Password = "admin123", Email = "admin@hospital.com", Role = "Admin", Status = "Actif" }
        };
                File.WriteAllText(DatabaseHelper.GetUsersFilePath(),
                                 JsonSerializer.Serialize(defaultUsers, new JsonSerializerOptions { WriteIndented = true }));

                // 2. Réinitialiser les services
                var defaultServices = new List<Service>
        {
            new Service { Id = 1, Name = "Cardiologie", Description = "Service des maladies cardiaques", DoctorCount = 0, Status = "Actif" },
            new Service { Id = 2, Name = "Pédiatrie", Description = "Service pour enfants", DoctorCount = 0, Status = "Actif" },
            new Service { Id = 3, Name = "Chirurgie", Description = "Service de chirurgie générale", DoctorCount = 0, Status = "Actif" },
            new Service { Id = 4, Name = "Urgences", Description = "Service d'urgence 24h/24", DoctorCount = 0, Status = "Actif" }
        };
                File.WriteAllText(DatabaseHelper.GetServicesFilePath(),
                                 JsonSerializer.Serialize(defaultServices, new JsonSerializerOptions { WriteIndented = true }));

                // 3. Réinitialiser les médicaments
                var defaultMedications = new List<Medication>
        {
            new Medication { Code = "PARA500", Name = "Paracétamol", Description = "Antidouleur et antipyrétique",
                             Quantity = 150, MinThreshold = 50, ExpirationDate = DateTime.Now.AddMonths(6), Status = "Disponible" },
            new Medication { Code = "IBUP400", Name = "Ibuprofène", Description = "Anti-inflammatoire",
                             Quantity = 80, MinThreshold = 90, ExpirationDate = DateTime.Now.AddDays(15), Status = "Faible stock" },
            new Medication { Code = "AMOX1G", Name = "Amoxicilline", Description = "Antibiotique",
                             Quantity = 45, MinThreshold = 20, ExpirationDate = DateTime.Now.AddMonths(3), Status = "Disponible" }
        };
                File.WriteAllText(DatabaseHelper.GetMedicationsFilePath(),
                                 JsonSerializer.Serialize(defaultMedications, new JsonSerializerOptions { WriteIndented = true }));

                // 4. Vider les autres fichiers
                File.WriteAllText(DatabaseHelper.GetPatientsFilePath(), "[]");
                File.WriteAllText(DatabaseHelper.GetPrescriptionsFilePath(), "[]");
                File.WriteAllText(DatabaseHelper.GetNotificationsFilePath(), "[]");
                File.WriteAllText(DatabaseHelper.GetActivitiesFilePath(), "[]");

                // 5. Ajouter une activité de réinitialisation
                var resetActivity = new Activity
                {
                    Id = 1,
                    DateTime = DateTime.Now,
                    User = "Admin System",
                    Action = "Réinitialisation complète du système",
                    Details = "Toutes les données ont été réinitialisées aux valeurs par défaut"
                };
                File.WriteAllText(DatabaseHelper.GetActivitiesFilePath(),
                                 JsonSerializer.Serialize(new List<Activity> { resetActivity }, new JsonSerializerOptions { WriteIndented = true }));

                // 6. Recharger l'interface
                LoadDashboardData();

                MessageBox.Show("✅ Système complètement réinitialisé!\n\n" +
                               "Le système est maintenant dans son état initial avec:\n" +
                               "• 1 utilisateur admin\n" +
                               "• 4 services\n" +
                               "• 3 médicaments\n" +
                               "• Aucun patient, prescription ou notification\n\n" +
                               "Prêt pour une nouvelle démonstration!",
                               "Réinitialisation complète réussie",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la réinitialisation: {ex.Message}",
                               "Erreur critique",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }
        // ============ ACTIVATION/DÉSACTIVATION UTILISATEURS ============

        private void ActivateUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                var users = DatabaseHelper.GetUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                if (user != null && user.Status != "Actif")
                {
                    // Confirmation d'activation
                    if (MessageBox.Show($"Voulez-vous vraiment activer l'utilisateur :\n\n" +
                                       $"👤 {user.FirstName} {user.LastName}\n" +
                                       $"📧 {user.Email}\n" +
                                       $"🎭 Rôle : {user.Role}",
                        "Confirmation d'activation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        user.Status = "Actif";
                        DatabaseHelper.SaveUsers(users);
                        LoadUsersData();

                        DatabaseHelper.AddActivity(new Activity
                        {
                            DateTime = DateTime.Now,
                            User = "Admin System",
                            Action = "Activation utilisateur",
                            Details = $"Utilisateur activé: {user.FirstName} {user.LastName}"
                        });

                        MessageBox.Show($"✅ Utilisateur {user.FirstName} {user.LastName} activé avec succès!",
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void DeactivateUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                var users = DatabaseHelper.GetUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                if (user != null && user.Status != "Inactif")
                {
                    // Confirmation de désactivation
                    string message = $"Voulez-vous vraiment désactiver l'utilisateur :\n\n";
                    message += $"👤 {user.FirstName} {user.LastName}\n";
                    message += $"📧 {user.Email}\n";
                    message += $"🎭 Rôle : {user.Role}\n";

                    if (user.ServiceName != null)
                    {
                        message += $"🏥 Service : {user.ServiceName}\n";
                    }

                    message += $"\n⚠️ Conséquences :\n";
                    message += $"• L'utilisateur ne pourra plus se connecter\n";
                    message += $"• Toutes ses sessions actives seront terminées\n";

                    if (user.Role == "Doctor")
                    {
                        message += $"\n📊 Pour les médecins :\n";
                        message += $"• Les patients assignés resteront actifs\n";
                        message += $"• Les prescriptions existantes seront conservées\n";
                    }

                    var result = MessageBox.Show(message,
                        "⚠️ CONFIRMATION DE DÉSACTIVATION",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        user.Status = "Inactif";
                        DatabaseHelper.SaveUsers(users);
                        LoadUsersData();
                        LoadStatistics();

                        DatabaseHelper.AddActivity(new Activity
                        {
                            DateTime = DateTime.Now,
                            User = "Admin System",
                            Action = "Désactivation utilisateur",
                            Details = $"Utilisateur désactivé: {user.FirstName} {user.LastName} ({user.Role})"
                        });

                        MessageBox.Show($"✅ Utilisateur {user.FirstName} {user.LastName} désactivé avec succès!\n\n" +
                                       "Statut : ❌ Inactif\n" +
                                       "L'utilisateur ne peut plus se connecter au système.",
                            "Désactivation réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // L'utilisateur a annulé
                        MessageBox.Show("❌ Désactivation annulée.\nL'utilisateur reste actif.",
                            "Annulation", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else if (user != null && user.Status == "Inactif")
                {
                    MessageBox.Show($"L'utilisateur {user.FirstName} {user.LastName} est déjà inactif.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

    }
}