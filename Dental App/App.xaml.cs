using Dental_App.Views;
using Microsoft.EntityFrameworkCore;
using Prism.DryIoc;
using System.Configuration;
using System.Data;
using System.Windows;

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
            var optionsBuilder = new DbContextOptionsBuilder<Dental_App.Models.DentalContext>();
            optionsBuilder.UseSqlite("Data Source=app.db");

            // This makes AppContext available for injection everywhere
            containerRegistry.RegisterInstance(new Dental_App.Models.DentalContext(optionsBuilder.Options));

            containerRegistry.RegisterForNavigation<MainView, MainViewModel>();
        }
    }

}
