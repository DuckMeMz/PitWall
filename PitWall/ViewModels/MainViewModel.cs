using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Input;
using PitWall.Commands;
using PitWall.Models;
using PitWall.Services;

namespace PitWall.ViewModels;
public class MainViewModel
{
    public MainViewModel()
    {
        IncreasePositionCommand = new RelayCommand(MoveUp);
        DecreasePositionCommand = new RelayCommand(MoveDown);
    }
    public Driver SelectedDriver { get; set; } = new Driver { FullName = "Carlos Sainz", LastName = "Sainz", ShortName = "SAI", TeamLogo = "/Assets/Images/Teams/Williams.svg", Position = 9 };

    public List<Driver> Drivers { get; } =
    [
        new() { FullName = "Lando Norris",      LastName = "Norris",      ShortName = "NOR", TeamLogo = "Assets/Images/Teams/McLaren.svg",       Position = 1 },
        new() { FullName = "Oscar Piastri",     LastName = "Piastri",     ShortName = "PIA", TeamLogo = "Assets/Images/Teams/McLaren.svg",       Position = 2 },
        new() { FullName = "Max Verstappen",    LastName = "Verstappen",  ShortName = "VER", TeamLogo = "Assets/Images/Teams/RedBull.svg",       Position = 3 },
        new() { FullName = "George Russell",    LastName = "Russell",     ShortName = "RUS", TeamLogo = "Assets/Images/Teams/Mercedes.svg",      Position = 4 },
        new() { FullName = "Kimi Antonelli",    LastName = "Antonelli",   ShortName = "ANT", TeamLogo = "Assets/Images/Teams/Mercedes.svg",      Position = 5 },
        new() { FullName = "Charles Leclerc",   LastName = "Leclerc",     ShortName = "LEC", TeamLogo = "Assets/Images/Teams/Ferrari.svg",       Position = 6 },
        new() { FullName = "Lewis Hamilton",    LastName = "Hamilton",    ShortName = "HAM", TeamLogo = "/Assets/Images/Teams/Ferrari.svg",       Position = 7 },
        new() { FullName = "Fernando Alonso",   LastName = "Alonso",      ShortName = "ALO", TeamLogo = "Assets/Images/Teams/AstonMartin.svg",   Position = 8 },
        new() { FullName = "Carlos Sainz",      LastName = "Sainz",       ShortName = "SAI", TeamLogo = "Assets/Images/Teams/Williams.svg",      Position = 9 },
        new() { FullName = "Alexander Albon",   LastName = "Albon",       ShortName = "ALB", TeamLogo = "Assets/Images/Teams/Williams.svg",      Position = 10 },
        new() { FullName = "Pierre Gasly",      LastName = "Gasly",       ShortName = "GAS", TeamLogo = "Assets/Images/Teams/Alpine.svg",        Position = 11 },
        new() { FullName = "Franco Colapinto",  LastName = "Colapinto",   ShortName = "COL", TeamLogo = "Assets/Images/Teams/Alpine.svg",        Position = 12 },
        new() { FullName = "Isack Hadjar",      LastName = "Hadjar",      ShortName = "HAD", TeamLogo = "Assets/Images/Teams/RacingBulls.svg",   Position = 13 },
        new() { FullName = "Liam Lawson",       LastName = "Lawson",      ShortName = "LAW", TeamLogo = "Assets/Images/Teams/RacingBulls.svg",   Position = 14 },
        new() { FullName = "Arvid Lindblad",    LastName = "Lindblad",    ShortName = "LIN", TeamLogo = "Assets/Images/Teams/RedBull.svg",       Position = 15 },
        new() { FullName = "Oliver Bearman",    LastName = "Bearman",     ShortName = "BEA", TeamLogo = "Assets/Images/Teams/Haas.svg",          Position = 16 },
        new() { FullName = "Esteban Ocon",      LastName = "Ocon",        ShortName = "OCO", TeamLogo = "Assets/Images/Teams/Haas.svg",          Position = 17 },
        new() { FullName = "Gabriel Bortoleto", LastName = "Bortoleto",   ShortName = "BOR", TeamLogo = "Assets/Images/Teams/Audi.svg",          Position = 18 },
        new() { FullName = "Nico Hülkenberg",   LastName = "Hülkenberg",  ShortName = "HUL", TeamLogo = "Assets/Images/Teams/Audi.svg",          Position = 19 },
        new() { FullName = "Sergio Perez",      LastName = "Perez",       ShortName = "PER", TeamLogo = "Assets/Images/Teams/Cadillac.svg",      Position = 20 },
        new() { FullName = "Valtteri Bottas",   LastName = "Bottas",      ShortName = "BOT", TeamLogo = "Assets/Images/Teams/Cadillac.svg",      Position = 21 },
        new() { FullName = "Lance Stroll",      LastName = "Stroll",      ShortName = "STR", TeamLogo = "Assets/Images/Teams/AstonMartin.svg",   Position = 22 }
    ];
    public ICommand IncreasePositionCommand { get; }
    public ICommand DecreasePositionCommand { get; }

    private readonly ApiService service = new();


    private void MoveUp()
    {
        if (SelectedDriver == null) return;
        if (SelectedDriver.Position == 1) return;
        SelectedDriver.Position--;
    }

    private void MoveDown()
    {
        if (SelectedDriver == null) return;
        if (SelectedDriver.Position == 22) return;
        SelectedDriver.Position++;
    }

    public async Task LoadData()
    {
        string json = await service.FetchAsync("");
    }
}