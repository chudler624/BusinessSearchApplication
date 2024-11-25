namespace BusinessSearch.Models.ViewModels
{
    public class TeamMemberViewModel
    {
        public TeamMember TeamMember { get; set; }
        public IEnumerable<CrmList> AssignedLists { get; set; }
    }

    public class TeamManagementViewModel
    {
        public IEnumerable<TeamMember> TeamMembers { get; set; }
        public TeamMember? NewTeamMember { get; set; }
    }
}
