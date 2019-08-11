using System.Collections.Generic;

namespace AweCsome.Entities
{
    public class AweCsomeUser
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public bool IsEmailAuthenticationGuestUser { get; set; }
        public bool IsHiddenInUI { get; set; }
        public bool IsShareByEmailGuestUser { get; set; }
        public bool IsSiteAdmin { get; set; }
        public string LoginName { get; set; }
        public string Title { get; set; }
        public List<AweCsomeGroup> Groups { get; set; }
    }
}
