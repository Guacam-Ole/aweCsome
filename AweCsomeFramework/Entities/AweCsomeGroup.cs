using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Entities
{
    public class AweCsomeGroup
    {
        public int Id { get; set; }
        public bool AllowMembersEditMembership { get; set; }
        public bool AllowRequestToJoinLeave { get; set; }
        public bool AutoAcceptRequestToJoinLeave { get; set; }
        public bool CanCurrentUserEditMembership { get; set; }
        public bool CanCurrentUserManageGroup { get; set; }
        public bool CanCurrentUserViewMembership { get; set; }
        public string Description { get; set; }
        public bool IsHiddenInUI { get; set; }
        public string LoginName { get; set; }
        public bool OnlyAllowMembersViewMembership { get; set; }
        public string OwnerTitle { get; set; }
        public string RequestToJoinLeaveEmailSetting { get; set; }
        public string Title { get; set; }
        public List<AweCsomeUser> Users { get; set; }



    }
}
