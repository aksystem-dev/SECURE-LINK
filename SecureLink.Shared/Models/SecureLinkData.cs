using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureLink.Shared.Models
{
    public class SecureLinkData
    {
        public bool IsValid { get; set; }      // Je odkaz platný?
        public string Message { get; set; }    // Textová zpráva pro uživatele
        public bool ShowCommentBox { get; set; } // Zobrazit pole pro komentář?
        public string DatabaseName { get; set; } // Název databáze pro SQL příkaz
        public List<ActionOption> Actions { get; set; } = new(); // Možné akce
    }

    public class ActionOption
    {
        public int SecureLinkSettingsId { get; set; }
        public ActionType Action { get; set; } // Typ akce (Confirm, Reject, etc.)
        public string ButtonText { get; set; } // Text tlačítka pro UI
        public string SqlCommand { get; set; }
    }

}
