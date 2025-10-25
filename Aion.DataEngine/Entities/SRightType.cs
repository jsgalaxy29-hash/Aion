using Aion.DataEngine.Entities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Aion.DataEngine.Entities
{
    public class SRightType : BaseEntity
    {
        public string Code { get; set; } = string.Empty;   // ex: "READ", "WRITE", "ADMIN"
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
