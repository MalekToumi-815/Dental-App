using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;
using System;
using System.Windows.Media;

namespace Dental_App.ViewModels
{
    public class NotificationDialogViewModel : BindableBase, IDialogAware
    {
        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        private string _icon;
        public string Icon
        {
            get { return _icon; }
            set { SetProperty(ref _icon, value); }
        }
        
        private SolidColorBrush _headerBackground;
        public SolidColorBrush HeaderBackground
        {
            get { return _headerBackground; }
            set { SetProperty(ref _headerBackground, value); }
        }
        
        private SolidColorBrush _iconForeground;
        public SolidColorBrush IconForeground
        {
            get { return _iconForeground; }
            set { SetProperty(ref _iconForeground, value); }
        }

        public DelegateCommand CloseCommand { get; }

        public DialogCloseListener RequestClose { get; }

        public NotificationDialogViewModel()
        {
            CloseCommand = new DelegateCommand(CloseDialog);
        }

        private void CloseDialog()
        {
            RequestClose.Invoke(ButtonResult.OK);
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Title = parameters.GetValue<string>("title");
            Message = parameters.GetValue<string>("message");
            var type = parameters.GetValue<string>("type");

            switch (type?.ToLower())
            {
                case "success":
                    Icon = "\uE73E"; 
                    HeaderBackground = new SolidColorBrush(Color.FromArgb(40, 46, 204, 113));
                    IconForeground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                    break;
                case "error":
                    Icon = "\uE711"; 
                    HeaderBackground = new SolidColorBrush(Color.FromArgb(40, 231, 76, 60));
                    IconForeground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                    break;
                case "warning":
                    Icon = "\uE7BA"; 
                    HeaderBackground = new SolidColorBrush(Color.FromArgb(40, 241, 196, 15));
                    IconForeground = new SolidColorBrush(Color.FromRgb(241, 196, 15));
                    break;
                default:
                    Icon = "\uE946"; 
                    HeaderBackground = new SolidColorBrush(Color.FromArgb(40, 52, 152, 219));
                    IconForeground = new SolidColorBrush(Color.FromRgb(52, 152, 219));
                    break;
            }
        }
    }
}