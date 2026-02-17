using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class Utilisateur
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public string Prenom { get; set; } = null!;

    public string MotDePasseHash { get; set; } = null!;
}
