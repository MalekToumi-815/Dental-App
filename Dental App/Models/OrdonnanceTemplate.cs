using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dental_App.Models;

[Table("OrdonnanceTemplate")]
public partial class OrdonnanceTemplate
{
    [Key]
    public int Id { get; set; }

    public double TemplateX { get; set; }

    public double TemplateY { get; set; }

    public string? TemplatePath { get; set; }
}
