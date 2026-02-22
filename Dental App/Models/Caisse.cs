using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class Caisse
{
    public string DateDuJour { get; set; } = null!;

    public decimal? Montant { get; set; }
}
