using Dental_App.Services;
using Dental_App.ViewModels;
using Dental_App.Views;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Windows;
using Prism.Ioc;

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
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Dental_App"
            );

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var dbPath = Path.Combine(folder, "dental.db");

            containerRegistry.RegisterSingleton<Dental_App.Models.DentalContext>(() =>
            {
                var options = new DbContextOptionsBuilder<Dental_App.Models.DentalContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;

                return new Dental_App.Models.DentalContext(options);
            });

            // Register all services
            containerRegistry.RegisterSingleton<IActeMedicalService, ActeMedicalService>();
            containerRegistry.RegisterSingleton<IAntecedentService, AntecedentService>();
            containerRegistry.RegisterSingleton<IDentService, DentService>();
            containerRegistry.RegisterSingleton<IPatientService, PatientService>();
            containerRegistry.RegisterSingleton<IMedicamentService, MedicamentService>();
            containerRegistry.RegisterSingleton<IProthesisteService, ProthesisteService>();
            containerRegistry.RegisterSingleton<ICommandeProthesisteService, CommandeProthesisteService>();
            containerRegistry.RegisterSingleton<ICaisseService, CaisseService>();
            containerRegistry.RegisterSingleton<IConsultationService, ConsultationService>();
            containerRegistry.RegisterSingleton<IRendezVousService, RendezVousService>();
            containerRegistry.RegisterSingleton<IOrdonnanceService, OrdonnanceService>();
            containerRegistry.RegisterSingleton<IRadioImageService, RadioImageService>();
            containerRegistry.RegisterSingleton<IOdontogrammeLibreService, OdontogrammeLibreService>();

            // Register Views for Navigation
            containerRegistry.RegisterForNavigation<SidebarView, SidebarViewModel>("SidebarView");
            containerRegistry.RegisterForNavigation<ToolbarView, ToolbarViewModel>("ToolbarView");
            containerRegistry.RegisterForNavigation<DashboardView, DashboardViewModel>("DashboardView");
            containerRegistry.RegisterForNavigation<ActesMedicauxView, ActesMedicauxViewModel>("ActesMedicauxView");
            containerRegistry.RegisterForNavigation<RadioImagesView, RadioImagesViewModel>("RadioImagesView");
            containerRegistry.RegisterForNavigation<PatientsView, PatientsViewModel>("PatientsView");
            containerRegistry.RegisterForNavigation<MainView>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            try
            {
                var regionManager = Container.Resolve<IRegionManager>();

                // This "injects" the Sidebar into the left column immediately
                regionManager.RegisterViewWithRegion("SidebarRegion", typeof(SidebarView));

                // This "injects" the Toolbar into the top immediately
                regionManager.RegisterViewWithRegion("ToolbarRegion", typeof(ToolbarView));

                // This "injects" the Dashboard into the right column immediately
                regionManager.RegisterViewWithRegion("ContentRegion", typeof(DashboardView));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during initialization: {ex}");
                MessageBox.Show($"Initialization Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled Exception: {args.ExceptionObject}");
            };

            base.OnStartup(e);
        }
    }

}
