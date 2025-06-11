using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureLink.Shared.Models
{
    public enum UserTypeEnum
    {
        Admin = 1,
        Guest = 2,
        Reader = 3,
        Writer = 4
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }

        public string DatabaseName { get; set; }
        public UserTypeEnum UserType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
