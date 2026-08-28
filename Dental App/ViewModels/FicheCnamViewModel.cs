using Dental_App.Models;
using Microsoft.EntityFrameworkCore;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dental_App.ViewModels
{
    public class FicheCnamViewModel : BindableBase
    {
        private readonly DentalContext _context;

        public ObservableCollection<string> Familles { get; set; } = new();
        public ObservableCollection<string> SousFamilles { get; set; } = new();
        public ObservableCollection<ActeCnam> Actes { get; set; } = new();

        private string _familleSelectionnee;
        public string FamilleSelectionnee
        {
            get => _familleSelectionnee;
            set
            {
                SetProperty(ref _familleSelectionnee, value);
                ChargerSousFamilles();
            }
        }

        private string _sousFamilleSelectionnee;
        public string SousFamilleSelectionnee
        {
            get => _sousFamilleSelectionnee;
            set
            {
                SetProperty(ref _sousFamilleSelectionnee, value);
                ChargerActes();
            }
        }

        public FicheCnamViewModel(DentalContext context)
        {
            _context = context;
            ChargerFamilles();
        }

        private void ChargerFamilles()
        {
            var familles = _context.ActesCnam
                .Select(a => a.Famille)
                .Distinct()
                .ToList();

            Familles.Clear();
            foreach (var f in familles)
                Familles.Add(f);
        }

        private void ChargerSousFamilles()
        {
            SousFamilles.Clear();
            Actes.Clear();

            if (string.IsNullOrEmpty(FamilleSelectionnee)) return;

            var sousFamilles = _context.ActesCnam
                .Where(a => a.Famille == FamilleSelectionnee)
                .Select(a => a.SousFamille)
                .Distinct()
                .ToList();

            foreach (var sf in sousFamilles)
                SousFamilles.Add(sf);
        }

        private void ChargerActes()
        {
            Actes.Clear();

            if (string.IsNullOrEmpty(SousFamilleSelectionnee)) return;

            var actes = _context.ActesCnam
                .Where(a => a.SousFamille == SousFamilleSelectionnee)
                .ToList();

            foreach (var a in actes)
                Actes.Add(a);
        }
        private ActeCnam _acteSelectionne;
        public ActeCnam ActeSelectionne
        {
            get => _acteSelectionne;
            set => SetProperty(ref _acteSelectionne, value);
        }
    }
}