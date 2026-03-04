using PersistenceToolkit.Domain;
using System.Collections.Generic;

namespace PersistenceToolkit.Tests.Entities
{
    public class Parent : Entity, IAggregateRoot
    {
        public string Title { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Child> Children { get; set; }
        public virtual OneToOneChild MainChild { get; set; } // Not ignored
        public virtual ICollection<Child> IgnoredChildren { get; set; } // Ignored
        public virtual OneToOneChild IgnoredChild { get; set; } // Ignored
    }
    public class OneToOneChild : Entity
    {
        public int ParentId { get; set; }
        public string Title { get; set; }
        public virtual ICollection<GrandChild> GrandChildren { get; set; }
        public virtual OneToOneGrandChild MainGrandChild { get; set; } // Not ignored
        public virtual ICollection<GrandChild> IgnoredGrandChildren { get; set; } // Ignored
        public virtual OneToOneGrandChild IgnoredGrandChild { get; set; } // Ignored
    }
    public class Child : Entity
    {
        public int ParentId { get; set; }
        public string Title { get; set; }
        public virtual ICollection<GrandChild> GrandChildren { get; set; }
        public virtual OneToOneGrandChild MainGrandChild { get; set; } // Not ignored
        public virtual ICollection<GrandChild> IgnoredGrandChildren { get; set; } // Ignored
        public virtual OneToOneGrandChild IgnoredGrandChild { get; set; } // Ignored
    }
    public class GrandChild : Entity
    {
        public int ChildId { get; set; }
        public string Title { get; set; }
    }
    public class OneToOneGrandChild : Entity
    {
        public int ChildId { get; set; }
        public string Title { get; set; }
    }
    public class User : Entity, IAggregateRoot
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
} 