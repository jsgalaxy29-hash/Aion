using System;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Implémentation simple de IClock.
    /// </summary>
    public class SystemClock : Aion.DataEngine.Interfaces.IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Now => DateTime.Now;
    }
}