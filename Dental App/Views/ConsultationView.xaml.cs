using System.Windows.Controls;
using System.Windows;
using Dental_App.ViewModels;

namespace Dental_App.Views
{
    /// <summary>
    /// Interaction logic for ConsultationView.xaml
    /// </summary>
    public partial class ConsultationView : UserControl
    {
        public ConsultationView()
        {
            InitializeComponent();

            this.Loaded += ConsultationView_Loaded;
            this.IsVisibleChanged += ConsultationView_IsVisibleChanged;
        }

        private void ConsultationView_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ConsultationViewModel vm)
            {
                vm.Refresh();
            }
        }

        private void ConsultationView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible && this.DataContext is ConsultationViewModel vm)
            {
                vm.Refresh();
            }
        }
    }
}
