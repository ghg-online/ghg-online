using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Entities
{
    public class Directory
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }

        public Guid Parent {  get; set; }

        public Directory(Guid id, string name, bool isDeleted, Guid parent)
        {
            Id = id;
            Name = name;
            IsDeleted = isDeleted;
            Parent = parent;
        }
    }
}
