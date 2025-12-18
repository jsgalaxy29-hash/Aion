using System;

namespace Aion.DataEngine.Interfaces
{
    public interface IClock { DateTime UtcNow { get; } }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
