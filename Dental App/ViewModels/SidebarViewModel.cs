using Prism.Commands;
using Prism.Mvvm;

namespace Dental_App.ViewModels
{
    public class SidebarViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        public SidebarViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<string>(Navigate);
        }

        public DelegateCommand<string> NavigateCommand { get; }

        private void Navigate(string navigatePath)
        {
            if (string.IsNullOrEmpty(navigatePath)) return;

            // This targets the ContentRegion in your MainView
            _regionManager.RequestNavigate("ContentRegion", navigatePath);
        }
    }
}