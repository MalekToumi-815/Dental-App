using Dental_App.Services;
using Dental_App.ViewModels;
using Dental_App.Views;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Windows;
using Prism.Ioc;
using System.Threading.Tasks;

using System;

namespace Dental_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        // Defer shell creation; we'll create and show it after splash finishes
        protected override Window CreateShell()
        {
            return null;
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
            containerRegistry.RegisterSingleton(typeof(ILiveSearchService<>), typeof(LiveSearchService<>));
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
            containerRegistry.RegisterSingleton<IOrdonnanceServiceTemplate, OrdonnanceServiceTemplate>();
            containerRegistry.RegisterSingleton<IRadioImageService, RadioImageService>();
            containerRegistry.RegisterSingleton<IOdontogrammeLibreService, OdontogrammeLibreService>();
            containerRegistry.RegisterSingleton<IAppNotificationService, AppNotificationService>();

            // Register ThemeService
            containerRegistry.RegisterSingleton<IThemeService, ThemeService>();

            // Register Views for Navigation
            containerRegistry.RegisterForNavigation<SidebarView, SidebarViewModel>("SidebarView");
            containerRegistry.RegisterForNavigation<ToolbarView, ToolbarViewModel>("ToolbarView");
            containerRegistry.RegisterForNavigation<DashboardView, DashboardViewModel>("DashboardView");
            containerRegistry.RegisterForNavigation<ActesMedicauxView, ActesMedicauxViewModel>("ActesMedicauxView");
            containerRegistry.RegisterForNavigation<RadioImagesView, RadioImagesViewModel>("RadioImagesView");
            containerRegistry.RegisterForNavigation<PatientsView, PatientsViewModel>("PatientsView");
            containerRegistry.RegisterForNavigation<ConsultationView, ConsultationViewModel>("ConsultationView");
            containerRegistry.RegisterForNavigation<OdontogrammeView, OdontogrammeViewModel>("OdontogrammeView");
            containerRegistry.RegisterForNavigation<ProthesisteView, ProthesisteViewModel>();
            containerRegistry.RegisterForNavigation<CommandeProthesisteView, CommandeProthesisteViewModel>();
            containerRegistry.RegisterForNavigation<AntecedentView, AntecedentViewModel>();
            containerRegistry.RegisterForNavigation<CaisseView, CaisseViewModel>();
            containerRegistry.RegisterForNavigation<RendezVousView, RendezVousViewModel>();
            containerRegistry.RegisterForNavigation<OrdonnanceView, OrdonnanceViewModel>("OrdonnanceView");
            containerRegistry.RegisterForNavigation<OrdonnanceTemplateDialogView, OrdonnanceTemplateDialogViewModel>("OrdonnanceTemplateDialogView");
            containerRegistry.RegisterForNavigation<MainView>();

            // Register Evolution view (Caisse evolution charts)
            containerRegistry.RegisterForNavigation<EvolutionView, EvolutionViewModel>("EvolutionView");

            containerRegistry.RegisterDialog<NotificationDialogView, NotificationDialogViewModel>();
        }

        protected override void OnInitialized()
        {
            // Show splash screen immediately on UI thread
            var splash = new SplashScreenView();
            splash.Show();

            // Run initialization on background thread to keep UI responsive
            Task.Run(async () =>
            {
                try
                {
                    await PerformStartupInitializationAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Startup initialization error: {ex}");
                }

                // After initialization, create and show main window on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var main = Container.Resolve<MainView>();

                        // Ensure the Prism RegionManager is attached to the shell so regions/navigation work
                        try
                        {
                            var regionManager = Container.Resolve<IRegionManager>();
                            RegionManager.SetRegionManager(main, regionManager);
                        }
                        catch (Exception rex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to attach RegionManager: {rex}");
                        }

                        Application.Current.MainWindow = main;
                        main.Show();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to create main window: {ex}");
                    }
                    finally
                    {
                        try { splash.Close(); } catch { }
                    }
                });
            });
        }

        private async Task PerformStartupInitializationAsync()
        {
            try
            {
                // Ensure DB exists and run any migrations/seeds as needed
                var ctx = Container.Resolve<Dental_App.Models.DentalContext>();
                await ctx.Database.EnsureCreatedAsync();

                // Short simulated delay (remove in production)
                await Task.Delay(800);

                // Apply theme and register initial regions on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var themeService = Container.Resolve<IThemeService>();
                        themeService.RestoreDarkTheme();

                        var regionManager = Container.Resolve<IRegionManager>();
                        regionManager.RegisterViewWithRegion("SidebarRegion", typeof(SidebarView));
                        regionManager.RegisterViewWithRegion("ToolbarRegion", typeof(ToolbarView));
                        regionManager.RegisterViewWithRegion("ContentRegion", typeof(DashboardView));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"During PerformStartupInitializationUI: {ex}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PerformStartupInitializationAsync error: {ex}");
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