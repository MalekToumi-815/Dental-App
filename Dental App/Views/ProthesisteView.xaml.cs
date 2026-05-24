using Dental_App.ViewModels;
using System.Windows.Controls;
using System.Windows;

namespace Dental_App.Views
{
    /// <summary>
    /// Interaction logic for ProthesisteView.xaml
    /// </summary>
    public partial class ProthesisteView : UserControl
    {
        public ProthesisteView()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (DataContext is ProthesisteViewModel vm)
                {
                    vm.Refresh();
                }
            };

            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible && this.DataContext is ProthesisteViewModel vm)
                {
                    vm.Refresh();
                }
            };

            this.DataContextChanged += (s, e) =>
            {
                if (this.IsVisible && this.DataContext is ProthesisteViewModel vm)
                {
                    vm.Refresh();
                }
            };
        }
    }
}
