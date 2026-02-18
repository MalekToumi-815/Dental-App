using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class CommandeProthesiste
{
    public int Id { get; set; }

    public int IdProthesiste { get; set; }

    public DateTime? Date { get; set; }

    public string? Achats { get; set; }

    public double? SommePayees { get; set; }

    public virtual Prothesiste IdProthesisteNavigation { get; set; } = null!;
}
