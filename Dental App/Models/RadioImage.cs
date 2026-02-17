using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class RadioImage
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string? FileName { get; set; }

    public DateTime? DatePrise { get; set; }

    public string? Type { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
