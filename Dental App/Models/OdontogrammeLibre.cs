using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class OdontogrammeLibre
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string? InkFilePath { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
