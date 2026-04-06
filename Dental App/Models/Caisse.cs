using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class Caisse
{
    public DateOnly DateDuJour { get; set; }

    public decimal? Montant { get; set; }

    /// <summary>
    /// Type de caisse: true = Revenu, false = Dépense
    /// </summary>
    public bool IsRevenu { get; set; } = true;
}
