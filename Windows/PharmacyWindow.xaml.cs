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
    public partial class PharmacyWindow : Window
    {
       // public Button BtnHistory { get; private set; }

        public PharmacyWindow()
        {
            InitializeComponent();
            this.PreviewKeyDown += PharmacyWindow_PreviewKeyDown;
            ShowPrepareOrders();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BtnPrepareOrders?.Focus();
        }

        private void PharmacyWindow_PreviewKeyDown(object sender, KeyEventArgs e)
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
                    ShowPrepareOrders();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                {
                    ShowStock();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                {
                    ShowIncoming();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D4 || e.Key == Key.NumPad4) // AJOUTER ce raccourci
                {
                    ShowHistory();
                    e.Handled = true;
                    return;
                }
            }
        }
        private void SetActiveNav(Button active)
        {
            var activeBrush = new SolidColorBrush(Color.FromRgb(10, 74, 80));
            var normalBrush = Brushes.Transparent;

            if (BtnPrepareOrders != null) BtnPrepareOrders.Background = normalBrush;
            if (BtnStock != null) BtnStock.Background = normalBrush;
            if (BtnIncoming != null) BtnIncoming.Background = normalBrush;
            if (BtnHistory != null) BtnHistory.Background = normalBrush; // AJOUTER cette ligne

            if (active != null) active.Background = activeBrush;
        }
        private void ScrollToTop()
        {
            if (MainScrollViewer == null) return;

            Dispatcher.BeginInvoke(
                new Action(() => MainScrollViewer.ScrollToTop()),
                DispatcherPriority.Background);
        }

        private void BtnPrepareOrders_Click(object sender, RoutedEventArgs e) => ShowPrepareOrders();
        private void BtnStock_Click(object sender, RoutedEventArgs e) => ShowStock();
        private void BtnIncoming_Click(object sender, RoutedEventArgs e) => ShowIncoming();

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ShowPrepareOrders()
        {
            SetActiveNav(BtnPrepareOrders);
            ContentPanel.Children.Clear();
            ScrollToTop();

            ContentPanel.Children.Add(CreateTitle("Prescriptions à Préparer"));

            var prescriptions = DatabaseHelper.GetPrescriptions()
                .Where(p => p.Status == "Créée")
                .ToList();

            if (prescriptions.Count == 0)
            {
                TextBlock emptyText = new TextBlock
                {
                    Text = "Aucune prescription à préparer",
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

                ContentPanel.Children.Add(CreateOrderCard(prescription, patient));
            }

            // Boutons d'action
            var actionCard = new Border { Style = (Style)this.Resources["CardStyle"] };
            var actionWrap = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var refreshBtn = new Button
            {
                Content = "🔄 Actualiser",
                Style = (Style)this.Resources["PrimaryButton"],
                Margin = new Thickness(5)
            };
            refreshBtn.Click += (s, e) => ShowPrepareOrders();
            actionWrap.Children.Add(refreshBtn);

            actionCard.Child = actionWrap;
            ContentPanel.Children.Add(actionCard);
        }

        private void ShowStock()
        {
            SetActiveNav(BtnStock);
            ContentPanel.Children.Clear();
            ScrollToTop();

            ContentPanel.Children.Add(CreateTitle("Gestion du Stock"));

            var medications = DatabaseHelper.GetMedications();

            if (medications.Count == 0)
            {
                TextBlock emptyText = new TextBlock
                {
                    Text = "Aucun médicament en stock",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                ContentPanel.Children.Add(emptyText);
                return;
            }

            // Barre de recherche
            Border searchCard = new Border
            {
                Style = (Style)this.Resources["CardStyle"],
                Margin = new Thickness(0, 0, 0, 16),
                Name = "SearchCard"
            };

            StackPanel searchPanel = new StackPanel();

            TextBlock searchTitle = new TextBlock
            {
                Text = "🔍 Rechercher un médicament",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            searchPanel.Children.Add(searchTitle);

            Grid searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBox searchTextBox = new TextBox
            {
                Height = 40,
                FontSize = 14,
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 0, 10, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(searchTextBox, 0);

            Button searchButton = new Button
            {
                Content = "Rechercher",
                Style = (Style)this.Resources["PrimaryButton"],
                Height = 40,
                Padding = new Thickness(20, 0, 20, 0)
            };
            Grid.SetColumn(searchButton, 1);

            // Variable pour stocker tous les médicaments
            var allMedications = medications;

            searchButton.Click += (s, e) =>
            {
                string searchTerm = searchTextBox.Text.Trim().ToLower();

                if (string.IsNullOrEmpty(searchTerm))
                {
                    RefreshStockTable(allMedications);
                }
                else
                {
                    var filteredMeds = allMedications.Where(m =>
                        m.Name.ToLower().Contains(searchTerm) ||
                        m.Code.ToLower().Contains(searchTerm) ||
                        m.Description.ToLower().Contains(searchTerm))
                        .ToList();

                    RefreshStockTable(filteredMeds);
                }
            };

            // Recherche en temps réel
            searchTextBox.TextChanged += (s, e) =>
            {
                string searchTerm = searchTextBox.Text.Trim().ToLower();

                if (string.IsNullOrEmpty(searchTerm))
                {
                    RefreshStockTable(allMedications);
                }
                else
                {
                    var filteredMeds = allMedications.Where(m =>
                        m.Name.ToLower().Contains(searchTerm) ||
                        m.Code.ToLower().Contains(searchTerm) ||
                        m.Description.ToLower().Contains(searchTerm))
                        .ToList();

                    RefreshStockTable(filteredMeds);
                }
            };

            searchGrid.Children.Add(searchTextBox);
            searchGrid.Children.Add(searchButton);
            searchPanel.Children.Add(searchGrid);
            searchCard.Child = searchPanel;
            ContentPanel.Children.Add(searchCard);

            // Créer le conteneur principal
            var stockContainer = new StackPanel();

            // En-tête du tableau
            var headerGrid = CreateStockGrid(isHeader: true);
            AddHeaderCell(headerGrid, "Code", 0);
            AddHeaderCell(headerGrid, "Nom", 1);
            AddHeaderCell(headerGrid, "Description", 2);
            AddHeaderCell(headerGrid, "Quantité", 3);
            AddHeaderCell(headerGrid, "Seuil Min", 4);
            AddHeaderCell(headerGrid, "Expiration", 5);
            AddHeaderCell(headerGrid, "Statut", 6);
            stockContainer.Children.Add(headerGrid);

            if (allMedications.Count == 0)
            {
                stockContainer.Children.Add(new TextBlock
                {
                    Text = "Aucun médicament en stock",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
            }
            else
            {
                foreach (var med in allMedications)
                {
                    stockContainer.Children.Add(CreateStockRow(med));
                }

                stockContainer.Children.Add(new TextBlock
                {
                    Text = $"📊 {allMedications.Count} médicament(s)",
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }

            var stockCard = new Border
            {
                Style = (Style)this.Resources["CardStyle"],
                Child = stockContainer,
                Name = "StockTableCard"
            };
            ContentPanel.Children.Add(stockCard);

            // Section de réapprovisionnement améliorée
            var restockCard = new Border { Style = (Style)this.Resources["CardStyle"], Margin = new Thickness(0, 20, 0, 0) };
            var restockPanel = new StackPanel();

            restockPanel.Children.Add(new TextBlock
            {
                Text = "📦 Gestion des stocks",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var tabControl = new TabControl
            {
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Tab 1 : Réapprovisionnement rapide
            TabItem quickRestockTab = new TabItem
            {
                Header = "Réapprovisionnement rapide"
            };

            var quickRestockPanel = new StackPanel { Margin = new Thickness(10) };

            var quickRestockWrap = new WrapPanel();

            Button restockParacetamolBtn = new Button
            {
                Content = "➕ Paracétamol (+50)",
                Style = (Style)this.Resources["SuccessButton"],
                Margin = new Thickness(5),
                Tag = "PARA500"
            };
            restockParacetamolBtn.Click += (s, e) => RestockMedication("PARA500", 50);
            quickRestockWrap.Children.Add(restockParacetamolBtn);

            Button restockIbuprofenBtn = new Button
            {
                Content = "➕ Ibuprofène (+50)",
                Style = (Style)this.Resources["SuccessButton"],
                Margin = new Thickness(5),
                Tag = "IBUP400"
            };
            restockIbuprofenBtn.Click += (s, e) => RestockMedication("IBUP400", 50);
            quickRestockWrap.Children.Add(restockIbuprofenBtn);

            Button restockAmoxicillinBtn = new Button
            {
                Content = "➕ Amoxicilline (+30)",
                Style = (Style)this.Resources["SuccessButton"],
                Margin = new Thickness(5),
                Tag = "AMOX1G"
            };
            restockAmoxicillinBtn.Click += (s, e) => RestockMedication("AMOX1G", 30);
            quickRestockWrap.Children.Add(restockAmoxicillinBtn);

            quickRestockPanel.Children.Add(quickRestockWrap);
            quickRestockTab.Content = quickRestockPanel;

            // Tab 2 : Ajout manuel
            TabItem manualRestockTab = new TabItem
            {
                Header = "Ajouter manuellement"
            };

            var manualRestockPanel = new StackPanel { Margin = new Thickness(10) };

            manualRestockPanel.Children.Add(new TextBlock
            {
                Text = "Sélectionner un médicament:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            ComboBox medicationCombo = new ComboBox
            {
                ItemsSource = allMedications.Select(m => $"{m.Name} ({m.Code})"),
                Height = 35,
                Margin = new Thickness(0, 0, 0, 15)
            };
            if (medicationCombo.Items.Count > 0)
                medicationCombo.SelectedIndex = 0;
            manualRestockPanel.Children.Add(medicationCombo);

            manualRestockPanel.Children.Add(new TextBlock
            {
                Text = "Quantité à ajouter:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            TextBox quantityTextBox = new TextBox
            {
                Text = "1",
                Height = 35,
                Margin = new Thickness(0, 0, 0, 15)
            };
            manualRestockPanel.Children.Add(quantityTextBox);

            Button addManuallyBtn = new Button
            {
                Content = "➕ Ajouter au stock",
                Style = (Style)this.Resources["SuccessButton"],
                Width = 180,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            addManuallyBtn.Click += (s, e) =>
            {
                if (medicationCombo.SelectedItem == null)
                {
                    MessageBox.Show("Veuillez sélectionner un médicament.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!int.TryParse(quantityTextBox.Text, out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("Veuillez entrer une quantité valide (nombre positif).",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string selectedItem = medicationCombo.SelectedItem.ToString();
                string code = selectedItem.Split('(')[1].TrimEnd(')');

                RestockMedication(code, quantity);
            };
            manualRestockPanel.Children.Add(addManuallyBtn);

            Button addNewMedicationBtn = new Button
            {
                Content = "➕ Nouveau médicament",
                Style = (Style)this.Resources["PrimaryButton"],
                Width = 180,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 10, 0, 0)
            };
            addNewMedicationBtn.Click += (s, e) =>
            {
                var dialog = new AddEditMedicationDialog();
                if (dialog.ShowDialog() == true)
                {
                    var existingMeds = DatabaseHelper.GetMedications();

                    if (existingMeds.Any(m => m.Code == dialog.Code))
                    {
                        MessageBox.Show($"Le médicament avec le code {dialog.Code} existe déjà.",
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    existingMeds.Add(new Medication
                    {
                        Code = dialog.Code,
                        Name = dialog.Name,
                        Description = dialog.Description,
                        Quantity = dialog.Quantity,
                        MinThreshold = dialog.MinThreshold,
                        ExpirationDate = dialog.ExpirationDate,
                        Status = dialog.Quantity < dialog.MinThreshold ? "Faible stock" : "Disponible"
                    });

                    DatabaseHelper.SaveMedications(existingMeds);

                    DatabaseHelper.AddActivity(new Activity
                    {
                        DateTime = DateTime.Now,
                        User = "Pharmacie",
                        Action = "Nouveau médicament ajouté",
                        Details = $"Médicament: {dialog.Name} ({dialog.Code}), Quantité: {dialog.Quantity}"
                    });

                    MessageBox.Show($"Médicament {dialog.Name} ajouté avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                    ShowStock();
                }
            };
            manualRestockPanel.Children.Add(addNewMedicationBtn);

            manualRestockTab.Content = manualRestockPanel;

            tabControl.Items.Add(quickRestockTab);
            tabControl.Items.Add(manualRestockTab);

            restockPanel.Children.Add(tabControl);
            restockCard.Child = restockPanel;
            ContentPanel.Children.Add(restockCard);
        }

        private void ShowIncoming()
        {
            SetActiveNav(BtnIncoming);
            ContentPanel.Children.Clear();
            ScrollToTop();

            ContentPanel.Children.Add(CreateTitle("🚚 Arrivages & Demandes de Médicaments"));

            var arrivalCard = new Border { Style = (Style)this.Resources["CardStyle"], Margin = new Thickness(0, 0, 0, 20) };
            var arrivalPanel = new StackPanel();

            arrivalPanel.Children.Add(new TextBlock
            {
                Text = "📅 Prochain arrivage prévu",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 10)
            });

            arrivalPanel.Children.Add(new TextBlock
            {
                Text = "Date: " + DateTime.Now.AddDays(7).ToString("dddd d MMMM yyyy"),
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 15)
            });

            var arrivalGrid = new Grid();
            arrivalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            arrivalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            arrivalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            arrivalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            arrivalGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddArrivalHeaderWithWidth(arrivalGrid, "", 0, 40);
            AddArrivalHeaderWithWidth(arrivalGrid, "Médicament", 1, 250);
            AddArrivalHeaderWithWidth(arrivalGrid, "Quantité", 2, 120);
            AddArrivalHeaderWithWidth(arrivalGrid, "Statut", 3, 100);

            var arrivals = new[]
            {
                new { Icon = "💊", Name = "Paracétamol ", Quantity = "1000 unités", Status = "Confirmé" },
                new { Icon = "💊", Name = "Amoxicilline ", Quantity = "500 unités", Status = "Confirmé" },
                new { Icon = "💊", Name = "Ibuprofène ", Quantity = "750 unités", Status = "Confirmé" },
                new { Icon = "⭐", Name = "Aspirine  (Nouveau)", Quantity = "600 unités", Status = "Confirmé" }
            };

            for (int i = 0; i < arrivals.Length; i++)
            {
                arrivalGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                int row = i + 1;

                AddArrivalCell(arrivalGrid, arrivals[i].Icon, 0, row, 40);
                AddArrivalCellWrapped(arrivalGrid, arrivals[i].Name, 1, row, 250);
                AddArrivalCell(arrivalGrid, arrivals[i].Quantity, 2, row, 120);

                var statusBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(5),
                    Width = 100,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                statusBorder.Child = new TextBlock
                {
                    Text = arrivals[i].Status,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11,
                    TextAlignment = TextAlignment.Center
                };

                Grid.SetColumn(statusBorder, 3);
                Grid.SetRow(statusBorder, row);
                arrivalGrid.Children.Add(statusBorder);
            }

            arrivalPanel.Children.Add(arrivalGrid);

            StackPanel arrivalButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 20, 0, 0)
            };

            Button simulateArrivalBtn = new Button
            {
                Content = "📦 Simuler l'arrivage",
                Style = (Style)this.Resources["SuccessButton"],
                Width = 180,
                Height = 45,
                FontSize = 14
            };
            simulateArrivalBtn.Click += (s, e) =>
            {
                SimulateArrival();
                MessageBox.Show("Arrivage simulé avec succès! Les stocks ont été mis à jour.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowStock();
            };
            arrivalButtons.Children.Add(simulateArrivalBtn);

            arrivalPanel.Children.Add(arrivalButtons);
            arrivalCard.Child = arrivalPanel;
            ContentPanel.Children.Add(arrivalCard);

            var requestCard = new Border { Style = (Style)this.Resources["CardStyle"] };
            var requestPanel = new StackPanel();

            requestPanel.Children.Add(new TextBlock
            {
                Text = "📝 Faire une demande de médicaments",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 15)
            });

            requestPanel.Children.Add(new TextBlock
            {
                Text = "Médicament demandé:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            TextBox medRequestTextBox = new TextBox
            {
                Height = 35,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15),
                ToolTip = "Nom du médicament ou code"
            };
            requestPanel.Children.Add(medRequestTextBox);

            requestPanel.Children.Add(new TextBlock
            {
                Text = "Quantité demandée:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            TextBox quantityRequestTextBox = new TextBox
            {
                Height = 35,
                FontSize = 14,
                Text = "1",
                Margin = new Thickness(0, 0, 0, 15)
            };
            requestPanel.Children.Add(quantityRequestTextBox);

            requestPanel.Children.Add(new TextBlock
            {
                Text = "Raison/Urgence:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            ComboBox reasonCombo = new ComboBox
            {
                ItemsSource = new[]
                {
                    "Stock épuisé",
                    "Stock faible",
                    "Demande médicale urgente",
                    "Médicament nouveau requis",
                    "Augmentation des besoins",
                    "Autre"
                },
                SelectedIndex = 0,
                Height = 35,
                Margin = new Thickness(0, 0, 0, 20)
            };
            requestPanel.Children.Add(reasonCombo);

            Button sendRequestBtn = new Button
            {
                Content = "📤 Envoyer la demande",
                Style = (Style)this.Resources["PrimaryButton"],
                Width = 180,
                Height = 45,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            sendRequestBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(medRequestTextBox.Text))
                {
                    MessageBox.Show("Veuillez saisir le nom du médicament.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!int.TryParse(quantityRequestTextBox.Text, out int qty) || qty <= 0)
                {
                    MessageBox.Show("Veuillez saisir une quantité valide.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string requestDetails = $"Demande de {medRequestTextBox.Text.Trim()} - " +
                                       $"Quantité: {qty} - " +
                                       $"Raison: {reasonCombo.SelectedItem}";

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = "Pharmacie",
                    Action = "Demande de médicament",
                    Details = requestDetails
                });

                MessageBox.Show($"✅ Demande envoyée avec succès!\n\n" +
                               $"Médicament: {medRequestTextBox.Text.Trim()}\n" +
                               $"Quantité: {qty}\n" +
                               $"Raison: {reasonCombo.SelectedItem}\n\n" +
                               $"Votre demande a été enregistrée et sera traitée.",
                    "Demande envoyée", MessageBoxButton.OK, MessageBoxImage.Information);

                medRequestTextBox.Text = "";
                quantityRequestTextBox.Text = "1";
                reasonCombo.SelectedIndex = 0;
            };

            requestPanel.Children.Add(sendRequestBtn);
            requestCard.Child = requestPanel;
            ContentPanel.Children.Add(requestCard);
        }

        // UI Helpers
        private TextBlock CreateTitle(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                Margin = new Thickness(0, 0, 0, 16)
            };
        }

        private Border CreateOrderCard(Prescription prescription, Patient patient)
        {
            var card = new Border { Style = (Style)this.Resources["CardStyle"], Margin = new Thickness(0, 0, 0, 10) };
            var mainPanel = new StackPanel();

            StackPanel headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };

            Border idBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(13, 92, 99)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 15, 0)
            };
            idBadge.Child = new TextBlock
            {
                Text = $"#{prescription.Id}",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            headerPanel.Children.Add(idBadge);

            StackPanel patientInfo = new StackPanel();
            patientInfo.Children.Add(new TextBlock
            {
                Text = patient.FullName,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            });
            patientInfo.Children.Add(new TextBlock
            {
                Text = $"📞 Tél: {patient.Phone} • 🏥 Service: {patient.ServiceName}",
                FontSize = 13,
                Foreground = Brushes.Gray
            });
            patientInfo.Children.Add(new TextBlock
            {
                Text = $"👨‍⚕️ Médecin: {prescription.DoctorName} • 📅 {prescription.CreationDate:dd/MM/yyyy HH:mm}",
                FontSize = 13,
                Foreground = Brushes.Gray
            });

            headerPanel.Children.Add(patientInfo);
            mainPanel.Children.Add(headerPanel);

            Border medSection = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                Background = new SolidColorBrush(Color.FromArgb(10, 13, 92, 99))
            };

            StackPanel medPanel = new StackPanel();

            medPanel.Children.Add(new TextBlock
            {
                Text = "💊 Médicaments à préparer:",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // ✅ FIX: grid correct (row 0 header + rows 1..N items)
            int totalQuantity = 0;
            var medicationsGrid = BuildMedicationsGrid(prescription, out totalQuantity);
            medPanel.Children.Add(medicationsGrid);

            medPanel.Children.Add(new TextBlock
            {
                Text = $"📦 Total: {totalQuantity} unités pour {prescription.Medications.Count} médicament(s)",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            });

            medSection.Child = medPanel;
            mainPanel.Children.Add(medSection);

            var globalStatus = CheckMedicationStock(prescription);
            Border statusSection = new Border
            {
                Background = globalStatus.Contains("✅") ? new SolidColorBrush(Color.FromArgb(30, 46, 204, 113)) :
                            globalStatus.Contains("⚠️") ? new SolidColorBrush(Color.FromArgb(30, 255, 193, 7)) :
                            new SolidColorBrush(Color.FromArgb(30, 220, 53, 69)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 15)
            };

            statusSection.Child = new TextBlock
            {
                Text = "📋 Disponibilité: " + globalStatus,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = globalStatus.Contains("✅") ? Brushes.Green :
                            globalStatus.Contains("⚠️") ? Brushes.Orange : Brushes.Red
            };

            mainPanel.Children.Add(statusSection);

            WrapPanel buttonWrap = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            Button prepareBtn = new Button
            {
                Content = "✅ PRÉPARER LA COMMANDE",
                Style = (Style)this.Resources["PrimaryButton"],
                Tag = prescription.Id,
                Height = 45,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 8, 8),
                MinWidth = 220,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            prepareBtn.Click += (s, e) => PrepareOrder(prescription.Id);

            if (globalStatus.Contains("❌") || globalStatus.Contains("Stock insuffisants"))
            {
                prepareBtn.IsEnabled = false;
                prepareBtn.Content = "❌ STOCK INSUFFISANT";
                prepareBtn.Style = (Style)this.Resources["DangerButton"];
                prepareBtn.ToolTip = "Certains médicaments ne sont pas disponibles en quantité suffisante";
            }

            buttonWrap.Children.Add(prepareBtn);

            Button problemBtn = new Button
            {
                Content = "🚨 Signaler problème",
                Style = (Style)this.Resources["DangerButton"],
                Tag = prescription.Id,
                Height = 45,
                MinWidth = 160,
                Margin = new Thickness(0, 0, 8, 8),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            problemBtn.Click += (s, e) => ShowReportProblemDialog(prescription.Id);
            buttonWrap.Children.Add(problemBtn);

            Button checkStockBtn = new Button
            {
                Content = "🔍 Vérifier les stocks",
                Style = (Style)this.Resources["InfoButton"],
                Tag = prescription.Id,
                Height = 45,
                MinWidth = 160,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            checkStockBtn.Click += (s, e) =>
            {
                var detailedStatus = GetDetailedStockStatus(prescription);
                MessageBox.Show(detailedStatus, "Détail des stocks",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            };
            buttonWrap.Children.Add(checkStockBtn);

            mainPanel.Children.Add(buttonWrap);
            card.Child = mainPanel;
            return card;
        }

        // ✅ NEW: Builds the medications grid correctly (row 0 header + rows 1..N items)
        private Grid BuildMedicationsGrid(Prescription prescription, out int totalQuantity)
        {
            totalQuantity = 0;

            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Left
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });   // #
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });  // Médicament (nom seulement)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });  // Dosage
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });  // Fréquence
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });  // Durée
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });   // Qté
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });  // Statut

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddGridHeaderWithFixedWidth(grid, "#", 0, 50);
            AddGridHeaderWithFixedWidth(grid, "Médicament", 1, 180);
            AddGridHeaderWithFixedWidth(grid, "Dosage", 2, 100);
            AddGridHeaderWithFixedWidth(grid, "Fréquence", 3, 100);
            AddGridHeaderWithFixedWidth(grid, "Durée", 4, 100);
            AddGridHeaderWithFixedWidth(grid, "Qté", 5, 80);
            AddGridHeaderWithFixedWidth(grid, "Statut", 6, 120);

            int displayIndex = 1;

            foreach (var med in prescription.Medications)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                int row = grid.RowDefinitions.Count - 1;

                AddGridCellWithFixedWidth(grid, displayIndex.ToString(), 0, row, HorizontalAlignment.Center, 50);

                // ✅ CORRECTION : Utiliser MedicationName directement (sans combinaison)
                AddGridCellWithFixedWidth(grid, med.MedicationName, 1, row, HorizontalAlignment.Left, 180, allowWrap: true);

                // ✅ CORRECTION : Utiliser les champs séparés
                AddGridCellWithFixedWidth(grid, med.Dosage, 2, row, HorizontalAlignment.Center, 100);
                AddGridCellWithFixedWidth(grid, med.Frequency, 3, row, HorizontalAlignment.Center, 100);
                AddGridCellWithFixedWidth(grid, med.Duration, 4, row, HorizontalAlignment.Center, 100);
                AddGridCellWithFixedWidth(grid, med.Quantity.ToString(), 5, row, HorizontalAlignment.Center, 80);

                var stockInfo = CheckMedicationStockForItem(med);
                var statusCell = CreateStatusCell(stockInfo, row, 6);
                grid.Children.Add(statusCell);

                totalQuantity += med.Quantity;
                displayIndex++;
            }

            return grid;
        }
        // ✅ NOUVELLE MÉTHODE : Extraire seulement le nom du médicament
        private string ExtractMedicationNameOnly(string medicationFullString)
        {
            if (string.IsNullOrEmpty(medicationFullString))
                return "Médicament inconnu";

            // Supprimer les parties après " - " (fréquence, durée, etc.)
            int separatorIndex = medicationFullString.IndexOf(" - ");
            if (separatorIndex > 0)
            {
                return medicationFullString.Substring(0, separatorIndex);
            }

            return medicationFullString;
        }
        // ✅ FIX: header is always row 0 (prevents overlap / shifting)
        private void AddGridHeaderWithFixedWidth(Grid grid, string text, int column, double width)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 13, 92, 99)),
                Padding = new Thickness(8, 6, 8, 6),
                Width = width,
                BorderThickness = new Thickness(0, 0, 1, 1),
                BorderBrush = Brushes.LightGray
            };

            var header = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            border.Child = header;
            Grid.SetColumn(border, column);
            Grid.SetRow(border, 0); // ✅ important
            grid.Children.Add(border);
        }

        // ✅ FIX: consistent borders + wrapping only for long medication names
        private void AddGridCellWithFixedWidth(Grid grid, string text, int column, int row,
                                              HorizontalAlignment alignment, double width, bool allowWrap = false)
        {
            var border = new Border
            {
                Padding = new Thickness(8, 6, 8, 6),
                Width = width,
                BorderThickness = new Thickness(0, 0, 1, 1),
                BorderBrush = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center
            };

            var cell = new TextBlock
            {
                Text = text,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = alignment,
                TextWrapping = allowWrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                TextTrimming = allowWrap ? TextTrimming.None : TextTrimming.CharacterEllipsis
            };

            border.Child = cell;
            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);
            grid.Children.Add(border);
        }

        // Méthodes pour la section arrivage
        private void AddArrivalHeaderWithWidth(Grid grid, string text, int column, double width)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 13, 92, 99)),
                Padding = new Thickness(10, 8, 10, 8),
                BorderThickness = new Thickness(0, 0, 1, 0),
                BorderBrush = Brushes.LightGray
            };

            var header = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Width = width,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            border.Child = header;
            Grid.SetColumn(border, column);
            // row 0 by default (header row)
            grid.Children.Add(border);
        }

        private void AddArrivalCell(Grid grid, string text, int column, int row, double width)
        {
            var border = new Border
            {
                Padding = new Thickness(10, 8, 10, 8),
                BorderThickness = new Thickness(0, 0, 1, 0),
                BorderBrush = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center
            };

            var cell = new TextBlock
            {
                Text = text,
                FontSize = 13,
                Width = width,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = cell;
            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);
            grid.Children.Add(border);
        }

        private void AddArrivalCellWrapped(Grid grid, string text, int column, int row, double width)
        {
            var border = new Border
            {
                Padding = new Thickness(10, 8, 10, 8),
                BorderThickness = new Thickness(0, 0, 1, 0),
                BorderBrush = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center
            };

            var cell = new TextBlock
            {
                Text = text,
                FontSize = 13,
                Width = width,
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            border.Child = cell;
            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);
            grid.Children.Add(border);
        }

        private Border CreateStatusCell((bool available, string message) stockInfo, int row, int column)
        {
            var color = stockInfo.available
                ? (stockInfo.message.Contains("STOCK FAIBLE") ? "#FFC107" : "#28A745")
                : "#DC3545";

            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(2),
                ToolTip = stockInfo.message,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = new TextBlock
            {
                Text = stockInfo.available ? "✓" : "✗",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);

            return border;
        }

        private (bool available, string message) CheckMedicationStockForItem(Prescription.PrescriptionItem item)
        {
            var medications = DatabaseHelper.GetMedications();

            // ✅ CORRECTION : Utiliser MedicationName directement
            var medInStock = medications.FirstOrDefault(m =>
                m.Name.Contains(item.MedicationName, StringComparison.OrdinalIgnoreCase) ||
                m.Code.Contains(item.MedicationName, StringComparison.OrdinalIgnoreCase));

            if (medInStock == null)
                return (false, $"❌ {item.MedicationName}: NON TROUVÉ en stock");

            if (medInStock.Quantity <= 0)
                return (false, $"❌ {item.MedicationName}: ÉPUISÉ (Stock: 0)");

            if (medInStock.Quantity < item.Quantity)
                return (false, $"❌ {item.MedicationName}: STOCK INSUFFISANT (Demande: {item.Quantity}, Disponible: {medInStock.Quantity})");

            if (medInStock.IsLowStock)
                return (true, $"⚠️ {item.MedicationName}: STOCK FAIBLE (Disponible: {medInStock.Quantity}, Seuil: {medInStock.MinThreshold})");

            return (true, $"✅ {item.MedicationName}: DISPONIBLE (Stock: {medInStock.Quantity}, Seuil: {medInStock.MinThreshold})");
        }
        private string GetDetailedStockStatus(Prescription prescription)
        {
            var message = $"📋 Détail des stocks pour la prescription #{prescription.Id}\n\n";
            message += $"Patient: {prescription.PatientName}\n\n";

            var medications = DatabaseHelper.GetMedications();
            int availableCount = 0;
            int lowStockCount = 0;
            int unavailableCount = 0;

            foreach (var med in prescription.Medications)
            {
                var key = med.MedicationName.Split(' ')[0];

                var medInStock = medications.FirstOrDefault(m =>
                    m.Name.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    m.Code.Contains(key, StringComparison.OrdinalIgnoreCase));

                if (medInStock == null)
                {
                    message += $"❌ {med.MedicationName}: NON TROUVÉ en stock\n";
                    unavailableCount++;
                }
                else if (medInStock.Quantity <= 0)
                {
                    message += $"❌ {med.MedicationName}: ÉPUISÉ (Stock: 0)\n";
                    unavailableCount++;
                }
                else if (medInStock.Quantity < med.Quantity)
                {
                    message += $"❌ {med.MedicationName}: STOCK INSUFFISANT (Demande: {med.Quantity}, Disponible: {medInStock.Quantity})\n";
                    unavailableCount++;
                }
                else if (medInStock.IsLowStock)
                {
                    message += $"⚠️ {med.MedicationName}: STOCK FAIBLE (Disponible: {medInStock.Quantity}, Seuil: {medInStock.MinThreshold})\n";
                    lowStockCount++;
                    availableCount++;
                }
                else
                {
                    message += $"✅ {med.MedicationName}: DISPONIBLE (Stock: {medInStock.Quantity}, Seuil: {medInStock.MinThreshold})\n";
                    availableCount++;
                }
            }

            message += $"\n📊 RÉSUMÉ:\n";
            message += $"✅ Disponibles: {availableCount}\n";
            message += $"⚠️ Stock faible: {lowStockCount}\n";
            message += $"❌ Non disponibles: {unavailableCount}\n";
            message += $"📦 Total médications: {prescription.Medications.Count}";

            return message;
        }

        private void ShowReportProblemDialog(int prescriptionId)
        {
            var prescription = DatabaseHelper.GetPrescriptions().FirstOrDefault(p => p.Id == prescriptionId);
            if (prescription == null) return;

            var patient = DatabaseHelper.GetPatients().FirstOrDefault(p => p.Id == prescription.PatientId);
            var doctor = DatabaseHelper.GetUsers().FirstOrDefault(u => u.Id == prescription.DoctorId);

            Window problemWindow = new Window
            {
                Title = "🚨 Signaler un problème",
                Width = 500,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel mainPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Background = Brushes.White
            };

            mainPanel.Children.Add(new TextBlock
            {
                Text = $"Prescription #{prescriptionId}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Red,
                Margin = new Thickness(0, 0, 0, 10)
            });

            mainPanel.Children.Add(CreateInfoField("Patient:", patient?.FullName ?? "Inconnu"));
            mainPanel.Children.Add(CreateInfoField("Médecin:", doctor?.FullName ?? "Inconnu"));

            mainPanel.Children.Add(new TextBlock
            {
                Text = "Type de problème:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 15, 0, 5)
            });

            ComboBox problemTypeCombo = new ComboBox
            {
                ItemsSource = new[]
                {
                    "Médicament indisponible",
                    "Médicament expiré",
                    "Quantité insuffisante",
                    "Erreur dans la prescription",
                    "Problème de livraison",
                    "Autre problème"
                },
                SelectedIndex = 0,
                Height = 35,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(problemTypeCombo);

            mainPanel.Children.Add(new TextBlock
            {
                Text = "Détails du problème:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            TextBox detailsTextBox = new TextBox
            {
                Height = 120,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15)
            };
            mainPanel.Children.Add(detailsTextBox);

            CheckBox urgentCheckBox = new CheckBox
            {
                Content = "⚠️ Problème urgent - nécessite une action immédiate",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = Brushes.Red,
                FontWeight = FontWeights.SemiBold
            };
            mainPanel.Children.Add(urgentCheckBox);

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button sendButton = new Button
            {
                Content = "Envoyer le signalement",
                Width = 180,
                Height = 40,
                Background = Brushes.Red,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 10, 0)
            };

            sendButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(detailsTextBox.Text))
                {
                    MessageBox.Show("Veuillez décrire le problème.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string problemType = problemTypeCombo.SelectedItem?.ToString() ?? "Problème non spécifié";
                string urgency = urgentCheckBox.IsChecked == true ? "URGENT" : "Normal";

                DatabaseHelper.AddNotification(new Notification
                {
                    UserId = prescription.DoctorId,
                    Title = urgentCheckBox.IsChecked == true ? "🚨 PROBLÈME URGENT" : "⚠️ Problème signalé",
                    Message = $"De: Pharmacie\n" +
                             $"Type: {problemType}\n" +
                             $"Urgence: {urgency}\n" +
                             $"Prescription ID: {prescriptionId}\n" +
                             $"Patient: {patient?.FullName ?? "Inconnu"}\n\n" +
                             $"Détails:\n{detailsTextBox.Text}",
                    Type = "PharmacyProblem",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    RelatedPrescriptionId = prescriptionId,
                    RelatedPatientId = prescription.PatientId,
                    SenderId = 4,
                    SenderName = "Pharmacie",
                    IsUrgent = urgentCheckBox.IsChecked == true
                });

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = "Pharmacie",
                    Action = "Problème signalé",
                    Details = $"Prescription ID: {prescriptionId}, Type: {problemType}, Urgence: {urgency}"
                });

                MessageBox.Show("Problème signalé au médecin avec succès!",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                problemWindow.Close();
                ShowPrepareOrders();
            };

            Button cancelButton = new Button
            {
                Content = "Annuler",
                Width = 100,
                Height = 40,
                Background = Brushes.Gray,
                Foreground = Brushes.White
            };
            cancelButton.Click += (s, e) => problemWindow.Close();

            buttonPanel.Children.Add(sendButton);
            buttonPanel.Children.Add(cancelButton);
            mainPanel.Children.Add(buttonPanel);

            problemWindow.Content = mainPanel;
            problemWindow.ShowDialog();
        }

        private StackPanel CreateInfoField(string label, string value)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };

            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold,
                Width = 80,
                FontSize = 14
            });

            panel.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            });

            return panel;
        }

        private string CheckMedicationStock(Prescription prescription)
        {
            var medications = DatabaseHelper.GetMedications();
            var missingMeds = new System.Collections.Generic.List<string>();

            foreach (var med in prescription.Medications)
            {
                var key = med.MedicationName.Split(' ')[0];

                var stockMed = medications.FirstOrDefault(m =>
                    m.Name.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    m.Code.Contains(key, StringComparison.OrdinalIgnoreCase));

                if (stockMed == null)
                {
                    missingMeds.Add($"{med.MedicationName} (Non trouvé)");
                }
                else if (stockMed.Quantity < med.Quantity)
                {
                    missingMeds.Add($"{med.MedicationName} ({stockMed.Quantity}/{med.Quantity})");
                }
            }

            if (missingMeds.Count == 0)
                return "✅ Tous les médicaments disponibles";
            else
                return $"⚠️ Stock insuffisant: {string.Join(", ", missingMeds)}";
        }

        private void PrepareOrder(int prescriptionId)
        {
            var prescription = DatabaseHelper.GetPrescriptions().FirstOrDefault(p => p.Id == prescriptionId);
            if (prescription != null)
            {
                if (!CheckAndDeductMedications(prescription))
                {
                    MessageBox.Show("Impossible de préparer la prescription. Stocks insuffisants!",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                prescription.Status = "Prête";
                DatabaseHelper.UpdatePrescription(prescription);

                DatabaseHelper.AddNotification(new Notification
                {
                    UserId = 3,
                    Title = "Médicaments prêts",
                    Message = $"Les médicaments pour {prescription.PatientName} sont prêts à être récupérés\nPrescription ID: {prescriptionId}",
                    Type = "Success",
                    CreatedAt = DateTime.Now,
                    RelatedPrescriptionId = prescriptionId,
                    RelatedPatientId = prescription.PatientId
                });

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = "Pharmacie",
                    Action = "Médicaments préparés",
                    Details = $"Prescription ID: {prescriptionId}, Patient: {prescription.PatientName}\nMédicaments déduits des stocks"
                });

                MessageBox.Show("Commande préparée avec succès!\nStocks mis à jour et infirmier notifié.",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                ShowPrepareOrders();
            }
        }

        private bool CheckAndDeductMedications(Prescription prescription)
        {
            var medications = DatabaseHelper.GetMedications();
            bool allAvailable = true;

            // Vérifier disponibilité globale
            foreach (var med in prescription.Medications)
            {
                var key = med.MedicationName.Split(' ')[0];

                var stockMed = medications.FirstOrDefault(m =>
                    m.Name.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    m.Code.Contains(key, StringComparison.OrdinalIgnoreCase));

                if (stockMed == null || stockMed.Quantity < med.Quantity)
                {
                    allAvailable = false;
                    break;
                }
            }

            if (!allAvailable)
                return false;

            // Déduire quantités
            foreach (var med in prescription.Medications)
            {
                var key = med.MedicationName.Split(' ')[0];

                var stockMed = medications.FirstOrDefault(m =>
                    m.Name.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    m.Code.Contains(key, StringComparison.OrdinalIgnoreCase));

                if (stockMed != null)
                {
                    stockMed.DeductQuantity(med.Quantity);
                }
            }

            DatabaseHelper.SaveMedications(medications);
            return true;
        }

        private void RestockMedication(string code, int quantity)
        {
            var medications = DatabaseHelper.GetMedications();
            var medication = medications.FirstOrDefault(m => m.Code == code);

            if (medication != null)
            {
                medication.AddQuantity(quantity);
                DatabaseHelper.SaveMedications(medications);

                DatabaseHelper.AddActivity(new Activity
                {
                    DateTime = DateTime.Now,
                    User = "Pharmacie",
                    Action = "Réapprovisionnement",
                    Details = $"{medication.Name}: +{quantity} unités"
                });

                MessageBox.Show($"{medication.Name} réapprovisionné: +{quantity} unités\nNouvelle quantité: {medication.Quantity}",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                ShowStock();
            }
            else
            {
                MessageBox.Show($"Médicament avec le code {code} non trouvé.",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SimulateArrival()
        {
            var medications = DatabaseHelper.GetMedications();
            bool newMedicationsAdded = false;

            // D'abord, afficher une confirmation
            string confirmationMessage = "Confirmer l'arrivage des médicaments suivants:\n\n";
            confirmationMessage += "1. Paracétamol : +1000 unités\n";
            confirmationMessage += "2. Ibuprofène : +750 unités\n";
            confirmationMessage += "3. Amoxicilline : +500 unités\n";
            confirmationMessage += "4. Aspirine : NOUVEAU (600 unités)\n\n";
            confirmationMessage += "Le nouvel arrivage ajoutera l'Aspirine comme nouveau médicament.";

            if (MessageBox.Show(confirmationMessage, "Confirmer l'arrivage",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            // 1. Traiter les médicaments existants
            foreach (var med in medications)
            {
                if (med.Name.Contains("Paracétamol"))
                    med.AddQuantity(1000);
                else if (med.Name.Contains("Ibuprofène"))
                    med.AddQuantity(750);
                else if (med.Name.Contains("Amoxicilline"))
                    med.AddQuantity(500);
                // Note: on ne fait rien pour l'aspirine car elle n'existe pas encore
            }

            // 2. Vérifier et ajouter les NOUVEAUX médicaments

            // Vérifier si l'Aspirine existe déjà
            bool aspirinExists = medications.Any(m =>
                m.Name.Contains("Aspirine", StringComparison.OrdinalIgnoreCase) ||
                m.Code.Contains("ASPIRINE", StringComparison.OrdinalIgnoreCase));

            if (!aspirinExists)
            {
                // Ajouter l'Aspirine comme nouveau médicament
                medications.Add(new Medication
                {
                    Code = "ASPIR300",
                    Name = "Aspirine ",
                    Description = "Anti-inflammatoire et anti-agrégant plaquettaire",
                    Quantity = 600,
                    MinThreshold = 100,
                    ExpirationDate = DateTime.Now.AddYears(2),
                    Status = "Disponible"
                });
                newMedicationsAdded = true;
            }
            else
            {
                // Si l'aspirine existe déjà, augmenter sa quantité
                var aspirin = medications.FirstOrDefault(m =>
                    m.Name.Contains("Aspirine", StringComparison.OrdinalIgnoreCase));
                if (aspirin != null)
                {
                    aspirin.AddQuantity(600);
                }
            }

            DatabaseHelper.SaveMedications(medications);

            // Mettre à jour l'activité avec plus de détails
            DatabaseHelper.AddActivity(new Activity
            {
                DateTime = DateTime.Now,
                User = "Pharmacie",
                Action = "Arrivage simulé",
                Details = newMedicationsAdded ?
                    "Tous les stocks ont été réapprovisionnés + nouveau médicament ajouté (Aspirine)" :
                    "Tous les stocks ont été réapprovisionnés"
            });

            // Message plus informatif
            string message = "Arrivage simulé avec succès!\n\n";
            message += "Stocks mis à jour:\n";
            message += "• Paracétamol: +1000 unités\n";
            message += "• Ibuprofène: +750 unités\n";
            message += "• Amoxicilline: +500 unités\n";

            if (newMedicationsAdded)
            {
                message += "• Aspirine : NOUVEAU médicament ajouté (600 unités)\n\n";
                message += "✓ Le nouvel arrivage inclut un nouveau médicament.";
            }
            else
            {
                message += "• Aspirine: +600 unités\n\n";
                message += "✓ Tous les stocks ont été augmentés.";
            }

            MessageBox.Show(message, "Arrivage simulé", MessageBoxButton.OK, MessageBoxImage.Information);
            ShowStock();
        }
        private FrameworkElement CreateInfoBlock(string label, string value, double minWidth)
        {
            var panel = new StackPanel
            {
                MinWidth = minWidth,
                Margin = new Thickness(0, 0, 14, 10)
            };

            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 4)
            });

            panel.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            return panel;
        }

        private FrameworkElement CreateStatusPill(string label, string value, string hexColor, double minWidth)
        {
            var panel = new StackPanel
            {
                MinWidth = minWidth,
                Margin = new Thickness(0, 0, 14, 10)
            };

            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            var pill = new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            pill.Child = new TextBlock
            {
                Text = value,
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };

            panel.Children.Add(pill);
            return panel;
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Créée" => "#F5A623",
                "En préparation" => "#209CED",
                "Prête" => "#2ECC71",
                "Administrée" => "#0D5C63",
                _ => "#6C757D"
            };
        }

        private Grid CreateStockGrid(bool isHeader)
        {
            var grid = new Grid
            {
                Background = isHeader ? new SolidColorBrush(Color.FromRgb(240, 240, 240)) : Brushes.Transparent,
                Margin = new Thickness(0, isHeader ? 0 : 6, 0, isHeader ? 8 : 6),
                VerticalAlignment = VerticalAlignment.Stretch
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 90 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star), MinWidth = 160 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star), MinWidth = 200 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 90 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 90 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star), MinWidth = 110 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 100 });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            return grid;
        }

        private Grid CreateStockRow(Medication medication)
        {
            var row = CreateStockGrid(isHeader: false);

            AddRowCell(row, medication.Code, 0);
            AddRowCell(row, medication.Name, 1);
            AddRowCell(row, medication.Description, 2);
            AddRowCell(row, medication.Quantity.ToString(), 3);
            AddRowCell(row, medication.MinThreshold.ToString(), 4);
            AddRowCell(row, medication.ExpirationDate.ToString("dd/MM/yyyy"), 5);

            var statusPanel = new StackPanel();
            var statusColor = medication.Quantity <= 0 ? "#DC3545" :
                             medication.IsLowStock ? "#FFC107" :
                             medication.IsExpiringSoon ? "#F5A623" : "#28A745";
            var statusText = medication.Quantity <= 0 ? "Épuisé" :
                            medication.IsLowStock ? "Faible stock" :
                            medication.IsExpiringSoon ? "Expire bientôt" : "Disponible";

            var statusPill = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            statusPill.Child = new TextBlock
            {
                Text = statusText,
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            statusPanel.Children.Add(statusPill);
            Grid.SetColumn(statusPanel, 6);
            row.Children.Add(statusPanel);

            return row;
        }

        private void AddHeaderCell(Grid grid, string text, int column)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 13, 92, 99)),
                Padding = new Thickness(10, 10, 10, 10),
                BorderThickness = new Thickness(0, 0, 1, 0),
                BorderBrush = Brushes.LightGray
            };

            var cell = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                TextAlignment = TextAlignment.Center
            };

            border.Child = cell;
            Grid.SetColumn(border, column);
            grid.Children.Add(border);
        }

        private void AddRowCell(Grid grid, string text, int column)
        {
            var border = new Border
            {
                Padding = new Thickness(10, 8, 10, 8),
                BorderThickness = new Thickness(0, 0, 1, 0),
                BorderBrush = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center
            };

            var cell = new TextBlock
            {
                Text = text,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            border.Child = cell;
            Grid.SetColumn(border, column);
            grid.Children.Add(border);
        }

        private void RefreshStockTable(System.Collections.Generic.List<Medication> medications)
        {
            try
            {
                foreach (var child in ContentPanel.Children)
                {
                    if (child is Border border && border.Name == "StockTableCard")
                    {
                        if (border.Child is StackPanel panel)
                        {
                            panel.Children.Clear();

                            var headerGrid = CreateStockGrid(isHeader: true);
                            AddHeaderCell(headerGrid, "Code", 0);
                            AddHeaderCell(headerGrid, "Nom", 1);
                            AddHeaderCell(headerGrid, "Description", 2);
                            AddHeaderCell(headerGrid, "Quantité", 3);
                            AddHeaderCell(headerGrid, "Seuil Min", 4);
                            AddHeaderCell(headerGrid, "Expiration", 5);
                            AddHeaderCell(headerGrid, "Statut", 6);
                            panel.Children.Add(headerGrid);

                            if (medications.Count == 0)
                            {
                                panel.Children.Add(new TextBlock
                                {
                                    Text = "Aucun médicament trouvé",
                                    FontSize = 14,
                                    Foreground = Brushes.Gray,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(0, 20, 0, 0)
                                });
                            }
                            else
                            {
                                foreach (var med in medications)
                                {
                                    panel.Children.Add(CreateStockRow(med));
                                }

                                panel.Children.Add(new TextBlock
                                {
                                    Text = $"📊 {medications.Count} médicament(s) trouvé(s)",
                                    FontSize = 12,
                                    Foreground = Brushes.Gray,
                                    HorizontalAlignment = HorizontalAlignment.Right,
                                    Margin = new Thickness(0, 10, 0, 0)
                                });
                            }

                            return;
                        }
                    }
                }

                var newCard = CreateStockTableCard(medications);
                newCard.Name = "StockTableCard";

                int insertIndex = -1;
                for (int i = 0; i < ContentPanel.Children.Count; i++)
                {
                    if (ContentPanel.Children[i] is Border border && border.Name == "SearchCard")
                    {
                        insertIndex = i + 1;
                        break;
                    }
                }

                if (insertIndex >= 0 && insertIndex <= ContentPanel.Children.Count)
                {
                    ContentPanel.Children.Insert(insertIndex, newCard);
                }
                else
                {
                    ContentPanel.Children.Add(newCard);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour du tableau: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateStockTableCard(System.Collections.Generic.List<Medication> medications)
        {
            var container = new StackPanel();

            var headerGrid = CreateStockGrid(isHeader: true);
            AddHeaderCell(headerGrid, "Code", 0);
            AddHeaderCell(headerGrid, "Nom", 1);
            AddHeaderCell(headerGrid, "Description", 2);
            AddHeaderCell(headerGrid, "Quantité", 3);
            AddHeaderCell(headerGrid, "Seuil Min", 4);
            AddHeaderCell(headerGrid, "Expiration", 5);
            AddHeaderCell(headerGrid, "Statut", 6);
            container.Children.Add(headerGrid);

            if (medications.Count == 0)
            {
                container.Children.Add(new TextBlock
                {
                    Text = "Aucun médicament trouvé",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
            }
            else
            {
                foreach (var med in medications)
                {
                    container.Children.Add(CreateStockRow(med));
                }

                container.Children.Add(new TextBlock
                {
                    Text = $"📊 {medications.Count} médicament(s)",
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }

            return new Border
            {
                Style = (Style)this.Resources["CardStyle"],
                Child = container
            };
        }
        // Ajouter cette méthode
        private void ShowHistory()
        {
            ContentPanel.Children.Clear();
            SetActiveNav(BtnHistory); // Note: Il faut ajouter un bouton BtnHistory dans le XAML

            ContentPanel.Children.Add(CreateTitle("📋 Historique de la Pharmacie"));

            var activities = DatabaseHelper.GetActivities()
                .Where(a => a.User.Contains("Pharmacie") || a.Action.Contains("médicament"))
                .OrderByDescending(a => a.DateTime)
                .Take(30)
                .ToList();

            var prescriptions = DatabaseHelper.GetPrescriptions()
                .Where(p => p.Status == "Administrée" || p.Status == "Prête")
                .OrderByDescending(p => p.CreationDate)
                .Take(15)
                .ToList();

            // Section Activités
            if (activities.Count > 0)
            {
                Border activitiesCard = new Border
                {
                    Style = (Style)this.Resources["CardStyle"],
                    Margin = new Thickness(0, 0, 0, 20)
                };

                StackPanel activitiesPanel = new StackPanel();

                activitiesPanel.Children.Add(new TextBlock
                {
                    Text = "📝 Journal des activités récentes",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 15),
                    Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99))
                });

                foreach (var activity in activities)
                {
                    Border activityItem = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(0, 8, 0, 8)
                    };

                    StackPanel itemPanel = new StackPanel();

                    TextBlock actionText = new TextBlock
                    {
                        Text = $"• {activity.Action}",
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold
                    };
                    itemPanel.Children.Add(actionText);

                    TextBlock detailsText = new TextBlock
                    {
                        Text = activity.Details,
                        FontSize = 13,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(10, 2, 0, 0)
                    };
                    itemPanel.Children.Add(detailsText);

                    TextBlock dateText = new TextBlock
                    {
                        Text = activity.DateTime.ToString("dd/MM/yyyy HH:mm"),
                        FontSize = 11,
                        Foreground = Brushes.DarkGray,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    itemPanel.Children.Add(dateText);

                    activityItem.Child = itemPanel;
                    activitiesPanel.Children.Add(activityItem);
                }

                activitiesCard.Child = activitiesPanel;
                ContentPanel.Children.Add(activitiesCard);
            }

            // Section Prescriptions traitées
            if (prescriptions.Count > 0)
            {
                Border prescriptionsCard = new Border
                {
                    Style = (Style)this.Resources["CardStyle"],
                    Margin = new Thickness(0, 0, 0, 20)
                };

                StackPanel prescriptionsPanel = new StackPanel();

                prescriptionsPanel.Children.Add(new TextBlock
                {
                    Text = "💊 Prescriptions récemment traitées",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 15),
                    Foreground = new SolidColorBrush(Color.FromRgb(13, 92, 99))
                });

                foreach (var prescription in prescriptions)
                {
                    var patient = DatabaseHelper.GetPatients()
                        .FirstOrDefault(p => p.Id == prescription.PatientId);

                    Border prescriptionItem = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(0, 10, 0, 10)
                    };

                    StackPanel itemPanel = new StackPanel();

                    TextBlock prescInfo = new TextBlock
                    {
                        Text = $"Prescription #{prescription.Id} - {patient?.FullName ?? "Patient inconnu"}",
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold
                    };
                    itemPanel.Children.Add(prescInfo);

                    TextBlock details = new TextBlock
                    {
                        Text = $"Médecin: {prescription.DoctorName} • Médicaments: {prescription.Medications.Count}",
                        FontSize = 13,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(10, 2, 0, 0)
                    };
                    itemPanel.Children.Add(details);

                    TextBlock statusDate = new TextBlock
                    {
                        Text = $"Statut: {prescription.Status} • {prescription.CreationDate:dd/MM/yyyy HH:mm}",
                        FontSize = 12,
                        Foreground = prescription.Status == "Administrée" ? Brushes.Green : Brushes.Orange,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    itemPanel.Children.Add(statusDate);

                    prescriptionItem.Child = itemPanel;
                    prescriptionsPanel.Children.Add(prescriptionItem);
                }

                prescriptionsCard.Child = prescriptionsPanel;
                ContentPanel.Children.Add(prescriptionsCard);
            }

            if (activities.Count == 0 && prescriptions.Count == 0)
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
            }
        }
        private void BtnHistory_Click(object sender, RoutedEventArgs e) => ShowHistory();

        
    }

}

