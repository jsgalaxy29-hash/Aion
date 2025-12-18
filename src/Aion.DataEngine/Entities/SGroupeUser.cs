using Aion.DataEngine.Entities;
using System;
using System.Buffers.Text;

namespace Aion.DataEngine.Entities
{
    public class SGroupUser : BaseEntity
    {
        public int UserId { get; set; }

        public int GroupId { get; set; }
    }
}
