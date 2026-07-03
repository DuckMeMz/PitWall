namespace PitWall.Models;

public readonly record struct ApiField<T>(string Name)
{
    public override string ToString() => Name;
}
