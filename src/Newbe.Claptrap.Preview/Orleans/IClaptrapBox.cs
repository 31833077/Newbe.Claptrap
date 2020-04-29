using Newbe.Claptrap.Preview.Abstractions.Core;

namespace Newbe.Claptrap.Preview.Orleans
{
    public interface IClaptrapBox
    {
        IClaptrap Claptrap { get; }
    }

    public interface IClaptrapBox<out TStateData> : IClaptrapBox
        where TStateData : IStateData
    {
        TStateData StateData { get; }
    }
}