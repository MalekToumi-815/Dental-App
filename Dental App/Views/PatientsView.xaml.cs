using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dental_App.ViewModels;

namespace Dental_App.Views
{
    /// <summary>
    /// Interaction logic for PatientsView.xaml
    /// </summary>
    public partial class PatientsView : UserControl
    {
        public PatientsView()
        {
            InitializeComponent();

            // Refresh ViewModel when the view becomes visible (e.g., navigated back to)
            this.Loaded += PatientsView_Loaded;
            this.IsVisibleChanged += PatientsView_IsVisibleChanged;
        }

        private void PatientsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PatientsViewModel vm)
            {
                vm.Refresh();
            }
        }

        private void PatientsView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible && this.DataContext is PatientsViewModel vm)
            {
                vm.Refresh();
            }
        }

        private void ScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                double newOffset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
                scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(newOffset, scrollViewer.ScrollableHeight)));
                e.Handled = true;
            }
        }
    }
}
