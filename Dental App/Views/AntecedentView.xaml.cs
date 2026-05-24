using System.Windows.Controls;
using System.Windows;
using Dental_App.ViewModels;

namespace Dental_App.Views
{
    /// <summary>
    /// Interaction logic for AntecedentView.xaml
    /// </summary>
    public partial class AntecedentView : UserControl
    {
        public AntecedentView()
        {
            InitializeComponent();

            this.Loaded += AntecedentView_Loaded;
            this.IsVisibleChanged += AntecedentView_IsVisibleChanged;
        }

        private void AntecedentView_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AntecedentViewModel vm)
            {
                vm.Refresh();
            }
        }

        private void AntecedentView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible && this.DataContext is AntecedentViewModel vm)
            {
                vm.Refresh();
            }
        }
    }
}
