using Prism.Dialogs;

namespace Dental_App.Services
{
    public interface IAppNotificationService
    {
        void ShowSuccess(string message, string title = "Success");
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Warning");
    }

    public class AppNotificationService : IAppNotificationService
    {
        private readonly IDialogService _dialogService;

        public AppNotificationService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public void ShowSuccess(string message, string title = "Success")
        {
            ShowNotification(title, message, "success");
        }

        public void ShowError(string message, string title = "Error")
        {
            ShowNotification(title, message, "error");
        }

        public void ShowWarning(string message, string title = "Warning")
        {
            ShowNotification(title, message, "warning");
        }

        private void ShowNotification(string title, string message, string type)
        {
            var parameters = new DialogParameters
            {
                { "title", title },
                { "message", message },
                { "type", type }
            };

            _dialogService.ShowDialog("NotificationDialogView", parameters, (IDialogResult result) => { });
        }
    }
}