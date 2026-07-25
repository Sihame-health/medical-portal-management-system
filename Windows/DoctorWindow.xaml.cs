using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MedicalSystem.Database;
using MedicalSystem.Models;

namespace MedicalSystem
{
    public partial class DoctorWindow : Window
    {
        private User currentUser;
        private Patient? selectedPatient;
        private DispatcherTimer notificationTimer;

        // CORRECTION: Variables pour stocker les références aux contrôles de prescription
        private ListBox? currentMedicationsList;
        private TextBlock? currentInstructionsText;

        public DoctorWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            DoctorNameText.Text = $"Dr. {user.LastName}";

            // Initialiser le timer pour vérifier les nouvelles notifications
            notificationTimer = new DispatcherTimer();
            notificationTimer.Interval = TimeSpan.FromSeconds(30);
            notificationTimer.Tick += NotificationTimer_Tick;
            notificationTimer.Start();

            ShowPatientList();
            UpdateNotificationBadge();

            // CORRECTION: Désactiver le bouton "Créer Prescription" au démarrage
            BtnCreatePrescription.IsEnabled = false;
        }

        private void DoctorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BtnPatientList?.Focus();
        }

        private void DoctorWindow_PreviewKeyDown(object sender, KeyEventArgs e)
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
                    ShowPatientList();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                {
                    // CORRECTION: Vérifier si un patient est sélectionné avant d'activer
                    if (selectedPatient != null && CanCreatePrescription(selectedPatient))
                        ShowCreatePrescription(selectedPatient.Id);
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                {
                    ShowHistory();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D4 || e.Key == Key.NumPad4)
                {
                    ShowNotifications();
                    e.Handled = true;
                    return;
                }
            }
        }

        private void BtnPatientList_Click(object sender, RoutedEventArgs e) => ShowPatientList();

        private void BtnCreatePrescription_Click(object sender, RoutedEventArgs e)
        {
            // CORRECTION: Vérifier si un patient est sélectionné ET si on peut créer une prescription
            if (selectedPatient != null && CanCreatePrescription(selectedPatient))
                ShowCreatePrescription(selectedPatient.Id);
            else
                MessageBox.Show("Veuillez d'abord sélectionner un patient éligible pour une prescription.",
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e) => ShowHistory();
        private void BtnNotifications_Click(object sender, RoutedEventArgs e) => ShowNotifications();

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        // CORRECTION: Méthode pour vérifier si on peut créer une prescription pour un patient
        private bool CanCreatePrescription(Patient patient)
        {
            // Vérifier si le patient a déjà une prescription
            var existingPrescription = DatabaseHelper.GetPrescriptions()
                .FirstOrDefault(p => p.PatientId == patient.Id &&
                                   (p.Status == "Créée" || p.Status == "En préparation" || p.Status == "Prête"));

            // On ne peut créer une prescription que si:
            // 1. Le patient est en consultation OU en attente
            // 2. Il n'a pas déjà une prescription active
            return (patient.Status == "En consultation" || patient.Status == "En attente") &&
                   existingPrescription == null;
        }

        private void NotificationTimer_Tick(object sender, EventArgs e)
        {
            UpdateNotificationBadge();

            // Vérifier les notifications urgentes non lues
            var urgentNotifications = DatabaseHelper.GetNotifications()
                .Where(n => n.UserId == currentUser.Id && !n.IsRead && n.IsUrgent)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            if (urgentNotifications.Any())
            {
                // Montrer la plus récente en popup
                ShowUrgentNotificationPopup(urgentNotifications.First());
            }
        }

        private void UpdateNotificationBadge()
        {
            var notifications = DatabaseHelper.GetNotifications()
                .Where(n => n.UserId == currentUser.Id && !n.IsRead)
                .ToList();

            int unreadCount = notifications.Count;

            if (NotificationBadge != null && NotificationCount != null)
            {
                if (unreadCount > 0)
                {
                    NotificationCount.Text = unreadCount.ToString();
                    NotificationBadge.Visibility = Visibility.Visible;

                    // Si notification urgente, flash rouge
                    if (notifications.Any(n => n.IsUrgent))
                    {
                        FlashUrgentNotification();
                    }
                }
                else
                {
                    NotificationBadge.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void FlashUrgentNotification()
        {
            Dispatcher.Invoke(() =>
            {
                var originalColor = NotificationBadge.Background;
                NotificationBadge.Background = Brushes.Red;

                DispatcherTimer flashTimer = new DispatcherTimer();
                flashTimer.Interval = TimeSpan.FromMilliseconds(500);
                int flashCount = 0;

                flashTimer.Tick += (s, e) =>
                {
                    flashCount++;
                    if (flashCount % 2 == 0)
                    {
                        NotificationBadge.Background = Brushes.Red;
                    }
                    else
                    {
                        NotificationBadge.Background = Brushes.DarkRed;
                    }

                    if (flashCount >= 6)
                    {
                        flashTimer.Stop();
                        NotificationBadge.Background = originalColor;
                    }
                };

                flashTimer.Start();
            });
        }

        private void ShowUrgentNotificationPopup(Notification notification)
        {
            Dispatcher.Invoke(() =>
            {
                // CORRECTION: Vérifier si la fenêtre principale est encore ouverte
                if (!this.IsLoaded || this.Visibility != Visibility.Visible)
                {
                    return; // Fenêtre fermée, ne pas afficher la popup
                }

                try
                {
                            Window window = new()
                        {
                            Title = "⚠️ NOTIFICATION URGENTE",
                            Width = 500,
                            Height = 400,
                            WindowStyle = WindowStyle.ToolWindow,
                            ResizeMode = ResizeMode.NoResize,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = this,
                            Topmost = true
                        };
                        Window popupWindow = window;

                        StackPanel panel = new StackPanel
                        {
                            Margin = new Thickness(20),
                            Background = Brushes.White
                        };

                        // En-tête
                        StackPanel header = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(0, 0, 0, 20)
                        };

                        TextBlock icon = new TextBlock
                        {
                            Text = "⚠️",
                            FontSize = 30,
                            Margin = new Thickness(0, 0, 10, 0)
                        };

                        TextBlock title = new TextBlock
                        {
                            Text = "REMARQUE URGENTE",
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.Red
                        };

                        header.Children.Add(icon);
                        header.Children.Add(title);
                        panel.Children.Add(header);

                        // Contenu
                        panel.Children.Add(new TextBlock
                        {
                            Text = $"De: {notification.SenderName}",
                            FontSize = 14,
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 0, 0, 10)
                        });

                        panel.Children.Add(new TextBlock
                        {
                            Text = notification.Message,
                            FontSize = 14,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 0, 0, 20)
                        });

                        // Boutons
                        StackPanel buttonPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 20, 0, 0)
                        };

                        Button viewPatientBtn = new Button
                        {
                            Content = "Voir le patient",
                            Width = 120,
                            Height = 35,
                            Background = Brushes.Red,
                            Foreground = Brushes.White,
                            Margin = new Thickness(0, 0, 10, 0)
                        };

                        viewPatientBtn.Click += (s, e) =>
                        {
                            if (notification.RelatedPatientId.HasValue)
                            {
                                ShowPatientFile(notification.RelatedPatientId.Value, fromHistory: false);
                                popupWindow.Close();
                            }
                        };

                        Button closeBtn = new Button
                        {
                            Content = "J'ai compris",
                            Width = 120,
                            Height = 35,
                            Background = Brushes.Gray,
                            Foreground = Brushes.White
                        };

                        closeBtn.Click += (s, e) => popupWindow.Close();

                        buttonPanel.Children.Add(viewPatientBtn);
                        buttonPanel.Children.Add(closeBtn);
                        panel.Children.Add(buttonPanel);

                        popupWindow.Content = panel;
                        popupWindow.ShowDialog();
                    }
            catch (InvalidOperationException ex)
            {
                // Log l'erreur mais ne pas bloquer l'application
                Console.WriteLine($"Erreur d'affichage de popup: {ex.Message}");
            }
        });
        }

        private void ShowPatientList()
        {
            ContentPanel.Children.Clear();
            selectedPatient = null;

            // CORRECTION: Désactiver le bouton "Créer Prescription" quand on change de vue
            BtnCreatePrescription.IsEnabled = false;

            // Compter les patients en attente
            var waitingPatients = DatabaseHelper.GetPatients()
                .Count(p => p.ServiceId == currentUser.ServiceId && p.Status == "En attente");

            TextBlock title = new TextBlock
            {
                Text = $"👥 Mes Patients du Jour ({waitingPatients} en attente)",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            // Filtrer seulement les patients EN ATTENTE ou EN CONSULTATION
            var patients = DatabaseHelper.GetPatients()
                .Where(p => p.ServiceId == currentUser.ServiceId &&
                           (p.Status == "En attente" || p.Status == "En consultation"))
                .OrderBy(p => p.RegistrationDate)
                .ToList();

            if (patients.Count == 0)
            {
                TextBlock emptyText = new TextBlock
                {
                    Text = "Aucun patient en attente de consultation",
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
            Border patientCard = new Border
            {
                Style = (Style)this.Resources["CardStyle"],
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            patientCard.MouseLeftButtonUp += (s, e) =>
            {
                selectedPatient = patient;
                ShowPatientFile(patient.Id, fromHistory: false);
            };

            StackPanel cardPanel = new StackPanel();

            // Patient info
            TextBlock patientInfo = new TextBlock
            {
                Text = $"{patient.FullName} - {patient.Age} ans",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            cardPanel.Children.Add(patientInfo);

            TextBlock serviceInfo = new TextBlock
            {
                Text = $"Service: {patient.ServiceName}",
                FontSize = 14,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 5)
            };
            cardPanel.Children.Add(serviceInfo);

            TextBlock arrivalInfo = new TextBlock
            {
                Text = $"Arrivé le: {patient.RegistrationDate:dd/MM/yyyy HH:mm}",
                FontSize = 14,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 5)
            };
            cardPanel.Children.Add(arrivalInfo);

            // Vérifier s'il y a des remarques non lues pour ce patient
            var patientNotifications = DatabaseHelper.GetNotifications()
                .Where(n => n.UserId == currentUser.Id &&
                           n.RelatedPatientId == patient.Id &&
                           !n.IsRead &&
                           n.Type == "NurseRemark")
                .Count();

            if (patientNotifications > 0)
            {
                // Badge de notification
                Border notificationBadge = new Border
                {
                    Background = Brushes.Red,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(8, 4, 8, 4),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                StackPanel notificationPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                TextBlock notificationIcon = new TextBlock
                {
                    Text = "💬",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 5, 0),
                    Foreground = Brushes.White
                };

                TextBlock notificationText = new TextBlock
                {
                    Text = $"{patientNotifications} remarque(s)",
                    FontSize = 11,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold
                };

                notificationPanel.Children.Add(notificationIcon);
                notificationPanel.Children.Add(notificationText);
                notificationBadge.Child = notificationPanel;
                cardPanel.Children.Add(notificationBadge);
            }

            // Status badge
            Border statusBadge = new Border
            {
                Background = patient.Status == "En attente" ? Brushes.Orange : Brushes.LightBlue,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 5, 0, 0)
            };
            TextBlock statusText = new TextBlock
            {
                Text = patient.Status,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            statusBadge.Child = statusText;
            cardPanel.Children.Add(statusBadge);

            // Buttons
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button viewBtn = new Button
            {
                Content = "Voir le dossier",
                Style = (Style)this.Resources["InfoButtonStyle"],
                Width = 120,
                Margin = new Thickness(0, 0, 10, 0),
                Tag = patient.Id
            };
            viewBtn.Click += (s, e) =>
            {
                selectedPatient = patient;
                ShowPatientFile(patient.Id, fromHistory: false);
            };

            Button consultBtn = new Button
            {
                Content = patient.Status == "En consultation" ? "Continuer consultation" : "Commencer consultation",
                Style = (Style)this.Resources["SuccessButtonStyle"],
                Width = 220,
                Tag = patient.Id
            };
            consultBtn.Click += (s, e) =>
            {
                selectedPatient = patient;
                StartConsultation(patient.Id);
            };

            buttonPanel.Children.Add(viewBtn);
            buttonPanel.Children.Add(consultBtn);
            cardPanel.Children.Add(buttonPanel);

            patientCard.Child = cardPanel;
            return patientCard;
        }

        // CORRECTION: Ajout du paramètre fromHistory pour distinguer l'origine
        private void ShowPatientFile(int patientId, bool fromHistory = false)
        {
            var patient = DatabaseHelper.GetPatients().FirstOrDefault(p => p.Id == patientId);
            if (patient == null) return;
            selectedPatient = patient;

            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = $"📄 Dossier Patient: {patient.FullName}",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            Border infoCard = new Border { Style = (Style)this.Resources["CardStyle"] };
            StackPanel infoPanel = new StackPanel();

            AddInfoField(infoPanel, "Nom complet", patient.FullName);
            AddInfoField(infoPanel, "CIN/Passeport", patient.CIN);
            AddInfoField(infoPanel, "Âge", patient.Age.ToString());
            //AddInfoField(infoPanel, "Adresse:", patient.Address);
            AddInfoField(infoPanel, "Téléphone", patient.Phone);
            AddInfoField(infoPanel, "Service", patient.ServiceName);
            AddInfoField(infoPanel, "Date d'arrivée", patient.RegistrationDate.ToString("dd/MM/yyyy HH:mm"));
            AddInfoField(infoPanel, "Statut", patient.Status);

            if (!string.IsNullOrEmpty(patient.MedicalHistory))
            {
                AddInfoField(infoPanel, "Antécédents médicaux", patient.MedicalHistory);
            }

            infoCard.Child = infoPanel;
            ContentPanel.Children.Add(infoCard);

            // CORRECTION: Vérifier si le patient a déjà une prescription
            var existingPrescription = DatabaseHelper.GetPrescriptions()
                .FirstOrDefault(p => p.PatientId == patient.Id);

            // Buttons
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            Button backBtn = new Button
            {
                Content = fromHistory ? "Retour à l'historique" : "Retour à la liste",
                Style = (Style)this.Resources["PrimaryButtonStyle"],
                Width = 150,
                Margin = new Thickness(0, 0, 10, 0)
            };
            backBtn.Click += (s, e) =>
            {
                if (fromHistory)
                    ShowHistory();
                else
                    ShowPatientList();
            };

            // CORRECTION: Afficher le bon bouton selon la situation
            if (fromHistory)
            {
                // Dans l'historique, on ne peut pas créer de nouvelle prescription
                if (existingPrescription != null)
                {
                    Button viewPrescriptionBtn = new Button
                    {
                        Content = "📋 Voir la prescription",
                        Style = (Style)this.Resources["SuccessButtonStyle"],
                        Width = 180,
                        Tag = existingPrescription.Id
                    };
                    viewPrescriptionBtn.Click += (s, e) => ViewPrescriptionDetails(existingPrescription.Id);
                    buttonPanel.Children.Add(viewPrescriptionBtn);
                }
                else
                {
                    // Si pas de prescription (cas rare), on affiche un bouton inactif
                    Button noPrescriptionBtn = new Button
                    {
                        Content = "Aucune prescription",
                        Style = (Style)this.Resources["InfoButtonStyle"],
                        Width = 180,
                        IsEnabled = false
                    };
                    buttonPanel.Children.Add(noPrescriptionBtn);
                }
            }
            else
            {
                // Dans la liste active, on peut créer une prescription si éligible
                if (CanCreatePrescription(patient))
                {
                    Button createPrescriptionBtn = new Button
                    {
                        Content = existingPrescription != null ? "Modifier prescription" : "Créer prescription",
                        Style = (Style)this.Resources["SuccessButtonStyle"],
                        Width = 180,
                        Tag = patientId
                    };
                    createPrescriptionBtn.Click += (s, e) => ShowCreatePrescription(patientId);
                    buttonPanel.Children.Add(createPrescriptionBtn);

                    // CORRECTION: Activer le bouton dans la sidebar
                    BtnCreatePrescription.IsEnabled = true;
                }
                else
                {
                    // Patient non éligible pour une nouvelle prescription
                    if (existingPrescription != null)
                    {
                        Button viewPrescriptionBtn = new Button
                        {
                            Content = "Voir prescription existante",
                            Style = (Style)this.Resources["InfoButtonStyle"],
                            Width = 200,
                            Tag = existingPrescription.Id
                        };
                        viewPrescriptionBtn.Click += (s, e) => ViewPrescriptionDetails(existingPrescription.Id);
                        buttonPanel.Children.Add(viewPrescriptionBtn);
                    }

                    // CORRECTION: Désactiver le bouton dans la sidebar
                    BtnCreatePrescription.IsEnabled = false;
                }
            }

            buttonPanel.Children.Insert(0, backBtn);
            ContentPanel.Children.Add(buttonPanel);
        }

        private void StartConsultation(int patientId)
        {
            var patient = DatabaseHelper.GetPatients().FirstOrDefault(p => p.Id == patientId);
            if (patient != null)
            {
                patient.Status = "En consultation";
                patient.AssignedDoctorId = currentUser.Id;
                DatabaseHelper.UpdatePatient(patient);

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = currentUser.FullName,
                    Action = "Début consultation",
                    Details = $"Patient: {patient.FullName}"
                });

                MessageBox.Show($"Consultation commencée pour {patient.FullName}\n\n" +
                               "Le patient reste dans la liste avec le statut 'En consultation'",
                               "Consultation", MessageBoxButton.OK, MessageBoxImage.Information);

                ShowPatientFile(patientId, fromHistory: false);
            }
        }

        private void ShowCreatePrescription(int patientId)
        {
            var patient = DatabaseHelper.GetPatients().FirstOrDefault(p => p.Id == patientId);
            if (patient == null) return;

            // CORRECTION: Vérifier si on peut vraiment créer une prescription
            if (!CanCreatePrescription(patient))
            {
                MessageBox.Show("Ce patient n'est pas éligible pour une nouvelle prescription.\n" +
                              "Soit il a déjà une prescription active, soit son statut ne le permet pas.",
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = $"📝 Création de Prescription - {patient.FullName}",
                Style = (Style)this.Resources["PageTitle"]
            };
            ContentPanel.Children.Add(title);

            // Card des informations patient
            Border patientCard = new Border
            {
                Style = (Style)this.Resources["CardStyle"]
            };

            StackPanel patientPanel = new StackPanel();

            TextBlock patientTitle = new TextBlock
            {
                Text = "Informations Patient",
                Style = (Style)this.Resources["SectionTitle"]
            };
            patientPanel.Children.Add(patientTitle);

            // Info patient
            patientPanel.Children.Add(CreateInfoField("Nom", patient.FullName));
            patientPanel.Children.Add(CreateInfoField("Âge", $"{patient.Age} ans"));
            patientPanel.Children.Add(CreateInfoField("Service", patient.ServiceName));
            patientPanel.Children.Add(CreateInfoField("Statut", patient.Status));

            patientCard.Child = patientPanel;
            ContentPanel.Children.Add(patientCard);

            // Card des médicaments - NOUVELLE VERSION AVEC QUANTITÉ
            Border medicationCard = new Border
            {
                Style = (Style)this.Resources["CardStyle"]
            };

            StackPanel medicationPanel = new StackPanel();

            TextBlock medicationTitle = new TextBlock
            {
                Text = "Ajouter des Médicaments",
                Style = (Style)this.Resources["SectionTitle"]
            };
            medicationPanel.Children.Add(medicationTitle);

            // Grid avec 5 colonnes (ajout de la quantité)
            Grid medicationGrid = new Grid();
            medicationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }); // Médicament
            medicationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });    // Dosage
            medicationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });    // Fréquence
            medicationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });    // Durée
            medicationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });  // Quantité
            medicationGrid.Margin = new Thickness(0, 0, 0, 10);

            // Labels pour chaque colonne
            TextBlock medLabel = new TextBlock
            {
                Text = "Médicament",
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetColumn(medLabel, 0);

            TextBlock dosageLabel = new TextBlock
            {
                Text = "Dosage",
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetColumn(dosageLabel, 1);

            TextBlock freqLabel = new TextBlock
            {
                Text = "Fréquence",
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetColumn(freqLabel, 2);

            TextBlock durLabel = new TextBlock
            {
                Text = "Durée",
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetColumn(durLabel, 3);

            TextBlock qtyLabel = new TextBlock
            {
                Text = "Quantité",
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 3)
            };
            Grid.SetColumn(qtyLabel, 4);

            medicationGrid.Children.Add(medLabel);
            medicationGrid.Children.Add(dosageLabel);
            medicationGrid.Children.Add(freqLabel);
            medicationGrid.Children.Add(durLabel);
            medicationGrid.Children.Add(qtyLabel);

            // Variables pour stocker les contrôles d'entrée
            ComboBox medicineCombo = new ComboBox
            {
                Style = (Style)this.Resources["ComboBoxStyle"],
                ItemsSource = DatabaseHelper.GetMedications().Select(m => m.Name).ToList(),
                Margin = new Thickness(2)
            };
            if (medicineCombo.Items.Count > 0) medicineCombo.SelectedIndex = 0;
            Grid.SetColumn(medicineCombo, 0);
            Grid.SetRow(medicineCombo, 1);

            ComboBox dosageCombo = new ComboBox
            {
                Style = (Style)this.Resources["ComboBoxStyle"],
                ItemsSource = new[] { "500mg", "660mg", "1000mg" },
                SelectedIndex = 0,
                Margin = new Thickness(2)
            };
            Grid.SetColumn(dosageCombo, 1);
            Grid.SetRow(dosageCombo, 1);

            ComboBox frequencyCombo = new ComboBox
            {
                Style = (Style)this.Resources["ComboBoxStyle"],
                ItemsSource = new[] { "1x/jour", "2x/jour", "3x/jour" },
                SelectedIndex = 0,
                Margin = new Thickness(2)
            };
            Grid.SetColumn(frequencyCombo, 2);
            Grid.SetRow(frequencyCombo, 1);

            ComboBox durationCombo = new ComboBox
            {
                Style = (Style)this.Resources["ComboBoxStyle"],
                ItemsSource = new[] { "3 jours", "5 jours", "7 jours" },
                SelectedIndex = 0,
                Margin = new Thickness(2)
            };
            Grid.SetColumn(durationCombo, 3);
            Grid.SetRow(durationCombo, 1);

            // Champ pour la quantité
            TextBox quantityTextBox = new TextBox
            {
                Style = (Style)this.Resources["InputTextBoxStyle"],
                Text = "1",
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(quantityTextBox, 4);
            Grid.SetRow(quantityTextBox, 1);

            // Ajouter une rangée supplémentaire au grid
            medicationGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            medicationGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            medicationGrid.Children.Add(medicineCombo);
            medicationGrid.Children.Add(dosageCombo);
            medicationGrid.Children.Add(frequencyCombo);
            medicationGrid.Children.Add(durationCombo);
            medicationGrid.Children.Add(quantityTextBox);

            medicationPanel.Children.Add(medicationGrid);

            // CORRECTION: Créer le ListBox et le stocker dans une variable de classe
            currentMedicationsList = new ListBox
            {
                Height = 150,
                Margin = new Thickness(0, 15, 0, 0),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                SelectionMode = SelectionMode.Single,
                Name = "MedicationsList"
            };
            medicationPanel.Children.Add(currentMedicationsList);

            // CORRECTION: Panel pour les boutons d'action
            StackPanel buttonActionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button addButton = new Button
            {
                Content = "➕ Ajouter à la liste",
                Style = (Style)this.Resources["PrimaryButtonStyle"],
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(15, 8, 15, 8)
            };
            addButton.Click += (s, e) =>
            {
                if (medicineCombo.SelectedItem == null)
                {
                    MessageBox.Show("Veuillez sélectionner un médicament.",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Vérifier que la quantité est valide
                if (!int.TryParse(quantityTextBox.Text, out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("Veuillez entrer une quantité valide (nombre positif).",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // CORRECTION : Créer seulement le NOM du médicament avec dosage séparé
                string medicationName = medicineCombo.SelectedItem.ToString(); // "Paracétamol"
                string dosage = dosageCombo.SelectedItem.ToString(); // "500mg"
                string frequency = frequencyCombo.SelectedItem.ToString(); // "2x/jour"
                string duration = durationCombo.SelectedItem.ToString(); // "5 jours"

                // FORMAT CORRECT : Stocker les informations séparément
                string medicationDisplay = $"{medicationName} {dosage} - {frequency} - {duration} - Quantité: {quantity}";
                currentMedicationsList.Items.Add(medicationDisplay);

                // Réinitialiser la sélection
                medicineCombo.SelectedIndex = 0;
                dosageCombo.SelectedIndex = 0;
                frequencyCombo.SelectedIndex = 0;
                durationCombo.SelectedIndex = 0;
                quantityTextBox.Text = "1"; // Réinitialiser la quantité à 1

                // CORRECTION: Mettre à jour le compteur
                UpdateMedicationCount();
            };
            buttonActionPanel.Children.Add(addButton);

            // CORRECTION: Bouton de suppression avec référence directe au ListBox
            Button removeButton = new Button
            {
                Content = "🗑️ Supprimer la sélection",
                Style = (Style)this.Resources["DangerButtonStyle"],
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(15, 8, 15, 8)
            };
            removeButton.Click += (s, e) =>
            {
                if (currentMedicationsList.SelectedItem == null)
                {
                    MessageBox.Show("Veuillez d'abord sélectionner un médicament à supprimer dans la liste.",
                                  "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (MessageBox.Show($"Voulez-vous vraiment supprimer ce médicament?\n\n{currentMedicationsList.SelectedItem}",
                                  "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    currentMedicationsList.Items.Remove(currentMedicationsList.SelectedItem);
                    // CORRECTION: Mettre à jour le compteur après suppression
                    UpdateMedicationCount();
                }
            };
            buttonActionPanel.Children.Add(removeButton);

            medicationPanel.Children.Add(buttonActionPanel);

            // CORRECTION: Bouton pour vider toute la liste
            Button clearAllButton = new Button
            {
                Content = "🗑️ Vider toute la liste",
                Style = (Style)this.Resources["WarningButtonStyle"],
                Margin = new Thickness(0, 10, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(15, 8, 15, 8),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            clearAllButton.Click += (s, e) =>
            {
                if (currentMedicationsList.Items.Count == 0)
                {
                    MessageBox.Show("La liste des médicaments est déjà vide.",
                                  "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (MessageBox.Show($"Voulez-vous vraiment supprimer tous les médicaments de la liste?\n\nNombre de médicaments: {currentMedicationsList.Items.Count}",
                                  "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    currentMedicationsList.Items.Clear();
                    // CORRECTION: Mettre à jour le compteur
                    UpdateMedicationCount();
                }
            };
            medicationPanel.Children.Add(clearAllButton);

            medicationCard.Child = medicationPanel;
            ContentPanel.Children.Add(medicationCard);
            // Ajouter dans ShowCreatePrescription, après la création du Grid des médicaments :

            // Section Remarques
            Border remarksCard = new Border
            {
                Style = (Style)this.Resources["CardStyle"],
                Margin = new Thickness(0, 0, 0, 10)
            };

            StackPanel remarksPanel = new StackPanel();

            TextBlock remarksTitle = new TextBlock
            {
                Text = "📝 Remarques supplémentaires (optionnel)",
                Style = (Style)this.Resources["SectionTitle"]
            };
            remarksPanel.Children.Add(remarksTitle);

            TextBox remarksTextBox = new TextBox
            {
                Height = 80,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                FontSize = 14,
                Name = "RemarksTextBox"
            };
            remarksPanel.Children.Add(remarksTextBox);

            remarksCard.Child = remarksPanel;
            ContentPanel.Children.Add(remarksCard);

            // Card du résumé et validation
            Border summaryCard = new Border
            {
                Style = (Style)this.Resources["CardStyle"]
            };

            StackPanel summaryPanel = new StackPanel();

            TextBlock summaryTitle = new TextBlock
            {
                Text = "Résumé et Validation",
                Style = (Style)this.Resources["SectionTitle"]
            };
            summaryPanel.Children.Add(summaryTitle);

            // CORRECTION: Créer le TextBlock et le stocker dans une variable de classe
            currentInstructionsText = new TextBlock
            {
                FontSize = 14,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };

            // CORRECTION: Mettre à jour initialement le texte
            UpdateMedicationCount();

            summaryPanel.Children.Add(currentInstructionsText);

            // Vérification améliorée de disponibilité
            Button checkButton = new Button
            {
                Content = "🔍 Vérifier la disponibilité en pharmacie",
                Style = (Style)this.Resources["InfoButtonStyle"],
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(15, 8, 15, 8)
            };
            checkButton.Click += (s, e) =>
            {
                if (currentMedicationsList.Items.Count == 0)
                {
                    MessageBox.Show("Veuillez d'abord ajouter des médicaments à la prescription.",
                                  "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Vérification PRÉCISE de disponibilité avec quantité
                string message = "📋 Vérification des stocks en pharmacie:\n\n";
                var allMeds = DatabaseHelper.GetMedications();
                bool allAvailable = true;
                var unavailableMeds = new System.Collections.Generic.List<string>();
                var lowStockMeds = new System.Collections.Generic.List<string>();
                var insufficientStockMeds = new System.Collections.Generic.List<string>();

                foreach (string item in currentMedicationsList.Items)
                {
                    // Extraire le nom du médicament et la quantité
                    string[] parts = item.ToString().Split(new[] { " - " }, StringSplitOptions.None);
                    if (parts.Length < 4) continue;

                    // CORRECTION: Extraire correctement le nom du médicament
                    string medicationPart = parts[0]; // "Paracétamol 500mg"
                    string[] medParts = medicationPart.Split(' ');
                    string medName = string.Join(" ", medParts.Take(medParts.Length - 1)); // "Paracétamol"

                    // Extraire la quantité demandée
                    string qtyPart = parts[3];
                    if (!int.TryParse(qtyPart.Replace("Quantité: ", ""), out int requestedQuantity))
                    {
                        requestedQuantity = 1; // Valeur par défaut
                    }

                    // Chercher le médicament dans le stock
                    var medInStock = allMeds.FirstOrDefault(m =>
                        m.Name.Contains(medName, StringComparison.OrdinalIgnoreCase) ||
                        m.Code.Contains(medName, StringComparison.OrdinalIgnoreCase));

                    if (medInStock == null)
                    {
                        message += $"✗ {medName}: NON TROUVÉ en stock\n";
                        allAvailable = false;
                        unavailableMeds.Add($"{medName} (Non référencé)");
                    }
                    else if (medInStock.Quantity <= 0)
                    {
                        message += $"✗ {medName}: ÉPUISÉ (Stock: 0)\n";
                        allAvailable = false;
                        unavailableMeds.Add($"{medName} (Épuisé)");
                    }
                    else if (medInStock.Quantity < requestedQuantity)
                    {
                        message += $"✗ {medName}: STOCK INSUFFISANT (Demande: {requestedQuantity}, Disponible: {medInStock.Quantity})\n";
                        allAvailable = false;
                        insufficientStockMeds.Add($"{medName} ({medInStock.Quantity}/{requestedQuantity})");
                    }
                    else if (medInStock.IsLowStock)
                    {
                        message += $"⚠ {medName}: Stock faible (Disponible: {medInStock.Quantity}, Seuil: {medInStock.MinThreshold})\n";
                        lowStockMeds.Add($"{medName} ({medInStock.Quantity}/{medInStock.MinThreshold})");
                    }
                    else if (medInStock.IsExpiringSoon)
                    {
                        message += $"⏰ {medName}: Disponible mais expire bientôt ({medInStock.Quantity} unités)\n";
                    }
                    else
                    {
                        message += $"✓ {medName}: Disponible ({medInStock.Quantity} unités, seuil: {medInStock.MinThreshold})\n";
                    }
                }

                // Résumé des problèmes
                if (unavailableMeds.Count > 0)
                {
                    message += $"\n❌ CRITIQUE: {unavailableMeds.Count} médicament(s) non disponible(s):\n";
                    message += string.Join("\n", unavailableMeds);
                }

                if (insufficientStockMeds.Count > 0)
                {
                    message += $"\n❌ PROBLÈME: {insufficientStockMeds.Count} médicament(s) en quantité insuffisante:\n";
                    message += string.Join("\n", insufficientStockMeds);
                }

                if (lowStockMeds.Count > 0)
                {
                    message += $"\n⚠️ ATTENTION: {lowStockMeds.Count} médicament(s) en stock faible (sous le seuil):\n";
                    message += string.Join("\n", lowStockMeds);
                }

                if (allAvailable && lowStockMeds.Count == 0 && insufficientStockMeds.Count == 0)
                {
                    message += "\n✅ Tous les médicaments sont disponibles en quantité suffisante.";
                }

                MessageBox.Show(message, "Disponibilité des médicaments",
                               MessageBoxButton.OK,
                               (unavailableMeds.Count > 0 || insufficientStockMeds.Count > 0) ?
                                   MessageBoxImage.Error :
                                   (lowStockMeds.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information));
            };
            summaryPanel.Children.Add(checkButton);

            // Bouton valider avec vérification de stock PRÉCISE
            Button validateButton = new Button
            {
                Content = "✅ Valider et Envoyer la Prescription",
                Style = (Style)this.Resources["SuccessButtonStyle"],
                Margin = new Thickness(0, 10, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(20, 12, 20, 12),
                FontSize = 14
            };
            validateButton.Click += (s, e) =>
            {
                if (currentMedicationsList.Items.Count == 0)
                {
                    MessageBox.Show("Veuillez ajouter au moins un médicament à la prescription.",
                                   "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Vérifier PRÉCISÉMENT la disponibilité des stocks
                var allMeds = DatabaseHelper.GetMedications();
                var unavailableMeds = new System.Collections.Generic.List<string>();
                var lowStockMeds = new System.Collections.Generic.List<string>();
                var insufficientStockMeds = new System.Collections.Generic.List<string>();

                // Dictionnaire pour stocker les quantités demandées
                var requestedQuantities = new System.Collections.Generic.Dictionary<string, int>();

                foreach (string item in currentMedicationsList.Items)
                {
                    string[] parts = item.ToString().Split(new[] { " - " }, StringSplitOptions.None);
                    if (parts.Length < 4) continue;

                    // CORRECTION: Extraire correctement le nom du médicament
                    string medicationPart = parts[0]; // "Paracétamol 500mg"
                    string[] medParts = medicationPart.Split(' ');
                    string medName = string.Join(" ", medParts.Take(medParts.Length - 1)); // "Paracétamol"

                    // Extraire la quantité demandée
                    string qtyPart = parts[3];
                    if (!int.TryParse(qtyPart.Replace("Quantité: ", ""), out int requestedQuantity))
                    {
                        requestedQuantity = 1;
                    }

                    requestedQuantities[medName] = requestedQuantity;

                    var medInStock = allMeds.FirstOrDefault(m =>
                        m.Name.Contains(medName, StringComparison.OrdinalIgnoreCase) ||
                        m.Code.Contains(medName, StringComparison.OrdinalIgnoreCase));

                    if (medInStock == null)
                    {
                        unavailableMeds.Add($"{medName} (Non référencé)");
                    }
                    else if (medInStock.Quantity <= 0)
                    {
                        unavailableMeds.Add($"{medName} (Épuisé - Stock: 0)");
                    }
                    else if (medInStock.Quantity < requestedQuantity)
                    {
                        insufficientStockMeds.Add($"{medName} (Disponible: {medInStock.Quantity}, Demande: {requestedQuantity})");
                    }
                    else if (medInStock.IsLowStock)
                    {
                        lowStockMeds.Add($"{medName} (Stock faible: {medInStock.Quantity}/{medInStock.MinThreshold})");
                    }
                }

                // Si des médicaments ne sont pas disponibles, avertir le médecin
                if (unavailableMeds.Count > 0 || insufficientStockMeds.Count > 0)
                {
                    string warningMessage = "PROBLÈMES DE STOCK DÉTECTÉS:\n\n";

                    if (unavailableMeds.Count > 0)
                    {
                        warningMessage += $"❌ {unavailableMeds.Count} médicament(s) non disponible(s):\n";
                        warningMessage += string.Join("\n", unavailableMeds) + "\n\n";
                    }

                    if (insufficientStockMeds.Count > 0)
                    {
                        warningMessage += $"❌ {insufficientStockMeds.Count} médicament(s) en quantité insuffisante:\n";
                        warningMessage += string.Join("\n", insufficientStockMeds) + "\n\n";
                    }

                    warningMessage += "Voulez-vous quand même créer la prescription?";

                    if (MessageBox.Show(warningMessage, "Médicaments non disponibles",
                        MessageBoxButton.YesNo, MessageBoxImage.Error) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                else if (lowStockMeds.Count > 0)
                {
                    string warningMessage = $"ATTENTION: {lowStockMeds.Count} médicament(s) en stock faible:\n\n";
                    warningMessage += string.Join("\n", lowStockMeds);
                    warningMessage += "\n\nContinuer la création de la prescription?";

                    if (MessageBox.Show(warningMessage, "Stock faible",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                // Afficher un récapitulatif détaillé
                string recap = $"📋 Récapitulatif de la prescription pour {patient.FullName}:\n\n";
                recap += $"Nombre de médicaments: {currentMedicationsList.Items.Count}\n\n";
                recap += "Liste des médicaments:\n";

                int index = 1;
                int totalQuantity = 0;
                foreach (string item in currentMedicationsList.Items)
                {
                    recap += $"{index}. {item}\n";

                    // Compter la quantité totale
                    string[] parts = item.ToString().Split(new[] { " - " }, StringSplitOptions.None);
                    if (parts.Length >= 4)
                    {
                        string qtyPart = parts[3];
                        if (int.TryParse(qtyPart.Replace("Quantité: ", ""), out int qty))
                        {
                            totalQuantity += qty;
                        }
                    }
                    index++;
                }

                recap += $"\nQuantité totale demandée: {totalQuantity} unités\n";

                // Ajouter des avertissements
                if (unavailableMeds.Count > 0)
                {
                    recap += $"\n❌ {unavailableMeds.Count} médicament(s) non disponible(s)";
                }
                if (insufficientStockMeds.Count > 0)
                {
                    recap += $"\n❌ {insufficientStockMeds.Count} médicament(s) en quantité insuffisante";
                }
                if (lowStockMeds.Count > 0)
                {
                    recap += $"\n⚠️ {lowStockMeds.Count} médicament(s) en stock faible";
                }

                recap += "\n\nConfirmez-vous la création de cette prescription?";

                if (MessageBox.Show(recap, "Confirmation de la prescription",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                // Créer la prescription
                var prescription = new Prescription
                {
                    PatientId = patient.Id,
                    PatientName = patient.FullName,
                    DoctorId = currentUser.Id,
                    DoctorName = currentUser.FullName,
                    CreationDate = DateTime.Now,
                    Status = "Créée",
                    Notes = remarksTextBox.Text.Trim() // ← Ajout des remarques
                };

                // CORRECTION : Ajouter les médicaments avec parsing correct et séparation des données
                foreach (string item in currentMedicationsList.Items)
                {
                    string[] parts = item.ToString().Split(new[] { " - " }, StringSplitOptions.None);
                    if (parts.Length >= 4)
                    {
                        string medicationPart = parts[0]; // "Paracétamol 500mg"
                        string frequency = parts[1];      // "2x/jour"
                        string duration = parts[2];       // "5 jours"
                        string quantityPart = parts[3];   // "Quantité: 1"

                        // Séparer le nom et le dosage
                        string[] medParts = medicationPart.Split(' ');
                        string medicationName = string.Join(" ", medParts.Take(medParts.Length - 1)); // "Paracétamol"
                        string dosage = medParts.Last(); // "500mg"

                        // Extraire la quantité
                        int quantity = 1;
                        if (quantityPart.Contains("Quantité: ") &&
                            int.TryParse(quantityPart.Replace("Quantité: ", ""), out int parsedQty))
                        {
                            quantity = parsedQty;
                        }

                        prescription.Medications.Add(new Prescription.PrescriptionItem
                        {
                            MedicationName = medicationName, // "Paracétamol"
                            Dosage = dosage,                 // "500mg"
                            Frequency = frequency,           // "2x/jour"
                            Duration = duration,             // "5 jours"
                            Quantity = quantity              // 1
                        });
                    }
                }

                int prescriptionId = DatabaseHelper.AddPrescription(prescription);

                // METTRE À JOUR LE STATUT DU PATIENT - IMPORTANT !
                patient.Status = "Prescription créée";
                patient.AssignedDoctorId = currentUser.Id;
                DatabaseHelper.UpdatePatient(patient);

                // Calculer les problèmes de stock pour les notifications
                int totalProblems = unavailableMeds.Count + insufficientStockMeds.Count + lowStockMeds.Count;

                // Ajouter des notifications
                // Notification pour l'infirmier (doit inclure les remarques)
                // Notification pour l'infirmier - INCLURE LES REMARQUES
                DatabaseHelper.AddNotification(new Notification
                {
                    UserId = 3, // Infirmier
                    Title = "📝 Nouvelle prescription avec remarques",
                    Message = $"Nouvelle prescription pour {patient.FullName} créée par Dr. {currentUser.LastName}\n" +
                             $"Prescription ID: {prescriptionId}\n" +
                             $"Médicaments: {currentMedicationsList.Items.Count}\n" +
                             $"Quantité totale: {totalQuantity} unités\n" +
                             (totalProblems > 0 ? $"⚠️ Problèmes de stock détectés: {totalProblems}\n" : "") +
                             (!string.IsNullOrEmpty(remarksTextBox.Text.Trim()) ?
                              $"📝 REMARQUES DU MÉDECIN:\n{remarksTextBox.Text.Trim()}" :
                              "📝 Aucune remarque supplémentaire"),
                    Type = "Info",
                    CreatedAt = DateTime.Now,
                    RelatedPrescriptionId = prescriptionId,
                    RelatedPatientId = patient.Id,
                    SenderId = currentUser.Id,
                    SenderName = $"Dr. {currentUser.LastName}",
                    IsUrgent = false
                });
                DatabaseHelper.AddNotification(new Notification
                {
                    UserId = 4, // Pharmacie
                    Title = "💊 Prescription à préparer",
                    Message = $"Préparer les médicaments pour {patient.FullName}\n" +
                             $"Prescription ID: {prescriptionId}\n" +
                             $"Nombre de médicaments: {currentMedicationsList.Items.Count}\n" +
                             $"Quantité totale: {totalQuantity} unités" +
                             (unavailableMeds.Count > 0 ? $"\n❌ CRITIQUE: {unavailableMeds.Count} médicament(s) non disponible(s)" : "") +
                             (insufficientStockMeds.Count > 0 ? $"\n❌ PROBLÈME: {insufficientStockMeds.Count} médicament(s) en quantité insuffisante" : "") +
                             (lowStockMeds.Count > 0 ? $"\n⚠️ ALERTE: {lowStockMeds.Count} médicament(s) en stock faible" : ""),
                    Type = "Info",
                    CreatedAt = DateTime.Now,
                    RelatedPrescriptionId = prescriptionId,
                    RelatedPatientId = patient.Id,
                    SenderId = currentUser.Id,
                    SenderName = $"Dr. {currentUser.LastName}"
                });

                // Ajouter une activité
                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = currentUser.FullName,
                    Action = "Prescription créée",
                    Details = $"Patient: {patient.FullName}, Prescription ID: {prescriptionId}, " +
                             $"Médicaments: {currentMedicationsList.Items.Count}, Quantité totale: {totalQuantity}" +
                             (unavailableMeds.Count > 0 ? $", Non disponibles: {unavailableMeds.Count}" : "") +
                             (insufficientStockMeds.Count > 0 ? $", Quantité insuffisante: {insufficientStockMeds.Count}" : "") +
                             (lowStockMeds.Count > 0 ? $", Stock faible: {lowStockMeds.Count}" : "")
                });

                string successMessage = "✅ Prescription créée avec succès !\n\n";
                successMessage += "Récapitulatif:\n";
                successMessage += $"• Patient: {patient.FullName}\n";
                successMessage += $"• Prescription ID: {prescriptionId}\n";
                successMessage += $"• Médicaments prescrits: {currentMedicationsList.Items.Count}\n";
                successMessage += $"• Quantité totale: {totalQuantity} unités\n";

                if (unavailableMeds.Count > 0)
                {
                    successMessage += $"• ❌ {unavailableMeds.Count} médicament(s) non disponible(s)\n";
                }
                if (insufficientStockMeds.Count > 0)
                {
                    successMessage += $"• ❌ {insufficientStockMeds.Count} médicament(s) en quantité insuffisante\n";
                }
                if (lowStockMeds.Count > 0)
                {
                    successMessage += $"• ⚠️ {lowStockMeds.Count} médicament(s) en stock faible\n";
                }

                successMessage += "\nLe patient a été déplacé dans l'historique.";

                MessageBox.Show(successMessage, "Succès", MessageBoxButton.OK,
                               (unavailableMeds.Count > 0 || insufficientStockMeds.Count > 0) ?
                                   MessageBoxImage.Warning : MessageBoxImage.Information);

                // CORRECTION: Désactiver le bouton "Créer Prescription" dans la sidebar
                BtnCreatePrescription.IsEnabled = false;

                // Retourner à la liste des patients (qui ne montrera plus ce patient)
                ShowPatientList();
            };
            summaryPanel.Children.Add(validateButton);

            // Bouton annuler
            Button cancelButton = new Button
            {
                Content = "❌ Annuler",
                Style = (Style)this.Resources["DangerButtonStyle"],
                Margin = new Thickness(0, 10, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(15, 8, 15, 8)
            };
            cancelButton.Click += (s, e) => ShowPatientFile(patientId, fromHistory: false);
            summaryPanel.Children.Add(cancelButton);

            summaryCard.Child = summaryPanel;
            ContentPanel.Children.Add(summaryCard);
        }

        // CORRECTION: Méthode pour mettre à jour le compteur de médicaments
        private void UpdateMedicationCount()
        {
            if (currentInstructionsText != null && currentMedicationsList != null)
            {
                currentInstructionsText.Text = $"Patient: {selectedPatient?.FullName ?? "Patient"}\n" +
                                             $"Médicaments ajoutés: {currentMedicationsList.Items.Count}\n\n" +
                                             "Vérifiez les informations ci-dessus avant de valider la prescription.\n" +
                                             "⚠️ Après validation, le patient sera déplacé dans l'historique.";
            }
        }

        private void ShowHistory()
        {
            ContentPanel.Children.Clear();

            // CORRECTION: Désactiver le bouton "Créer Prescription" quand on est dans l'historique
            BtnCreatePrescription.IsEnabled = false;
            selectedPatient = null;

            // Compter les patients dans l'historique
            var historicalPatients = DatabaseHelper.GetPatients()
                .Count(p => p.AssignedDoctorId == currentUser.Id &&
                           (p.Status == "Prescription créée" || p.Status == "Traitement administré"));

            TextBlock title = new TextBlock
            {
                Text = $"📋 Historique des Patients Consultés ({historicalPatients} patients)",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            // Récupérer TOUS les patients qui ont été assignés à ce médecin
            var allPatients = DatabaseHelper.GetPatients()
                .Where(p => p.AssignedDoctorId == currentUser.Id &&
                           (p.Status == "Prescription créée" || p.Status == "Traitement administré" || p.Status == "En consultation"))
                .OrderByDescending(p => p.RegistrationDate)
                .ToList();

            var prescriptions = DatabaseHelper.GetPrescriptions()
                .Where(p => p.DoctorId == currentUser.Id)
                .ToList();

            if (allPatients.Count == 0)
            {
                TextBlock emptyText = new TextBlock
                {
                    Text = "Aucun patient dans votre historique",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                ContentPanel.Children.Add(emptyText);
                return;
            }

            // Grouper par statut pour une meilleure organisation
            var groups = allPatients.GroupBy(p => p.Status)
                                  .OrderBy(g => g.Key == "Prescription créée" ? 0 :
                                                g.Key == "Traitement administré" ? 1 :
                                                g.Key == "En consultation" ? 2 : 3);

            foreach (var group in groups)
            {
                // En-tête du groupe
                TextBlock groupTitle = new TextBlock
                {
                    Text = GetGroupTitle(group.Key),
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = GetGroupColor(group.Key),
                    Margin = new Thickness(0, 20, 0, 15),
                    Padding = new Thickness(10)
                };
                ContentPanel.Children.Add(groupTitle);

                foreach (var patient in group.OrderByDescending(p => p.RegistrationDate))
                {
                    // Trouver la prescription associée si elle existe
                    var patientPrescription = prescriptions.FirstOrDefault(p => p.PatientId == patient.Id);

                    Border patientCard = new Border
                    {
                        Style = (Style)this.Resources["CardStyle"],
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    StackPanel cardPanel = new StackPanel();

                    // Informations patient
                    TextBlock patientInfo = new TextBlock
                    {
                        Text = $"{patient.FullName} - {patient.Age} ans",
                        FontSize = 18,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    cardPanel.Children.Add(patientInfo);

                    TextBlock details = new TextBlock
                    {
                        Text = $"Service: {patient.ServiceName} • Arrivé le: {patient.RegistrationDate:dd/MM/yyyy HH:mm}",
                        FontSize = 14,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    cardPanel.Children.Add(details);

                    // Statut
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
                        Text = patient.Status,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White
                    };
                    statusBadge.Child = statusText;
                    cardPanel.Children.Add(statusBadge);

                    // Afficher les informations de prescription si disponible
                    if (patientPrescription != null)
                    {
                        TextBlock prescTitle = new TextBlock
                        {
                            Text = "Prescription:",
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 5, 0, 5)
                        };
                        cardPanel.Children.Add(prescTitle);

                        foreach (var med in patientPrescription.Medications.Take(3)) // Limiter à 3 médicaments
                        {
                            // CORRECTION: Afficher correctement les informations séparées
                            TextBlock medItem = new TextBlock
                            {
                                Text = $"• {med.MedicationName} {med.Dosage} - {med.Frequency} - {med.Duration}",
                                Margin = new Thickness(10, 0, 0, 2),
                                FontSize = 13
                            };
                            cardPanel.Children.Add(medItem);
                        }

                        if (patientPrescription.Medications.Count > 3)
                        {
                            TextBlock moreText = new TextBlock
                            {
                                Text = $"... et {patientPrescription.Medications.Count - 3} autres médicaments",
                                FontSize = 12,
                                Foreground = Brushes.Gray,
                                Margin = new Thickness(10, 0, 0, 5)
                            };
                            cardPanel.Children.Add(moreText);
                        }

                        TextBlock prescStatus = new TextBlock
                        {
                            Text = $"Statut prescription: {patientPrescription.Status}",
                            FontSize = 13,
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 5, 0, 0),
                            Foreground = patientPrescription.Status == "Administrée" ? Brushes.Green : Brushes.Orange
                        };
                        cardPanel.Children.Add(prescStatus);
                    }

                    // Boutons d'action
                    StackPanel buttonPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                    // Voir dossier
                    Button viewBtn = new Button
                    {
                        Content = "📄 Voir dossier",
                        Style = (Style)this.Resources["InfoButtonStyle"],
                        Width = 130,
                        Margin = new Thickness(0, 0, 10, 0),
                        Tag = patient.Id
                    };
                    viewBtn.Click += (s, e) => ShowPatientFile(patient.Id, fromHistory: true);
                    buttonPanel.Children.Add(viewBtn);

                    // Si le patient a une prescription, montrer le bouton pour la voir
                    if (patientPrescription != null)
                    {
                        Button viewPrescBtn = new Button
                        {
                            Content = "📋 Voir prescription",
                            Style = (Style)this.Resources["PrimaryButtonStyle"],
                            Width = 150,
                            Tag = patientPrescription.Id
                        };
                        viewPrescBtn.Click += (s, e) => ViewPrescriptionDetails(patientPrescription.Id);
                        buttonPanel.Children.Add(viewPrescBtn);
                    }

                    cardPanel.Children.Add(buttonPanel);
                    patientCard.Child = cardPanel;
                    ContentPanel.Children.Add(patientCard);
                }
            }
        }

        private string GetGroupTitle(string status)
        {
            return status switch
            {
                "Prescription créée" => "📝 Prescriptions Créées (En attente de préparation)",
                "Traitement administré" => "✅ Traitements Administrés",
                "En consultation" => "👨‍⚕️ En Cours de Consultation",
                _ => $"📋 {status}"
            };
        }

        private Brush GetGroupColor(string status)
        {
            return status switch
            {
                "Prescription créée" => new SolidColorBrush(Color.FromRgb(255, 165, 0)), // Orange
                "Traitement administré" => new SolidColorBrush(Color.FromRgb(46, 204, 113)), // Vert
                "En consultation" => new SolidColorBrush(Color.FromRgb(52, 152, 219)), // Bleu
                _ => new SolidColorBrush(Color.FromRgb(13, 92, 99)) // Teal
            };
        }

        private Brush GetStatusColor(string status)
        {
            return status switch
            {
                "En attente" => Brushes.Orange,
                "En consultation" => Brushes.LightBlue,
                "Prescription créée" => Brushes.LightGreen,
                "Traitement administré" => Brushes.Green,
                "Prise en charge" => Brushes.LightBlue,
                _ => Brushes.LightGray
            };
        }

        private void ViewPrescriptionDetails(int prescriptionId)
        {
            var prescription = DatabaseHelper.GetPrescriptions().FirstOrDefault(p => p.Id == prescriptionId);
            if (prescription == null) return;

            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = $"📋 Détails de la Prescription #{prescription.Id}",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            Border card = new Border { Style = (Style)this.Resources["CardStyle"] };
            StackPanel panel = new StackPanel();

            // Informations générales
            panel.Children.Add(CreateInfoField("Patient", prescription.PatientName));
            panel.Children.Add(CreateInfoField("Médecin", prescription.DoctorName));
            panel.Children.Add(CreateInfoField("Date de création", prescription.CreationDate.ToString("dd/MM/yyyy HH:mm")));
            panel.Children.Add(CreateInfoField("Statut", prescription.Status));

            // Liste des médicaments
            TextBlock medTitle = new TextBlock
            {
                Text = "Médicaments prescrits:",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 15, 0, 10)
            };
            panel.Children.Add(medTitle);

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

                // CORRECTION: Afficher correctement toutes les informations séparées
                TextBlock medText = new TextBlock
                {
                    Text = $"• {med.MedicationName} {med.Dosage}\n" +
                           $"  Fréquence: {med.Frequency}, Durée: {med.Duration}, Quantité: {med.Quantity}",
                    FontSize = 14
                };
                medCard.Child = medText;
                panel.Children.Add(medCard);
            }

            // Notes si disponibles
            if (!string.IsNullOrEmpty(prescription.Notes))
            {
                TextBlock notesTitle = new TextBlock
                {
                    Text = "Notes:",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 15, 0, 10)
                };
                panel.Children.Add(notesTitle);

                TextBlock notesText = new TextBlock
                {
                    Text = prescription.Notes,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                panel.Children.Add(notesText);
            }

            // Bouton retour
            Button backBtn = new Button
            {
                Content = "↩ Retour à l'historique",
                Style = (Style)this.Resources["PrimaryButtonStyle"],
                Width = 180,
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            backBtn.Click += (s, e) => ShowHistory();

            panel.Children.Add(backBtn);
            card.Child = panel;
            ContentPanel.Children.Add(card);
        }

        private void ShowNotifications()
        {
            ContentPanel.Children.Clear();

            // CORRECTION: Désactiver le bouton "Créer Prescription" quand on est dans les notifications
            BtnCreatePrescription.IsEnabled = false;
            selectedPatient = null;

            TextBlock title = new TextBlock
            {
                Text = "📬 Notifications",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(title);

            var notifications = DatabaseHelper.GetNotifications()
                .Where(n => n.UserId == currentUser.Id)
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
                ContentPanel.Children.Add(CreateNotificationCard(notification));
            }

            // Bouton pour marquer tout comme lu
            Button markAllReadBtn = new Button
            {
                Content = "Marquer tout comme lu",
                Style = (Style)this.Resources["PrimaryButtonStyle"],
                Width = 180,
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            markAllReadBtn.Click += (s, e) =>
            {
                var allNotifications = DatabaseHelper.GetNotifications();
                foreach (var notif in allNotifications.Where(n => n.UserId == currentUser.Id && !n.IsRead))
                {
                    notif.IsRead = true;
                }
                DatabaseHelper.SaveNotifications(allNotifications);
                ShowNotifications();
                UpdateNotificationBadge();
            };
            ContentPanel.Children.Add(markAllReadBtn);
        }

        private Border CreateNotificationCard(Notification notification)
        {
            Border card = new Border
            {
                Background = notification.IsRead ? Brushes.White : new SolidColorBrush(Color.FromArgb(30, 32, 156, 238)),
                BorderBrush = notification.IsUrgent ? Brushes.Red : Brushes.LightGray,
                BorderThickness = new Thickness(notification.IsUrgent ? 2 : 1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10)
            };

            StackPanel panel = new StackPanel();

            // En-tête avec icône
            StackPanel headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBlock icon = new TextBlock
            {
                Text = notification.IsUrgent ? "⚠️" : (notification.Type == "DoctorReply" ? "📬" : "💬"),
                FontSize = 20,
                Margin = new Thickness(0, 0, 10, 0)
            };

            StackPanel titlePanel = new StackPanel();
            TextBlock titleText = new TextBlock
            {
                Text = notification.Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = notification.IsUrgent ? Brushes.Red : Brushes.Black
            };

            TextBlock senderText = new TextBlock
            {
                Text = $"De: {notification.SenderName}",
                FontSize = 12,
                Foreground = Brushes.Gray
            };

            titlePanel.Children.Add(titleText);
            titlePanel.Children.Add(senderText);
            headerPanel.Children.Add(icon);
            headerPanel.Children.Add(titlePanel);

            // Message
            TextBlock messageText = new TextBlock
            {
                Text = notification.Message,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Informations supplémentaires
            StackPanel infoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBlock timeText = new TextBlock
            {
                Text = notification.TimeAgo,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 20, 0)
            };

            // Si la notification est liée à un patient, afficher le lien
            if (notification.RelatedPatientId.HasValue)
            {
                var patient = DatabaseHelper.GetPatients()
                    .FirstOrDefault(p => p.Id == notification.RelatedPatientId.Value);

                if (patient != null)
                {
                    TextBlock patientText = new TextBlock
                    {
                        Text = $"Patient: {patient.FullName}",
                        FontSize = 12,
                        Foreground = Brushes.Blue,
                        Cursor = System.Windows.Input.Cursors.Hand,
                        TextDecorations = System.Windows.TextDecorations.Underline
                    };

                    patientText.MouseLeftButtonUp += (s, e) =>
                    {
                        // CORRECTION: Ouvrir le dossier patient avec le bon contexte
                        ShowPatientFile(patient.Id, fromHistory: patient.Status == "Prescription créée" || patient.Status == "Traitement administré");
                    };

                    infoPanel.Children.Add(patientText);
                }
            }

            infoPanel.Children.Add(timeText);

            // Actions
            StackPanel actionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            if (!notification.IsRead)
            {
                Button markReadBtn = new Button
                {
                    Content = "Marquer comme lu",
                    Style = (Style)this.Resources["InfoButtonStyle"],
                    Width = 140,
                    Margin = new Thickness(0, 0, 10, 0),
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
                        UpdateNotificationBadge();
                        ShowNotifications();
                    }
                };
                actionPanel.Children.Add(markReadBtn);
            }

            // Bouton pour répondre si c'est une remarque d'infirmier
            if (notification.SenderId.HasValue && notification.Type == "NurseRemark")
            {
                Button replyBtn = new Button
                {
                    Content = "📝 Répondre",
                    Style = (Style)this.Resources["SuccessButtonStyle"],
                    Width = 120,
                    Tag = notification
                };
                replyBtn.Click += (s, e) =>
                {
                    ReplyToNurse(notification);
                };
                actionPanel.Children.Add(replyBtn);
            }

            // Bouton pour supprimer
            Button deleteBtn = new Button
            {
                Content = "🗑️ Supprimer",
                Style = (Style)this.Resources["DangerButtonStyle"],
                Width = 120,
                Tag = notification.Id
            };
            deleteBtn.Click += (s, e) =>
            {
                var allNotifications = DatabaseHelper.GetNotifications();
                allNotifications.RemoveAll(n => n.Id == notification.Id);
                DatabaseHelper.SaveNotifications(allNotifications);
                ShowNotifications();
                UpdateNotificationBadge();
            };
            actionPanel.Children.Add(deleteBtn);

            // Assemblage
            panel.Children.Add(headerPanel);
            panel.Children.Add(messageText);
            panel.Children.Add(infoPanel);
            panel.Children.Add(actionPanel);

            card.Child = panel;
            return card;
        }

        private void ReplyToNurse(Notification notification)
        {
            var nurse = DatabaseHelper.GetUsers().FirstOrDefault(u => u.Id == notification.SenderId);
            var patient = DatabaseHelper.GetPatients().FirstOrDefault(p => p.Id == notification.RelatedPatientId);

            if (nurse == null || patient == null) return;

            ContentPanel.Children.Clear();

            TextBlock title = new TextBlock
            {
                Text = $"📝 Répondre à Inf. {nurse.LastName}",
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

            // Informations
            panel.Children.Add(CreateInfoField("À", $"Inf. {nurse.FullName}"));
            panel.Children.Add(CreateInfoField("Patient", patient.FullName));
            panel.Children.Add(CreateInfoField("Chambre", patient.RoomNumber.ToString()));

            // Message original
            panel.Children.Add(new TextBlock
            {
                Text = "Message original:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 20, 0, 10)
            });

            Border originalMessage = new Border
            {
                Background = Brushes.LightGray,
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 0, 20)
            };

            TextBlock originalText = new TextBlock
            {
                Text = notification.Message,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };

            originalMessage.Child = originalText;
            panel.Children.Add(originalMessage);

            // Zone de réponse
            panel.Children.Add(new TextBlock
            {
                Text = "Votre réponse:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            TextBox replyTextBox = new TextBox
            {
                Height = 150,
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                FontSize = 14,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            panel.Children.Add(replyTextBox);

            // Boutons
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button sendBtn = new Button
            {
                Content = "📤 Envoyer la réponse",
                Style = (Style)this.Resources["SuccessButtonStyle"],
                Width = 180,
                Margin = new Thickness(0, 0, 10, 0)
            };

            sendBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(replyTextBox.Text))
                {
                    MessageBox.Show("Veuillez saisir une réponse.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Envoyer notification de réponse à l'infirmier
                DatabaseHelper.AddNotification(new Notification
                {
                    UserId = nurse.Id,
                    Title = "📬 Réponse du médecin",
                    Message = $"De: Dr. {currentUser.LastName}\n" +
                             $"Patient: {patient.FullName}\n\n" +
                             $"Votre message: {notification.Message}\n\n" +
                             $"Réponse du médecin:\n{replyTextBox.Text}",
                    Type = "DoctorReply",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    RelatedPatientId = patient.Id,
                    SenderId = currentUser.Id,
                    SenderName = $"Dr. {currentUser.LastName}",
                    IsUrgent = false
                });

                // Marquer la notification originale comme lue
                notification.IsRead = true;
                var allNotifications = DatabaseHelper.GetNotifications();
                var index = allNotifications.FindIndex(n => n.Id == notification.Id);
                if (index >= 0)
                {
                    allNotifications[index] = notification;
                    DatabaseHelper.SaveNotifications(allNotifications);
                }

                // Ajouter une activité
                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = currentUser.FullName,
                    Action = "Réponse à infirmier",
                    Details = $"À: Inf. {nurse.LastName}, Patient: {patient.FullName}"
                });

                MessageBox.Show("Réponse envoyée à l'infirmier!",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                ShowNotifications();
                UpdateNotificationBadge();
            };

            Button cancelBtn = new Button
            {
                Content = "Annuler",
                Style = (Style)this.Resources["DangerButtonStyle"],
                Width = 120
            };
            cancelBtn.Click += (s, e) => ShowNotifications();

            buttonPanel.Children.Add(sendBtn);
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

        private void AddInfoField(StackPanel panel, string label, string value)
        {
            // Créer un conteneur pour le champ
            Border fieldContainer = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 8, 0, 8),
                Margin = new Thickness(0, 0, 0, 5)
            };

            StackPanel fieldPanel = new StackPanel();

            // Label
            TextBlock labelText = new TextBlock
            {
                Text = label + ":",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            };
            fieldPanel.Children.Add(labelText);

            // Value - avec TextWrapping pour les longs textes
            TextBlock valueText = new TextBlock
            {
                Text = value,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10, 0, 0, 0)
            };
            fieldPanel.Children.Add(valueText);

            fieldContainer.Child = fieldPanel;
            panel.Children.Add(fieldContainer);
        }
    }
}