using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Security.RightsManagement;
using System.Text;
using System.Windows.Controls;

namespace PitWall.Models;

public class Driver : INotifyPropertyChanged
{
    public string BroadcastName { set; get; }
    public byte DriverNumber { set; get; }
    public string FirstName { set; get; }
    public string FullName { set; get; }
    public string HeadshotImage { set; get; }
    public string LastName { set; get; }
    public string NameAcronym { set; get; }
    public string TeamColor { set; get; }
    public string TeamName { set; get; }


    //Keys
    private ushort MeetingKey { set; get; }
    private ushort SessionKey { set; get; }

    private byte _position;
    public byte Position { get => _position; 
        set
        {
            if (_position == value) return;

            _position = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
