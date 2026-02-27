using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class Dent
{
    public int Id { get; set; }

    public int CodeFdi { get; set; }

    // new: optional FK to Patient
    public int? PatientId { get; set; }

    // navigation to Patient
    public virtual Patient? Patient { get; set; }

    public virtual ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
}
