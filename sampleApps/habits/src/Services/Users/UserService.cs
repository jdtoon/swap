using Microsoft.EntityFrameworkCore;
using habits.Data;
using habits.Data.Models;
using habits.Dtos;
using habits.Dtos.Data;

namespace habits.Services.Users
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public PagedResult<MemberDto> GetMembers(string search, string status, int page, int pageSize)
        {
            var members = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                members = members.Where(user => user.Name!.ToLower().Contains(search) ||
                                                user.Surname!.ToLower().Contains(search) ||
                                                (user.Name + " " + user.Surname!).ToLower().Contains(search) ||
                                                user.Email!.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                bool statusValue = status.ToLower() == "active" ? true : false;
                members = members.Where(user => user.IsActive == statusValue);
            }

            int totalRecords = members.Count();
            var paginatedData = members.Skip((page - 1) * pageSize)
                                       .Take(pageSize)
                                       .ToList();

            bool hasMore = (page * pageSize) <= totalRecords;

            return new PagedResult<MemberDto>
            {
                Data = paginatedData.Count > 0 ? paginatedData.Select(x => MemberDto.FromModel(x)).ToList() : [],
                HasMore = hasMore,
                TotalRecords = totalRecords,
                CurrentPage = page
            };
        }

        public MemberDto ToggleMemberStatus(string id)
        {
            var member = _context.Users.FirstOrDefault(u => u.Id == id);
            if (member == null)
            {
                return null!;
            }

            member.IsActive = !member.IsActive;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return MemberDto.FromModel(member);
            }

            return MemberDto.FromModel(member);
        }

        public UserDisplayDto GetUserDisplay(string username)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == username);
            if (user == null)
                return new UserDisplayDto();

            return UserDisplayDto.FromModel(user);
        }

        public MemberDto GetMemberById(string id)
        {
            var member = _context.Users.FirstOrDefault(u => u.Id == id);

            if (member == null)
                return null!;

            var userRole = _context.UserRoles.FirstOrDefault(u => u.UserId == id);

            if (userRole == null)
                return MemberDto.FromModel(member);

            var role = _context.Roles.FirstOrDefault(x => x.Id == userRole!.RoleId);
            return MemberDto.FromModel(member, role!);
        }

        public bool UpdateMember(MemberDto memberDto)
        {
            var member = _context.Users.FirstOrDefault(u => u.Id == memberDto.Id);
            if (member == null)
                return false;

            member.Name = memberDto.Name;
            member.Surname = memberDto.Surname;
            member.Color = memberDto.Color;
            member.PhoneNumber = memberDto.PhoneNumber;

            try
            {
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}