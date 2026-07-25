using MedicalSystem.Database;
using System.Windows;

namespace MedicalSystem
{
    public partial class App : Application
    {
        // Dans App.xaml.cs ou MainWindow.xaml.cs
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Corriger les prescriptions existantes au démarrage
            DatabaseHelper.FixExistingPrescriptions();

            // Continuer le démarrage normal
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}