using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using habits.Dtos;
using habits.Services.Users;
using Microsoft.AspNetCore.Identity;
using habits.Data.Models;

namespace habits.Controllers
{
    [Authorize]
    public class MembersController : Controller
    {
        private readonly IUserService _userService;
        private readonly UserManager<AppUser> _userManager;

        public MembersController(IUserService userService, UserManager<AppUser> userManager)
        {
            _userService = userService;
            _userManager = userManager;
        }

        public IActionResult Index(string? search = null)
        {
            ViewBag.IsHTMXRequest = HttpContext.Request.Headers.ContainsKey("HX-Request");

            if (!string.IsNullOrWhiteSpace(search))
            {
                HttpContext.Session.SetString("SelectedSearchMembers", search);
            }

            if (HttpContext.Request.Headers.ContainsKey("HX-Request"))
                return PartialView();

            return View();
        }

        public IActionResult GetMembers(string search = "", string status = "", int page = 1, int pageSize = 5)
        {
            if (HttpContext?.Session.Keys.Contains("SelectedSearchMembers") == true)
            {
                HttpContext.Session.Remove("SelectedSearchMembers");
            }

            var paginatedData = _userService.GetMembers(search, status, page, pageSize);
            return PartialView("_MembersTable", paginatedData);
        }

        [HttpPut]
        public IActionResult ToggleMemberStatus(string id)
        {
            var member = _userService.ToggleMemberStatus(id);
            return PartialView("_ToggleStatusButton", member);
        }

        public IActionResult GetMemberModal(string id)
        {
            var member = _userService.GetMemberById(id);
            if (member == null)
                return NotFound();

            return PartialView("_MemberModal", member);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMember(MemberDto memberDto)
        {
            var user = await _userManager.FindByIdAsync(memberDto.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Update basic info through service
            var isUpdated = _userService.UpdateMember(memberDto);
            if (!isUpdated)
            {
                return BadRequest();
            }

            // Handle role update
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrWhiteSpace(memberDto.Role))
                await _userManager.AddToRoleAsync(user, memberDto.Role);

            var updatedMember = _userService.GetMemberById(memberDto.Id);
            return PartialView("_MemberListItem", updatedMember);
        }
    }
}