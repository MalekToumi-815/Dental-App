using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dental_App.Models
{
    public class ActeCnam
    {
        public int Id { get; set; }
        public string? Famille { get; set; }
        public string? SousFamille { get; set; }
        public string? Code { get; set; }
        public string? Cotation { get; set; }
        public string? Designation { get; set; }

        public override string ToString()
        {
            return Designation ?? string.Empty;
        }
    }
}