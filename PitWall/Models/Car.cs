using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace PitWall.Models;

public class Car : INotifyPropertyChanged
{
    //Telementry
    public byte Throttle { set; get; }
    public byte Brake { set; get; }
    public ushort Speed { set; get; }
    public ushort RPM { set; get; }
    public byte Gear { set; get; }
    public byte DRS { set; get; }

    //Metadata
    public DateTimeOffset Date { get; set; }
    public byte DriverNumber { get; set; }
    public ushort MeetingKey { get; set; }
    public ushort SessionKey { get; set; }

}
