using PersistenceToolkit.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistenceToolkit.Tests.Initializers
{
    public class SystemUser : ISystemUser
    {
        public int UserId { get; set; }
        public int TenantId { get; set; }
    }

}
