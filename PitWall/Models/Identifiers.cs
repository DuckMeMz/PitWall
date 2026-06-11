namespace PitWall.Models;

public readonly record struct MeetingKey(int Value);
public readonly record struct SessionKey(int Value);

public readonly record struct DriverNumber(byte Value);
public readonly record struct LapNumber(byte Value);

public readonly record struct DRSState(byte Value);
