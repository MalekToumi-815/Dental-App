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
    /// Interaction logic for OrdonnanceView.xaml
    /// </summary>
    public partial class OrdonnanceView : UserControl
    {
        public OrdonnanceView()
        {
            InitializeComponent();

            this.Loaded += OrdonnanceView_Loaded;
            this.IsVisibleChanged += OrdonnanceView_IsVisibleChanged;
        }

        private void OrdonnanceView_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is OrdonnanceViewModel vm)
            {
                vm.Refresh();
            }
        }

        private void OrdonnanceView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible && this.DataContext is OrdonnanceViewModel vm)
            {
                vm.Refresh();
            }
        }
    }
}
