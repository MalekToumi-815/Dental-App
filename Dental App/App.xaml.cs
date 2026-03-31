using Dental_App.Views;
using Dental_App.Services;
using Microsoft.EntityFrameworkCore;
using System.IO;
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

            containerRegistry.RegisterForNavigation<MainView, ViewModels.MainViewModel>();
        }
    }

}
