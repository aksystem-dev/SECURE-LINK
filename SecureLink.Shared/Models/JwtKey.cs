using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureLink.Shared.Models
{
    public class JwtKey
    {
        public int Id { get; set; }
        public string KeyValue { get; set; }
        public KeyTypeEnum KeyType { get; set; }
        public bool IsActive { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public enum KeyTypeEnum
    {
        Primary = 1,
        Secondary = 2,
        Other = 3
    }
}
