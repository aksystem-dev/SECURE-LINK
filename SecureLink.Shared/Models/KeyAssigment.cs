using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureLink.Shared.Models
{
    public class KeyAssigment
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string IpAddress { get; set; }
        public string JwtKey { get; set; }
        public string Nonce { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
