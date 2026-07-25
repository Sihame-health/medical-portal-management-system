using MedicalSystem.Database;
using MedicalSystem.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MedicalSystem
{
    public partial class NurseWindow : Window
    {
        private User currentUser;

        public NurseWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            NurseNameText.Text = "NURSE PORTAL";
            NurseInfoText.Text = $"Inf. {user.LastName}";
            // Ajouter l'événement pour les raccourcis
            this.PreviewKeyDown += Window_PreviewKeyDown;
            ShowMyPatients();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BtnMyPatients?.Focus();
        }

        private void BtnMyPatients_Click(object sender, RoutedEventArgs e) => ShowMyPatients();
        private void BtnMedicationRequests_Click(object sender, RoutedEventArgs e) => ShowMedicationRequests();
        private void BtnNotifications_Click(object sender, RoutedEventArgs e) => ShowNotifications();

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ShowMyPatients()
        {
            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = "Patients Assignés à Moi",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            var patients = DatabaseHelper.GetPatients()
                .Where(p => p.AssignedNurseId == currentUser.Id || p.Status == "Prescription créée")
                .ToList();

            if (patients.Count == 0)
            {
                TextBlock emptyText = new TextBlock
                {
                    Text = "Aucun patient assigné pour le moment",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                ContentPanel.Children.Add(emptyText);
                return;
            }

            foreach (var patient in patients)
            {
                ContentPanel.Children.Add(CreatePatientCard(patient));
            }
        }

        private Border CreatePatientCard(Patient patient)
        {
            Border card = new Border
            {
                Style = (Style)this.Resources["CardStyle"],
                Margin = new Thickness(0, 0, 0, 10)
            };

            StackPanel mainPanel = new StackPanel();

            // Patient info
            TextBlock nameText = new TextBlock
            {
                Text = patient.FullName,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            mainPanel.Children.Add(nameText);

            TextBlock detailsText = new TextBlock
            {
                Text = $"Âge: {patient.Age} ans • Chambre: {patient.RoomNumber} • Service: {patient.ServiceName}",
                FontSize = 14,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(detailsText);

            // Status
            Border statusBadge = new Border
            {
                Background = GetStatusColor(patient.Status),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 10)
            };
            TextBlock statusText = new TextBlock
            {
                Text = $"Statut: {patient.Status}",
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            statusBadge.Child = statusText;
            mainPanel.Children.Add(statusBadge);

            // Buttons
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Nouveaux boutons pour l'infirmier
            if (patient.AssignedNurseId == currentUser.Id || patient.AssignedDoctorId.HasValue)
            {
                // Bouton assigner chambre
                if (patient.RoomNumber == 0)
                {
                    Button assignRoomBtn = new Button
                    {
                        Content = "🏥 Assigner chambre",
                        Style = (Style)this.Resources["InfoButton"],
                        Width = 160,
                        Margin = new Thickness(0, 0, 10, 0),
                        Tag = patient.Id
                    };
                    assignRoomBtn.Click += (s, e) => ShowAssignRoom(patient.Id);
                    buttonPanel.Children.Add(assignRoomBtn);
                }

                // Bouton envoyer remarque au médecin
                if (patient.AssignedDoctorId.HasValue)
                {
                    Button sendRemarkBtn = new Button
                    {
                        Content = "💬 Remarque au médecin",
                        Style = (Style)this.Resources["WarningButton"],
                        Width = 200,
                        Margin = new Thickness(0, 0, 10, 0),
                        Tag = patient.Id
                    };
                    sendRemarkBtn.Click += (s, e) => SendRemarkToDoctor(patient.Id);
                    buttonPanel.Children.Add(sendRemarkBtn);
                }
            }

            // Check if patient has a prescription
            var prescription = DatabaseHelper.GetPrescriptions()
                .FirstOrDefault(p => p.PatientId == patient.Id && p.Status != "Administrée");

            if (prescription != null)
            {
                if (prescription.Status == "Prête")
                {
                    Button administerBtn = new Button
                    {
                        Content = "💉 Administrer le traitement",
                        Style = (Style)this.Resources["SuccessButton"],
                        Width = 200,
                        Margin = new Thickness(0, 0, 10, 0),
                        Tag = patient.Id
                    };
                    administerBtn.Click += (s, e) => AdministerTreatment(patient.Id, prescription.Id);
                    buttonPanel.Children.Add(administerBtn);
                }
                else
                {
                    TextBlock waitingText = new TextBlock
                    {
                        Text = "⏳ En attente des médicaments de la pharmacie",
                        FontSize = 14,
                        Foreground = Brushes.Orange,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    buttonPanel.Children.Add(waitingText);
                }

                Button viewPrescriptionBtn = new Button
                {
                    Content = "📋 Voir prescription",
                    Style = (Style)this.Resources["InfoButton"],
                    Width = 140,
                    Margin = new Thickness(10, 0, 0, 0),
                    Tag = prescription.Id
                };
                viewPrescriptionBtn.Click += (s, e) => ViewPrescription(prescription.Id);
                buttonPanel.Children.Add(viewPrescriptionBtn);
            }
            else
            {
                Button assignBtn = new Button
                {
                    Content = "👤 Prendre en charge",
                    Style = (Style)this.Resources["PrimaryButton"],
                    Width = 160,
                    Tag = patient.Id
                };
                assignBtn.Click += (s, e) => TakeChargePatient(patient.Id);
                buttonPanel.Children.Add(assignBtn);
            }

            mainPanel.Children.Add(buttonPanel);
            card.Child = mainPanel;
            return card;
        }

        private void ShowAssignRoom(int patientId)
        {
            var patient = DatabaseHelper.GetPatients().FirstOrDefault(p => p.Id == patientId);
            if (patient == null) return;

            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = $"Assigner une chambre à {patient.FullName}",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            Border card = new Border
            {
                Style = (Style)this.Resources["CardStyle"],
                Margin = new Thickness(0, 0, 0, 20)
            };

            StackPanel panel = new StackPanel();

            // Info patient
            panel.Children.Add(CreateInfoField("Patient", patient.FullName));
            panel.Children.Add(CreateInfoField("Âge", $"{patient.Age} ans"));
            panel.Children.Add(CreateInfoField("Service", patient.ServiceName));
            panel.Children.Add(CreateInfoField("Statut", patient.Status));

            // Numéro de chambre
            panel.Children.Add(new TextBlock
            {
                Text = "Numéro de chambre:",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 20, 0, 10)
            });

            TextBox roomTextBox = new TextBox
            {
                FontSize = 16,
                Height = 40,
                Width = 120,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            panel.Children.Add(roomTextBox);

            // Boutons
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button assignBtn = new Button
            {
                Content = "✅ Assigner la chambre",
                Style = (Style)this.Resources["SuccessButton"],
                Width = 180,
                Margin = new Thickness(0, 0, 10, 0)
            };

            assignBtn.Click += (s, e) =>
            {
                if (!int.TryParse(roomTextBox.Text, out int roomNumber) || roomNumber <= 0)
                {
                    MessageBox.Show("Veuillez entrer un numéro de chambre valide.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                patient.RoomNumber = roomNumber;
                DatabaseHelper.UpdatePatient(patient);

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = currentUser.FullName,
                    Action = "Chambre assignée",
                    Details = $"Patient: {patient.FullName}, Chambre: {roomNumber}"
                });

                MessageBox.Show($"Chambre {roomNumber} assignée à {patient.FullName}",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                ShowMyPatients();
            };

            Button cancelBtn = new Button
            {
                Content = "❌ Annuler",
                Style = (Style)this.Resources["DangerButton"],
                Width = 120
            };
            cancelBtn.Click += (s, e) => ShowMyPatients();

            buttonPanel.Children.Add(assignBtn);
            buttonPanel.Children.Add(cancelBtn);
            panel.Children.Add(buttonPanel);

            card.Child = panel;
            ContentPanel.Children.Add(card);
        }

        private StackPanel CreateInfoField(string label, string value)
        {
            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBlock labelText = new TextBlock
            {
                Text = label + ":",
                FontWeight = FontWeights.SemiBold,
                Width = 120,
                FontSize = 14
            };

            TextBlock valueText = new TextBlock
            {
                Text = value,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };

            panel.Children.Add(labelText);
            panel.Children.Add(valueText);
            return panel;
        }

        private void SendRemarkToDoctor(int patientId)
        {
            var patient = DatabaseHelper.GetPatients().FirstOrDefault(p => p.Id == patientId);
            if (patient == null || !patient.AssignedDoctorId.HasValue) return;

            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = $"Envoyer une remarque au médecin - {patient.FullName}",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            Border card = new Border
            {
                Style = (Style)this.Resources["CardStyle"],
                Margin = new Thickness(0, 0, 0, 20)
            };

            StackPanel panel = new StackPanel();

            // Info médecin
            var doctor = DatabaseHelper.GetUsers().FirstOrDefault(u => u.Id == patient.AssignedDoctorId);
            if (doctor != null)
            {
                panel.Children.Add(CreateInfoField("Médecin", doctor.FullName));
                panel.Children.Add(CreateInfoField("Patient", patient.FullName));
                panel.Children.Add(CreateInfoField("Chambre", patient.RoomNumber.ToString()));
                panel.Children.Add(CreateInfoField("Service", patient.ServiceName));
            }

            // Type de remarque
            panel.Children.Add(new TextBlock
            {
                Text = "Type de remarque:",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 20, 0, 10)
            });

            ComboBox typeComboBox = new ComboBox
            {
                ItemsSource = new[]
                {
                    "Observation du patient",
                    "Problème avec les médicaments",
                    "Changement d'état du patient",
                    "Question sur le traitement",
                    "Autre"
                },
                SelectedIndex = 0,
                Height = 35,
                Margin = new Thickness(0, 0, 0, 20),
                Width = 250
            };
            panel.Children.Add(typeComboBox);

            // Zone de texte pour la remarque
            panel.Children.Add(new TextBlock
            {
                Text = "Détails de la remarque:",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            TextBox remarkTextBox = new TextBox
            {
                Height = 150,
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                FontSize = 14,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            panel.Children.Add(remarkTextBox);

            // Urgent checkbox
            CheckBox urgentCheckBox = new CheckBox
            {
                Content = "⚠️ URGENT - Notifier immédiatement le médecin",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold
            };

            panel.Children.Add(urgentCheckBox);

            // Boutons
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button sendBtn = new Button
            {
                Content = "📤 Envoyer la remarque",
                Style = (Style)this.Resources["PrimaryButton"],
                Width = 180,
                Margin = new Thickness(0, 0, 10, 0)
            };

            sendBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(remarkTextBox.Text))
                {
                    MessageBox.Show("Veuillez saisir une remarque.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Créer une notification pour le médecin
                DatabaseHelper.AddNotification(new Notification
                {
                    UserId = patient.AssignedDoctorId.Value,
                    Title = urgentCheckBox.IsChecked == true ? "⚠️ REMARQUE URGENTE" : $"💬 {typeComboBox.SelectedItem}",
                    Message = $"De: Inf. {currentUser.LastName}\n" +
                             $"Type: {typeComboBox.SelectedItem}\n" +
                             $"Patient: {patient.FullName} (Chambre {patient.RoomNumber})\n\n" +
                             $"{remarkTextBox.Text}",
                    Type = "NurseRemark",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    RelatedPatientId = patient.Id,
                    SenderId = currentUser.Id,
                    SenderName = $"Inf. {currentUser.LastName}",
                    IsUrgent = urgentCheckBox.IsChecked == true
                });

                // Ajouter une activité
                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = currentUser.FullName,
                    Action = "Remarque envoyée",
                    Details = $"Au médecin: {doctor?.FullName}, Type: {typeComboBox.SelectedItem}, Patient: {patient.FullName}"
                });

                MessageBox.Show("Remarque envoyée au médecin!",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                ShowMyPatients();
            };

            Button cancelBtn = new Button
            {
                Content = "Annuler",
                Style = (Style)this.Resources["DangerButton"],
                Width = 120
            };
            cancelBtn.Click += (s, e) => ShowMyPatients();

            buttonPanel.Children.Add(sendBtn);
            buttonPanel.Children.Add(cancelBtn);
            panel.Children.Add(buttonPanel);

            card.Child = panel;
            ContentPanel.Children.Add(card);
        }

        private void TakeChargePatient(int patientId)
        {
            var patient = DatabaseHelper.GetPatients().FirstOrDefault(p => p.Id == patientId);
            if (patient != null)
            {
                patient.AssignedNurseId = currentUser.Id;
                patient.Status = "Prise en charge";
                DatabaseHelper.UpdatePatient(patient);

                // Notifier le médecin si un médecin est assigné
                if (patient.AssignedDoctorId.HasValue)
                {
                    DatabaseHelper.AddNotification(new Notification
                    {
                        UserId = patient.AssignedDoctorId.Value,
                        Title = "Patient pris en charge",
                        Message = $"Le patient {patient.FullName} a été pris en charge par Inf. {currentUser.LastName}",
                        Type = "Info",
                        CreatedAt = DateTime.Now,
                        RelatedPatientId = patient.Id,
                        SenderId = currentUser.Id,
                        SenderName = $"Inf. {currentUser.LastName}"
                    });
                }

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = currentUser.FullName,
                    Action = "Prise en charge patient",
                    Details = $"Patient: {patient.FullName}"
                });

                MessageBox.Show($"Patient {patient.FullName} pris en charge avec succès!\n\n" +
                               "✓ Accomplir le patient jusqu'à sa chambre\n" +
                               "✓ Lire la prescription dans le système\n" +
                               "✓ Vérifier les médicaments reçus de la pharmacie\n" +
                               "✓ Administrer le traitement selon la prescription",
                    "Prise en charge", MessageBoxButton.OK, MessageBoxImage.Information);

                ShowMyPatients();
            }
        }

        private void ViewPrescription(int prescriptionId)
        {
            var prescription = DatabaseHelper.GetPrescriptions().FirstOrDefault(p => p.Id == prescriptionId);
            if (prescription == null) return;

            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = $"Prescription pour {prescription.PatientName}",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            Border prescriptionCard = new Border { Style = (Style)this.Resources["CardStyle"] };
            StackPanel prescriptionPanel = new StackPanel();

            // Informations générales
            TextBlock infoText = new TextBlock
            {
                Text = $"Prescrite par: {prescription.DoctorName}\nDate: {prescription.CreationDate:dd/MM/yyyy HH:mm}\nStatut: {prescription.Status}",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15),
                FontWeight = FontWeights.SemiBold
            };
            prescriptionPanel.Children.Add(infoText);

            // AJOUT: Afficher les remarques du médecin si elles existent
            if (!string.IsNullOrEmpty(prescription.Notes))
            {
                Border remarksCard = new Border
                {
                    BorderBrush = Brushes.Orange,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 15),
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 165, 0))
                };

                StackPanel remarksPanel = new StackPanel();

                TextBlock remarksTitle = new TextBlock
                {
                    Text = "📝 REMARQUES DU MÉDECIN:",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Orange,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                remarksPanel.Children.Add(remarksTitle);

                TextBlock remarksText = new TextBlock
                {
                    Text = prescription.Notes,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                remarksPanel.Children.Add(remarksText);

                remarksCard.Child = remarksPanel;
                prescriptionPanel.Children.Add(remarksCard);
            }

            TextBlock medTitle = new TextBlock
            {
                Text = "💊 MÉDICAMENTS PRESCRITS:",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99))
            };
            prescriptionPanel.Children.Add(medTitle);

            // Compter la quantité totale
            int totalQuantity = 0;

            foreach (var med in prescription.Medications)
            {
                Border medCard = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 5)
                };

                StackPanel medStack = new StackPanel();

                // Médicament avec dosage
                TextBlock medText = new TextBlock
                {
                    Text = $"• {med.MedicationName} {med.Dosage}",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold
                };
                medStack.Children.Add(medText);

                // Fréquence et durée
                TextBlock scheduleText = new TextBlock
                {
                    Text = $"  Fréquence: {med.Frequency} • Durée: {med.Duration}",
                    FontSize = 13,
                    Margin = new Thickness(10, 2, 0, 0)
                };
                medStack.Children.Add(scheduleText);

                // Quantité
                TextBlock quantityText = new TextBlock
                {
                    Text = $"  Quantité à administrer: {med.Quantity} unités",
                    FontSize = 13,
                    Foreground = Brushes.Blue,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(10, 5, 0, 0)
                };
                medStack.Children.Add(quantityText);

                medCard.Child = medStack;
                prescriptionPanel.Children.Add(medCard);

                totalQuantity += med.Quantity;
            }

            // Total général
            TextBlock totalText = new TextBlock
            {
                Text = $"📦 TOTAL: {totalQuantity} unités pour {prescription.Medications.Count} médicament(s)",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 15, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99))
            };
            prescriptionPanel.Children.Add(totalText);

            prescriptionCard.Child = prescriptionPanel;
            ContentPanel.Children.Add(prescriptionCard);

            // Instructions pour l'infirmier
            if (prescription.Status == "Prête" || prescription.Status == "En attente d'administration")
            {
                Border instructionsCard = new Border
                {
                    Style = (Style)this.Resources["CardStyle"],
                    Background = new SolidColorBrush(Color.FromArgb(20, 46, 204, 113)),
                    Margin = new Thickness(0, 15, 0, 0)
                };

                StackPanel instructionsPanel = new StackPanel();

                TextBlock instructionsTitle = new TextBlock
                {
                    Text = "📋 INSTRUCTIONS POUR L'INFIRMIER:",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = Brushes.Green
                };
                instructionsPanel.Children.Add(instructionsTitle);

                TextBlock instructionsText = new TextBlock
                {
                    Text = "1. Vérifier les médicaments reçus de la pharmacie\n" +
                          "2. Contrôler les dosages et quantités\n" +
                          "3. Respecter les horaires d'administration\n" +
                          "4. Signaler tout problème au médecin\n" +
                          "5. Confirmer l'administration une fois réalisée",
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                instructionsPanel.Children.Add(instructionsText);

                instructionsCard.Child = instructionsPanel;
                ContentPanel.Children.Add(instructionsCard);
            }

            // Bouton retour
            Button backBtn = new Button
            {
                Content = "← Retour à la liste",
                Style = (Style)this.Resources["PrimaryButton"],
                Width = 150,
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            backBtn.Click += (s, e) => ShowMyPatients();
            ContentPanel.Children.Add(backBtn);
        }
        private void AdministerTreatment(int patientId, int prescriptionId)
        {
            var patient = DatabaseHelper.GetPatients().FirstOrDefault(p => p.Id == patientId);
            var prescription = DatabaseHelper.GetPrescriptions().FirstOrDefault(p => p.Id == prescriptionId);

            if (patient != null && prescription != null)
            {
                // Vérifier que la prescription est bien "Prête"
                if (prescription.Status != "Prête")
                {
                    MessageBox.Show("Cette prescription n'est pas encore prête pour administration.",
                                  "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (MessageBox.Show($"Confirmer l'administration du traitement pour {patient.FullName}?\n\n" +
                                   $"Prescription: {prescription.Medications.Count} médicament(s)",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    patient.Status = "Traitement administré";
                    prescription.Status = "Administrée";

                    DatabaseHelper.UpdatePatient(patient);
                    DatabaseHelper.UpdatePrescription(prescription);

                    // Notifier le médecin
                    if (patient.AssignedDoctorId.HasValue)
                    {
                        DatabaseHelper.AddNotification(new Notification
                        {
                            UserId = patient.AssignedDoctorId.Value,
                            Title = "✅ Traitement administré",
                            Message = $"Le traitement a été administré à {patient.FullName} par Inf. {currentUser.LastName}\n" +
                                     $"Prescription ID: {prescriptionId}",
                            Type = "Success",
                            CreatedAt = DateTime.Now,
                            RelatedPatientId = patient.Id,
                            RelatedPrescriptionId = prescriptionId,
                            SenderId = currentUser.Id,
                            SenderName = $"Inf. {currentUser.LastName}"
                        });
                    }

                    DatabaseHelper.AddActivity(new Activity
                    {
                        DateTime = DateTime.Now,
                        User = currentUser.FullName,
                        Action = "Traitement administré",
                        Details = $"Patient: {patient.FullName}, Prescription ID: {prescriptionId}"
                    });

                    MessageBox.Show($"Traitement administré avec succès pour {patient.FullName}!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                    ShowMyPatients();
                }
            }
        }
        private void ShowMedicationRequests()
        {
            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = "Demandes de Médicaments",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            var prescriptions = DatabaseHelper.GetPrescriptions()
                .Where(p => p.Status == "Prête")
                .ToList();

            if (prescriptions.Count == 0)
            {
                TextBlock emptyText = new TextBlock
                {
                    Text = "Aucune demande de médicament en attente",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                ContentPanel.Children.Add(emptyText);
                return;
            }

            foreach (var prescription in prescriptions)
            {
                var patient = DatabaseHelper.GetPatients().FirstOrDefault(p => p.Id == prescription.PatientId);
                if (patient == null) continue;

                Border requestCard = new Border
                {
                    Style = (Style)this.Resources["CardStyle"],
                    Margin = new Thickness(0, 0, 0, 10)
                };

                StackPanel requestPanel = new StackPanel();

                TextBlock requestTitle = new TextBlock
                {
                    Text = $"💊 Médicaments prêts pour {patient.FullName}",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                requestPanel.Children.Add(requestTitle);

                TextBlock patientInfo = new TextBlock
                {
                    Text = $"🏥 Chambre: {patient.RoomNumber}   •👨‍⚕️ Prescription créée par: {prescription.DoctorName}   •🥼Service: {patient.ServiceName}",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                requestPanel.Children.Add(patientInfo);

                // AJOUT: Afficher les remarques si elles existent
                if (!string.IsNullOrEmpty(prescription.Notes))
                {
                    Border remarksBox = new Border
                    {
                        BorderBrush = Brushes.Orange,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(8),
                        Margin = new Thickness(0, 0, 0, 10),
                        Background = new SolidColorBrush(Color.FromArgb(20, 255, 165, 0))
                    };

                    TextBlock remarksLabel = new TextBlock
                    {
                        Text = "📝 Remarques du médecin:",
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 3),
                        Foreground = Brushes.Orange
                    };

                    TextBlock remarksText = new TextBlock
                    {
                        Text = prescription.Notes.Length > 100 ?
                               prescription.Notes.Substring(0, 100) + "..." :
                               prescription.Notes,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap
                    };

                    StackPanel remarksPanel = new StackPanel();
                    remarksPanel.Children.Add(remarksLabel);
                    remarksPanel.Children.Add(remarksText);
                    remarksBox.Child = remarksPanel;

                    requestPanel.Children.Add(remarksBox);
                }

                TextBlock medTitle = new TextBlock
                {
                    Text = "Médicaments:",
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                requestPanel.Children.Add(medTitle);

                foreach (var med in prescription.Medications)
                {
                    TextBlock medItem = new TextBlock
                    {
                        Text = $"• {med.MedicationName} {med.Dosage} - {med.Frequency} ({med.Quantity} unités)",
                        Margin = new Thickness(10, 0, 0, 2),
                        FontSize = 13
                    };
                    requestPanel.Children.Add(medItem);
                }

                Button confirmButton = new Button
                {
                    Content = "✅ Confirmer la réception",
                    Style = (Style)this.Resources["SuccessButton"],
                    Height = 45,
                    Margin = new Thickness(0, 15, 0, 0),
                    Tag = prescription.Id
                };
                confirmButton.Click += (s, e) =>
                {
                    prescription.Status = "En attente d'administration";
                    DatabaseHelper.UpdatePrescription(prescription);

                    DatabaseHelper.AddActivity(new Activity
                    {
                        DateTime = DateTime.Now,
                        User = currentUser.FullName,
                        Action = "Réception médicaments",
                        Details = $"Patient: {patient.FullName}"
                    });

                    MessageBox.Show("Réception confirmée! Le traitement est maintenant prêt à être administré.\n\n" +
                                  "N'oubliez pas de vérifier:\n" +
                                  "✓ Les dosages\n" +
                                  "✓ Les horaires d'administration\n" +
                                  "✓ Les remarques du médecin",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                    ShowMedicationRequests();
                };
                requestPanel.Children.Add(confirmButton);

                // Bouton pour voir les détails complets
                Button viewDetailsBtn = new Button
                {
                    Content = "📋 Voir détails complets",
                    Style = (Style)this.Resources["InfoButton"],
                    Height = 35,
                    Margin = new Thickness(0, 10, 0, 0),
                    Tag = prescription.Id
                };
                viewDetailsBtn.Click += (s, e) => ViewPrescription(prescription.Id);
                requestPanel.Children.Add(viewDetailsBtn);

                requestCard.Child = requestPanel;
                ContentPanel.Children.Add(requestCard);
            }
        }
        private void ShowNotifications()
        {
            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = "Notifications",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            var notifications = DatabaseHelper.GetNotifications()
       .Where(n => n.UserId == currentUser.Id ||
                  (n.RelatedPatientId.HasValue &&
                   DatabaseHelper.GetPatients()
                       .Any(p => p.Id == n.RelatedPatientId &&
                                p.AssignedNurseId == currentUser.Id)))
       .OrderByDescending(n => n.CreatedAt)
       .ToList();

            if (notifications.Count == 0)
            {
                TextBlock emptyText = new TextBlock
                {
                    Text = "Aucune notification",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                ContentPanel.Children.Add(emptyText);
                return;
            }

            foreach (var notification in notifications)
            {
                Border notifCard = new Border
                {
                    Style = (Style)this.Resources["CardStyle"],
                    Margin = new Thickness(0, 0, 0, 10),
                    Background = notification.IsRead ? Brushes.White : new SolidColorBrush(Color.FromArgb(30, 32, 156, 238))
                };

                StackPanel notifPanel = new StackPanel();

                TextBlock notifTitle = new TextBlock
                {
                    Text = notification.Title,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                notifPanel.Children.Add(notifTitle);

                TextBlock messageText = new TextBlock
                {
                    Text = notification.Message,
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                notifPanel.Children.Add(messageText);

                TextBlock senderText = new TextBlock
                {
                    Text = $"De: {notification.SenderName}",
                    FontSize = 12,
                    Foreground = Brushes.DarkGray,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                notifPanel.Children.Add(senderText);

                TextBlock timeText = new TextBlock
                {
                    Text = notification.TimeAgo,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99))
                };
                notifPanel.Children.Add(timeText);

                // Mark as read button
                if (!notification.IsRead)
                {
                    Button markReadBtn = new Button
                    {
                        Content = "Marquer comme lu",
                        Style = (Style)this.Resources["InfoButton"],
                        Width = 150,
                        Margin = new Thickness(0, 10, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Tag = notification.Id
                    };
                    markReadBtn.Click += (s, e) =>
                    {
                        notification.IsRead = true;
                        var allNotifications = DatabaseHelper.GetNotifications();
                        var index = allNotifications.FindIndex(n => n.Id == notification.Id);
                        if (index >= 0)
                        {
                            allNotifications[index] = notification;
                            DatabaseHelper.SaveNotifications(allNotifications);
                            ShowNotifications();
                        }
                    };
                    notifPanel.Children.Add(markReadBtn);
                }

                notifCard.Child = notifPanel;
                ContentPanel.Children.Add(notifCard);
            }
        }

        private Brush GetStatusColor(string status)
        {
            return status switch
            {
                "En attente" => Brushes.Orange,
                "En consultation" => Brushes.LightBlue,
                "Prescription créée" => Brushes.LightGreen,
                "Prise en charge" => Brushes.LightBlue,
                "Traitement administré" => Brushes.Green,
                _ => Brushes.LightGray
            };
        }
        // Ajouter ces méthodes à la classe NurseWindow

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BtnLogout_Click(sender, e);
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.D1 || e.Key == Key.NumPad1)
                {
                    ShowMyPatients();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                {
                    ShowMedicationRequests();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                {
                    ShowNotifications();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D4 || e.Key == Key.NumPad4)
                {
                    ShowHistory(); // Nouvelle fonction d'historique
                    e.Handled = true;
                    return;
                }
            }
        }

        private void ShowHistory()
        {
            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = "📋 Historique des Interventions",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            // Récupérer les patients traités par cet infirmier
            var patients = DatabaseHelper.GetPatients()
                .Where(p => p.AssignedNurseId == currentUser.Id)
                .OrderByDescending(p => p.RegistrationDate)
                .ToList();

            var prescriptions = DatabaseHelper.GetPrescriptions()
                .Where(p => p.Status == "Administrée")
                .ToList();

            var activities = DatabaseHelper.GetActivities()
                .Where(a => a.User.Contains(currentUser.LastName) || a.User.Contains("Infirmier"))
                .OrderByDescending(a => a.DateTime)
                .Take(20)
                .ToList();

            if (patients.Count == 0 && activities.Count == 0)
            {
                TextBlock emptyText = new TextBlock
                {
                    Text = "Aucun historique disponible",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                ContentPanel.Children.Add(emptyText);
                return;
            }

            // Section Patients traités
            if (patients.Count > 0)
            {
                TextBlock patientsTitle = new TextBlock
                {
                    Text = "👥 Patients traités récemment",
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 20, 0, 15),
                    Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99))
                };
                ContentPanel.Children.Add(patientsTitle);

                foreach (var patient in patients.Take(10))
                {
                    var patientPrescription = prescriptions.FirstOrDefault(p => p.PatientId == patient.Id);

                    Border card = new Border
                    {
                        Style = (Style)this.Resources["CardStyle"],
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    StackPanel panel = new StackPanel();

                    TextBlock patientInfo = new TextBlock
                    {
                        Text = $"{patient.FullName} - {patient.Age} ans",
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    panel.Children.Add(patientInfo);

                    TextBlock details = new TextBlock
                    {
                        Text = $"Service: {patient.ServiceName} • Chambre: {patient.RoomNumber}",
                        FontSize = 14,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    panel.Children.Add(details);

                    TextBlock date = new TextBlock
                    {
                        Text = $"Prise en charge: {patient.RegistrationDate:dd/MM/yyyy HH:mm}",
                        FontSize = 12,
                        Foreground = Brushes.DarkGray
                    };
                    panel.Children.Add(date);

                    if (patientPrescription != null)
                    {
                        TextBlock prescriptionInfo = new TextBlock
                        {
                            Text = $"Prescription administrée le: {patientPrescription.CreationDate:dd/MM/yyyy HH:mm}",
                            FontSize = 12,
                            Foreground = Brushes.Green,
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 10, 0, 0)
                        };
                        panel.Children.Add(prescriptionInfo);
                    }

                    card.Child = panel;
                    ContentPanel.Children.Add(card);
                }
            }

            // Section Activités
            if (activities.Count > 0)
            {
                TextBlock activitiesTitle = new TextBlock
                {
                    Text = "📝 Journal des activités",
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 30, 0, 15),
                    Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99))
                };
                ContentPanel.Children.Add(activitiesTitle);

                foreach (var activity in activities)
                {
                    Border card = new Border
                    {
                        Style = (Style)this.Resources["CardStyle"],
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    StackPanel panel = new StackPanel();

                    TextBlock actionText = new TextBlock
                    {
                        Text = activity.Action,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    panel.Children.Add(actionText);

                    TextBlock detailsText = new TextBlock
                    {
                        Text = activity.Details,
                        FontSize = 13,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    panel.Children.Add(detailsText);

                    TextBlock dateText = new TextBlock
                    {
                        Text = activity.DateTime.ToString("dd/MM/yyyy HH:mm"),
                        FontSize = 11,
                        Foreground = Brushes.DarkGray
                    };
                    panel.Children.Add(dateText);

                    card.Child = panel;
                    ContentPanel.Children.Add(card);
                }
            }
        }
        private void BtnHistory_Click(object sender, RoutedEventArgs e) => ShowHistory();
    }
}