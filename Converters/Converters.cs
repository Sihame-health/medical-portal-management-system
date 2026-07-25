using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MedicalSystem.Converters
{
    public class StatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value?.ToString() ?? "";
            string mode = parameter?.ToString() ?? "";

            if (mode == "Activate")
                return status.Equals("Inactif", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;

            if (mode == "Deactivate")
                return status.Equals("Actif", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatusToBadgeBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (value?.ToString() ?? "").Trim().ToLowerInvariant();

            if (s == "actif" || s == "disponible" || s == "prête" || s == "administrée")
                return new SolidColorBrush(Color.FromRgb(40, 167, 69));

            if (s == "inactif" || s == "terminée")
                return new SolidColorBrush(Color.FromRgb(108, 117, 125));

            if (s.Contains("faible") || s.Contains("attente") || s.Contains("créée"))
                return new SolidColorBrush(Color.FromRgb(255, 193, 7));

            if (s.Contains("expir") || s.Contains("problème"))
                return new SolidColorBrush(Color.FromRgb(220, 53, 69));

            if (s.Contains("préparation"))
                return new SolidColorBrush(Color.FromRgb(23, 162, 184));

            return new SolidColorBrush(Color.FromRgb(23, 162, 184));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatusToBadgeForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (value?.ToString() ?? "").Trim().ToLowerInvariant();
            if (s.Contains("faible") || s.Contains("attente") || s.Contains("créée"))
                return new SolidColorBrush(Color.FromRgb(33, 37, 41));
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ServiceToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string serviceName = value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(serviceName))
                return Brushes.Gray;
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}