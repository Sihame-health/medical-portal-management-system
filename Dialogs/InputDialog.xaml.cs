using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;

using System.Windows;

namespace MedicalSystem
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; } = "";

        public InputDialog(string title, string message)
        {
            InitializeComponent();
            Title = title;
            MessageText.Text = message;
            InputTextBox.Focus();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}