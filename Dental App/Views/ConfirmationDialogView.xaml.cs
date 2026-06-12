using System;
using System.Windows;
using System.Windows.Controls;
using Dental_App.ViewModels;

namespace Dental_App.Views
{
    /// <summary>
    /// Interaction logic for ConfirmationDialogView.xaml
    /// </summary>
    public partial class ConfirmationDialogView : UserControl
    {
        public ConfirmationDialogView()
        {
            InitializeComponent();
            this.DataContextChanged += ConfirmationDialogView_DataContextChanged;
            this.Unloaded += ConfirmationDialogView_Unloaded;
        }

        private void ConfirmationDialogView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Clean up to avoid holding references
            if (this.DataContext is ConfirmationDialogViewModel vm)
            {
                vm.CloseAction = null;
            }
        }

        private void ConfirmationDialogView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ConfirmationDialogViewModel vm)
            {
                // Preserve any existing CloseAction set by the caller
                var previous = vm.CloseAction;

                // Replace with wrapper that calls previous action (to set caller state) then closes the host window
                vm.CloseAction = (result) =>
                {
                    try
                    {
                        previous?.Invoke(result);
                    }
                    catch { }

                    try
                    {
                        var host = Window.GetWindow(this);
                        if (host != null)
                        {
                            // If shown via ShowDialog, setting DialogResult is useful and will close the dialog
                            try { host.DialogResult = result; } catch { }

                            // Ensure the window closes
                            host.Close();
                        }
                    }
                    catch { }
                };
            }
        }
    }
}