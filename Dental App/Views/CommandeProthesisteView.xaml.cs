using Dental_App.ViewModels;
using System.Windows.Controls;
using System.Windows;

namespace Dental_App.Views
{
    /// <summary>
    /// Interaction logic for CommandeProthesisteView.xaml
    /// </summary>
    public partial class CommandeProthesisteView : UserControl
    {
        public CommandeProthesisteView()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (DataContext is CommandeProthesisteViewModel vm)
                {
                    vm.Refresh();
                }
            };

            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible && this.DataContext is CommandeProthesisteViewModel vm)
                {
                    vm.Refresh();
                }
            };

            this.DataContextChanged += (s, e) =>
            {
                if (this.IsVisible && this.DataContext is CommandeProthesisteViewModel vm)
                {
                    vm.Refresh();
                }
            };
        }
    }
}
