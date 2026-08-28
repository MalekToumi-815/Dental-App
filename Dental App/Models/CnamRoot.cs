using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dental_App.Models
{
    public class CnamRoot
    {
        [JsonPropertyName("familles")]
        public List<CnamFamille> Familles { get; set; }
    }

    public class CnamFamille
    {
        [JsonPropertyName("famille")]
        public string Famille { get; set; }

        [JsonPropertyName("sousFamilles")]
        public List<CnamSousFamille> SousFamilles { get; set; }
    }

    public class CnamSousFamille
    {
        [JsonPropertyName("sousFamille")]
        public string SousFamille { get; set; }

        [JsonPropertyName("actes")]
        public List<CnamActe> Actes { get; set; }
    }

    public class CnamActe
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("cotation")]
        public string Cotation { get; set; }

        [JsonPropertyName("designation")]
        public string Designation { get; set; }
    }
}