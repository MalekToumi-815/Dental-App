using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class Consultation
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public DateTime? DateConsultation { get; set; }

    public string? Note { get; set; }

    public int? IdDent { get; set; }

    public decimal? MontantTotal { get; set; }

    public virtual Dent? IdDentNavigation { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<ActeMedical> IdActes { get; set; } = new List<ActeMedical>();
}
