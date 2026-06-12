using Prism.Commands;
using Prism.Mvvm;
using System;

namespace Dental_App.ViewModels
{
    public class ConfirmationDialogViewModel : BindableBase
    {
        public string Title { get; set; }
        public string Message { get; set; }

        public DelegateCommand YesCommand { get; }
        public DelegateCommand NoCommand { get; }

        public Action<bool> CloseAction { get; set; }

        public ConfirmationDialogViewModel()
        {
            YesCommand = new DelegateCommand(() => CloseAction?.Invoke(true));
            NoCommand = new DelegateCommand(() => CloseAction?.Invoke(false));
        }
    }
}