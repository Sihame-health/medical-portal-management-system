using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MedicalSystem.Database;
using MedicalSystem.Models;

namespace MedicalSystem
{
    public partial class ReceptionWindow : Window
    {
        public ReceptionWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded; // Ajout du gestionnaire d'événements
            ShowRegisterPatient();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialisation supplémentaire si nécessaire
            Console.WriteLine("ReceptionWindow loaded successfully");
        }

        private void BtnRegisterPatient_Click(object sender, RoutedEventArgs e) => ShowRegisterPatient();
        private void BtnPatientList_Click(object sender, RoutedEventArgs e) => ShowPatientList();

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ShowRegisterPatient()
        {
            // Assurez-vous que ContentPanel existe
            if (ContentPanel == null)
            {
                MessageBox.Show("Erreur: ContentPanel n'est pas initialisé");
                return;
            }

            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = "Enregistrement d'un Nouveau Patient",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            Border formCard = new Border { Style = (Style)this.Resources["CardStyle"] };
            StackPanel formPanel = new StackPanel();

            // Champs simples
            TextBox firstNameBox = new TextBox
            {
                Height = 35,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                Name = "FirstNameBox"
            };
            TextBox lastNameBox = new TextBox
            {
                Height = 35,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                Name = "LastNameBox"
            };
            TextBox cinBox = new TextBox
            {
                Height = 35,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                Name = "CINBox"
            };
            TextBox ageBox = new TextBox
            {
                Height = 35,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                Name = "AgeBox"
            };
            TextBox phoneBox = new TextBox
            {
                Height = 35,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                Name = "PhoneBox"
            };
            TextBox historyBox = new TextBox
            {
                Height = 80,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Name = "HistoryBox"
            };

            // Labels
            formPanel.Children.Add(new TextBlock
            {
                Text = "Prénom *",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            formPanel.Children.Add(firstNameBox);

            formPanel.Children.Add(new TextBlock
            {
                Text = "Nom *",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            formPanel.Children.Add(lastNameBox);

            formPanel.Children.Add(new TextBlock
            {
                Text = "CIN / Passeport *",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            formPanel.Children.Add(cinBox);

            formPanel.Children.Add(new TextBlock
            {
                Text = "Âge *",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            formPanel.Children.Add(ageBox);

            formPanel.Children.Add(new TextBlock
            {
                Text = "Téléphone *",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            formPanel.Children.Add(phoneBox);

            formPanel.Children.Add(new TextBlock
            {
                Text = "Antécédents médicaux (optionnel)",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            formPanel.Children.Add(historyBox);

            // Service
            formPanel.Children.Add(new TextBlock
            {
                Text = "Service choisi *",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 20, 0, 10)
            });

            ComboBox serviceCombo = new ComboBox
            {
                Height = 40,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20),
                Name = "ServiceComboBox"
            };

            try
            {
                var services = DatabaseHelper.GetServices();
                foreach (var service in services)
                {
                    serviceCombo.Items.Add(new ComboBoxItem
                    {
                        Content = service.Name,
                        Tag = service.Id
                    });
                }
                if (serviceCombo.Items.Count > 0)
                    serviceCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des services: {ex.Message}");
            }

            formPanel.Children.Add(serviceCombo);

            // Bouton Enregistrer
            Button registerBtn = new Button
            {
                Content = "Enregistrer",
                Style = (Style)this.Resources["SuccessButton"],
                Width = 120,
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Name = "RegisterButton"
            };

            registerBtn.Click += (s, ev) =>
            {
                // Validation simple
                if (string.IsNullOrWhiteSpace(firstNameBox.Text) ||
                    string.IsNullOrWhiteSpace(lastNameBox.Text) ||
                    string.IsNullOrWhiteSpace(cinBox.Text) ||
                    string.IsNullOrWhiteSpace(phoneBox.Text) ||
                    string.IsNullOrWhiteSpace(ageBox.Text))
                {
                    MessageBox.Show("Veuillez remplir tous les champs obligatoires (*)",
                                  "Erreur de validation",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(ageBox.Text, out int age) || age <= 0 || age > 150)
                {
                    MessageBox.Show("Âge invalide. Veuillez entrer un âge entre 1 et 150 ans.",
                                  "Erreur de validation",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                // Validation téléphone
                if (!phoneBox.Text.All(char.IsDigit) || phoneBox.Text.Length < 8)
                {
                    MessageBox.Show("Numéro de téléphone invalide",
                                  "Erreur de validation",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // Créer patient
                    var patient = new Patient
                    {
                        FirstName = firstNameBox.Text.Trim(),
                        LastName = lastNameBox.Text.Trim(),
                        CIN = cinBox.Text.Trim(),
                        Age = age,
                        Phone = phoneBox.Text.Trim(),
                        MedicalHistory = historyBox.Text.Trim(),
                        ServiceId = (serviceCombo.SelectedItem as ComboBoxItem)?.Tag as int? ?? 1,
                        ServiceName = (serviceCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Général",
                        RegistrationDate = DateTime.Now,
                        Status = "En attente"
                    };

                    DatabaseHelper.AddPatient(patient);
                    MessageBox.Show($"Patient {patient.FullName} enregistré avec succès!",
                                  "Succès",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    // Effacer les champs
                    firstNameBox.Text = "";
                    lastNameBox.Text = "";
                    cinBox.Text = "";
                    ageBox.Text = "";
                    phoneBox.Text = "";
                    historyBox.Text = "";

                    // Remettre le focus sur le premier champ
                    firstNameBox.Focus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}",
                                  "Erreur",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            };

            formPanel.Children.Add(registerBtn);
            formCard.Child = formPanel;
            ContentPanel.Children.Add(formCard);
        }

        private void ShowPatientList()
        {
            // Assurez-vous que ContentPanel existe
            if (ContentPanel == null)
            {
                MessageBox.Show("Erreur: ContentPanel n'est pas initialisé");
                return;
            }

            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = "Liste des Patients",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            try
            {
                var patients = DatabaseHelper.GetPatients();

                if (patients == null || patients.Count == 0)
                {
                    ContentPanel.Children.Add(new TextBlock
                    {
                        Text = "Aucun patient enregistré",
                        FontSize = 16,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 50, 0, 0)
                    });
                    return;
                }

                // Créer un ScrollViewer pour la liste
                ScrollViewer scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Height = 400
                };

                StackPanel patientsPanel = new StackPanel();

                // Liste simple
                foreach (var patient in patients)
                {
                    Border card = new Border
                    {
                        Style = (Style)this.Resources["CardStyle"],
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    StackPanel panel = new StackPanel();

                    panel.Children.Add(new TextBlock
                    {
                        Text = $"{patient.FullName} - {patient.Age} ans",
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold
                    });

                    panel.Children.Add(new TextBlock
                    {
                        Text = $"CIN: {patient.CIN} • Tél: {patient.Phone}",
                        FontSize = 14,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(0, 2, 0, 0)
                    });

                    panel.Children.Add(new TextBlock
                    {
                        Text = $"Service: {patient.ServiceName} • Statut: {patient.Status}",
                        FontSize = 14,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(0, 2, 0, 0)
                    });

                    panel.Children.Add(new TextBlock
                    {
                        Text = $"Enregistré le: {patient.RegistrationDate:dd/MM/yyyy HH:mm}",
                        FontSize = 12,
                        Foreground = Brushes.DarkGray,
                        Margin = new Thickness(0, 5, 0, 0)
                    });

                    card.Child = panel;
                    patientsPanel.Children.Add(card);
                }

                scrollViewer.Content = patientsPanel;
                ContentPanel.Children.Add(scrollViewer);
            }
            catch (Exception ex)
            {
                ContentPanel.Children.Add(new TextBlock
                {
                    Text = $"Erreur lors du chargement des patients: {ex.Message}",
                    FontSize = 14,
                    Foreground = Brushes.Red,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                });
            }
        }
    }
}