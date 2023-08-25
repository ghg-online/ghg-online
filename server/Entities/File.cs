using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Entities
{
    public class File
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }

        public Guid Parent { get; set; }
        public TypeCode Type { get; set; }

        [System.Flags]
        public enum TypeCode
        {
            Readable = 1,
            Writable = 2,
            Executable = 4,
            Invokable = 8,
        }

        public File(Guid id, string name, bool isDeleted, Guid parent, TypeCode type)
        {
            Id = id;
            Name = name;
            IsDeleted = isDeleted;
            Parent = parent;
            Type = type;
        }
    }
}
