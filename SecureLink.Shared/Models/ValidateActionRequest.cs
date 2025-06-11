using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureLink.Shared.Models
{
    public class ValidateActionRequest
    {
        public string EncryptedKey { get; set; } = string.Empty;
        public string ClientIPAddress { get; set; } = string.Empty;
    }
}
