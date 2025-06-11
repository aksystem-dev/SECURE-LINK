using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureLink.Shared.Models
{
    public enum ActionType
    {
        Confirm = 1,  // Potvrdit akci
        Reject = 2,   // Zamítnout akci
        Other = 3     // Další akce (pro budoucí rozšíření)
    }

    public class ConfirmActionRequest
    {
        public string Key { get; set; }         // Šifrovaný klíč z URL
        public ActionType Action { get; set; }  // Typ akce (Confirm / Reject)
        public string ClientIP { get; set; }    // IP adresa klienta
        public string? Comment { get; set; }    // Komentář uživatele   
    }
}
