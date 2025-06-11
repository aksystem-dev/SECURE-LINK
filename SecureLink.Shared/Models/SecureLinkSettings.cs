using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureLink.Shared.Models
{
    public class SecureLinkSettings
    {
        public int Id { get; set; }
        public string EncryptedKey { get; set; }
        public string Message { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool ShowCommentBox { get; set; }
        public bool Processed { get; set; }
        public string DatabaseName { get; set; }
    }
}
