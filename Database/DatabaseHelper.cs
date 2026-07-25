using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MedicalSystem.Models;

namespace MedicalSystem.Database
{
    public class DatabaseHelper
    {
        private static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string PatientsFile = Path.Combine(DataFolder, "patients.json");
        private static readonly string UsersFile = Path.Combine(DataFolder, "users.json");
        private static readonly string PrescriptionsFile = Path.Combine(DataFolder, "prescriptions.json");
        private static readonly string MedicationsFile = Path.Combine(DataFolder, "medications.json");
        private static readonly string ServicesFile = Path.Combine(DataFolder, "services.json");
        private static readonly string NotificationsFile = Path.Combine(DataFolder, "notifications.json");
        private static readonly string ActivitiesFile = Path.Combine(DataFolder, "activities.json");

        static DatabaseHelper()
        {
            Directory.CreateDirectory(DataFolder);
            InitializeData();
        }

        private static void InitializeData()
        {
            if (!File.Exists(UsersFile))
            {
                var defaultUsers = new List<User>
                {
                    new User { Id = 1, FirstName = "Admin", LastName = "System", Username = "admin",
                               Password = "admin123", Email = "admin@hospital.com", Role = "Admin", Status = "Actif" },
                    new User { Id = 2, FirstName = "Doctor", LastName = "Demo", Username = "medecin",
                               Password = "123456", Email = "medecin@hospital.com", Role = "Doctor",
                               Status = "Actif", ServiceId = 1, ServiceName = "Cardiologie" },
                    new User { Id = 3, FirstName = "Nurse", LastName = "Demo", Username = "infirmier",
                               Password = "123456", Email = "infirmier@hospital.com", Role = "Nurse", Status = "Actif" },
                    new User { Id = 4, FirstName = "Pharmacist", LastName = "Demo", Username = "pharmacien",
                               Password = "123456", Email = "pharmacien@hospital.com", Role = "Pharmacy", Status = "Actif" },
                    new User { Id = 5, FirstName = "Reception", LastName = "Demo", Username = "accueil",
                               Password = "123456", Email = "accueil@hospital.com", Role = "Reception", Status = "Actif" }
                };
                SaveUsers(defaultUsers);
            }

            if (!File.Exists(ServicesFile))
            {
                var defaultServices = new List<Service>
                {
                    new Service { Id = 1, Name = "Cardiologie", Description = "Service des maladies cardiaques", DoctorCount = 1, Status = "Actif" },
                    new Service { Id = 2, Name = "Pédiatrie", Description = "Service pour enfants", DoctorCount = 0, Status = "Actif" },
                    new Service { Id = 3, Name = "Chirurgie", Description = "Service de chirurgie générale", DoctorCount = 0, Status = "Actif" },
                    new Service { Id = 4, Name = "Urgences", Description = "Service d'urgence 24h/24", DoctorCount = 0, Status = "Actif" }
                };
                SaveServices(defaultServices);
            }

            if (!File.Exists(MedicationsFile))
            {
                var defaultMedications = new List<Medication>
                {
                    new Medication { Code = "PARA500", Name = "Paracétamol", Description = "Antidouleur et antipyrétique",
                                     Quantity = 150, MinThreshold = 50, ExpirationDate = DateTime.Now.AddMonths(6), Status = "Disponible" },
                    new Medication { Code = "IBUP400", Name = "Ibuprofène", Description = "Anti-inflammatoire",
                                     Quantity = 80, MinThreshold = 90, ExpirationDate = DateTime.Now.AddDays(15), Status = "Faible stock" },
                    new Medication { Code = "AMOX1G", Name = "Amoxicilline", Description = "Antibiotique",
                                     Quantity = 45, MinThreshold = 20, ExpirationDate = DateTime.Now.AddMonths(3), Status = "Disponible" }
                };
                SaveMedications(defaultMedications);
            }
        }

        // Patient operations
        public static List<Patient> GetPatients()
        {
            return File.Exists(PatientsFile)
                ? JsonSerializer.Deserialize<List<Patient>>(File.ReadAllText(PatientsFile)) ?? new List<Patient>()
                : new List<Patient>();
        }

        public static void SavePatients(List<Patient> patients)
        {
            File.WriteAllText(PatientsFile, JsonSerializer.Serialize(patients, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static int AddPatient(Patient patient)
        {
            var patients = GetPatients();
            patient.Id = patients.Count > 0 ? patients.Max(p => p.Id) + 1 : 1;
            patients.Add(patient);
            SavePatients(patients);
            return patient.Id;
        }

        public static void UpdatePatient(Patient patient)
        {
            var patients = GetPatients();
            var index = patients.FindIndex(p => p.Id == patient.Id);
            if (index >= 0)
            {
                patients[index] = patient;
                SavePatients(patients);
            }
        }

        // User operations
        public static List<User> GetUsers()
        {
            return File.Exists(UsersFile)
                ? JsonSerializer.Deserialize<List<User>>(File.ReadAllText(UsersFile)) ?? new List<User>()
                : new List<User>();
        }

        public static void SaveUsers(List<User> users)
        {
            File.WriteAllText(UsersFile, JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static User? Authenticate(string username, string password, string role)
        {
            var users = GetUsers();
            return users.FirstOrDefault(u =>
                u.Username == username &&
                u.Password == password &&
                u.Role == role &&
                u.Status == "Actif");
        }

        // Prescription operations
        public static List<Prescription> GetPrescriptions()
        {
            return File.Exists(PrescriptionsFile)
                ? JsonSerializer.Deserialize<List<Prescription>>(File.ReadAllText(PrescriptionsFile)) ?? new List<Prescription>()
                : new List<Prescription>();
        }

        public static void SavePrescriptions(List<Prescription> prescriptions)
        {
            File.WriteAllText(PrescriptionsFile, JsonSerializer.Serialize(prescriptions, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static int AddPrescription(Prescription prescription)
        {
            var prescriptions = GetPrescriptions();
            prescription.Id = prescriptions.Count > 0 ? prescriptions.Max(p => p.Id) + 1 : 1;
            prescriptions.Add(prescription);
            SavePrescriptions(prescriptions);
            return prescription.Id;
        }

        public static void UpdatePrescription(Prescription prescription)
        {
            var prescriptions = GetPrescriptions();
            var index = prescriptions.FindIndex(p => p.Id == prescription.Id);
            if (index >= 0)
            {
                prescriptions[index] = prescription;
                SavePrescriptions(prescriptions);
            }
        }

        // Medication operations
        public static List<Medication> GetMedications()
        {
            return File.Exists(MedicationsFile)
                ? JsonSerializer.Deserialize<List<Medication>>(File.ReadAllText(MedicationsFile)) ?? new List<Medication>()
                : new List<Medication>();
        }

        public static void SaveMedications(List<Medication> medications)
        {
            File.WriteAllText(MedicationsFile, JsonSerializer.Serialize(medications, new JsonSerializerOptions { WriteIndented = true }));
        }

        // Service operations
        public static List<Service> GetServices()
        {
            return File.Exists(ServicesFile)
                ? JsonSerializer.Deserialize<List<Service>>(File.ReadAllText(ServicesFile)) ?? new List<Service>()
                : new List<Service>();
        }

        public static void SaveServices(List<Service> services)
        {
            File.WriteAllText(ServicesFile, JsonSerializer.Serialize(services, new JsonSerializerOptions { WriteIndented = true }));
        }

        // Notification operations
        public static List<Notification> GetNotifications()
        {
            return File.Exists(NotificationsFile)
                ? JsonSerializer.Deserialize<List<Notification>>(File.ReadAllText(NotificationsFile)) ?? new List<Notification>()
                : new List<Notification>();
        }

        public static void SaveNotifications(List<Notification> notifications)
        {
            File.WriteAllText(NotificationsFile,
                JsonSerializer.Serialize(notifications, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static void AddNotification(Notification notification)
        {
            var notifications = GetNotifications();
            notification.Id = notifications.Count > 0 ? notifications.Max(n => n.Id) + 1 : 1;
            notifications.Add(notification);
            SaveNotifications(notifications);
        }

        // Activity operations
        public static List<Activity> GetActivities()
        {
            return File.Exists(ActivitiesFile)
                ? JsonSerializer.Deserialize<List<Activity>>(File.ReadAllText(ActivitiesFile)) ?? new List<Activity>()
                : new List<Activity>();
        }

        public static void SaveActivities(List<Activity> activities)
        {
            File.WriteAllText(ActivitiesFile, JsonSerializer.Serialize(activities, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static void AddActivity(Activity activity)
        {
            var activities = GetActivities();
            activity.Id = activities.Count > 0 ? activities.Max(a => a.Id) + 1 : 1;
            activities.Insert(0, activity);
            SaveActivities(activities);
        }
        // Méthode pour trouver un médicament par nom ou code
        public static Medication? FindMedication(string searchTerm)
        {
            var medications = GetMedications();

            // Chercher d'abord par code exact
            var medication = medications.FirstOrDefault(m =>
                m.Code.Equals(searchTerm, StringComparison.OrdinalIgnoreCase));

            if (medication != null)
                return medication;

            // Chercher par nom (partiel)
            medication = medications.FirstOrDefault(m =>
                m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

            return medication;
        }
        // Dans DatabaseHelper.cs, ajouter :
        public static bool CheckMedicationStock(string medicationName, int requestedQuantity)
        {
            var medications = GetMedications();
            var medication = medications.FirstOrDefault(m =>
                m.Name.Equals(medicationName, StringComparison.OrdinalIgnoreCase) ||
                m.Code.Equals(medicationName, StringComparison.OrdinalIgnoreCase));

            return medication != null && medication.Quantity >= requestedQuantity;
        }

        public static (bool available, int currentStock, int threshold) GetStockInfo(string medicationName)
        {
            var medications = GetMedications();
            var medication = medications.FirstOrDefault(m =>
                m.Name.Contains(medicationName, StringComparison.OrdinalIgnoreCase) ||
                m.Code.Contains(medicationName, StringComparison.OrdinalIgnoreCase));

            if (medication == null)
                return (false, 0, 0);

            return (medication.Quantity > 0, medication.Quantity, medication.MinThreshold);

        }
        // Ajouter dans DatabaseHelper.cs
        public static void FixExistingPrescriptions()
        {
            var prescriptions = GetPrescriptions();
            bool updated = false;

            foreach (var prescription in prescriptions)
            {
                foreach (var med in prescription.Medications)
                {
                    // Si le médicament contient " - ", c'est l'ancien format
                    if (med.MedicationName.Contains(" - "))
                    {
                        string[] parts = med.MedicationName.Split(new[] { " - " }, StringSplitOptions.None);

                        if (parts.Length >= 3)
                        {
                            // Extraire le nom et le dosage du premier élément
                            string firstPart = parts[0];
                            string[] nameParts = firstPart.Split(' ');

                            if (nameParts.Length >= 2)
                            {
                                // Le dernier élément est le dosage
                                med.Dosage = nameParts[nameParts.Length - 1];

                                // Le reste est le nom
                                med.MedicationName = string.Join(" ", nameParts.Take(nameParts.Length - 1));

                                // Les autres parties restent dans leurs champs respectifs
                                if (parts.Length > 1) med.Frequency = parts[1];
                                if (parts.Length > 2) med.Duration = parts[2];

                                updated = true;
                            }
                        }
                    }
                }
            }

            if (updated)
            {
                SavePrescriptions(prescriptions);
                Console.WriteLine("Prescriptions corrigées avec succès!");
            }
        }
        // Ajouter cette méthode
        public static void UpdateUserLastLogin(string username, string role)
        {
            var users = GetUsers();
            var user = users.FirstOrDefault(u =>
                u.Username == username &&
                u.Role == role);

            if (user != null)
            {
                user.LastLogin = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                SaveUsers(users);
            }
        }
        public static string GetPatientsFilePath() => PatientsFile;
        public static string GetPrescriptionsFilePath() => PrescriptionsFile;
        public static string GetNotificationsFilePath() => NotificationsFile;
        public static string GetActivitiesFilePath() => ActivitiesFile;
        public static string GetUsersFilePath() => UsersFile;
        public static string GetServicesFilePath() => ServicesFile;
        public static string GetMedicationsFilePath() => MedicationsFile;
    }
}