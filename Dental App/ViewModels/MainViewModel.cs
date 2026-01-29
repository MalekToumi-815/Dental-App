using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace Dental_App.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private String title = "hello bibi"; 
        public String Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }
    }
}
