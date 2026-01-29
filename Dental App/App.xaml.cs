using System.Configuration;
using System.Data;
using System.Windows;
using Dental_App.Views;
using Prism.DryIoc;

namespace Dental_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {   
        protected override Window CreateShell()
        {
            return ContainerLocator.Container.Resolve<MainView>();
        }
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register any types needed for dependency injection here
        }
    }

}
