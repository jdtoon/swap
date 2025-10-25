using habits.Data.Models;
using habits.Dtos;
using habits.Dtos.Data;

namespace habits.Services.Users
{
    public interface IUserService
    {
        PagedResult<MemberDto> GetMembers(string search, string status, int page, int pageSize);
        MemberDto ToggleMemberStatus(string id);
        UserDisplayDto GetUserDisplay(string username);
        MemberDto GetMemberById(string id);
        bool UpdateMember(MemberDto memberDto);
    }
}
