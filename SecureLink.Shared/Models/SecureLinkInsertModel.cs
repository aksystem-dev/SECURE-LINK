using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureLink.Shared.Models
{
    public class SecureLinkInsertModel
    {
        public string SecureLinkText { get; set; }
        public bool ShowCommentBox { get; set; }
        public DateTime ExpirationDate { get; set; }

        // Confirm action
        public bool ConfirmActionEnabled { get; set; }
        [RequiredIf("ConfirmActionEnabled", true)]
        public string ConfirmActionButtonText { get; set; }
        [RequiredIf("ConfirmActionEnabled", true)]
        public string ConfirmActionSQL { get; set; }

        // Reject action
        public bool RejectActionEnabled { get; set; }
        [RequiredIf("RejectActionEnabled", true)]
        public string RejectActionButtonText { get; set; }
        [RequiredIf("RejectActionEnabled", true)]
        public string RejectActionSQL { get; set; }

        // Custom action
        public bool CustomActionEnabled { get; set; }
        [RequiredIf("CustomActionEnabled", true)]
        public string CustomActionButtonText { get; set; }
        [RequiredIf("CustomActionEnabled", true)]
        public string CustomActionSQL { get; set; }
    }

}
