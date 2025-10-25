using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.infrastructure;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using carestream.core.dtos.facility;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;

namespace carestream.web.Controllers
{
    [Authorize]
    public class FacilitySelectionController : Controller
    {
        private readonly IFacilitySelectionService _facilitySelectionService;
        private readonly ICurrentFacilityContext _facilityContext;
        private readonly ILogger<FacilitySelectionController> _logger;

        public FacilitySelectionController(
            IFacilitySelectionService facilitySelectionService,
            ICurrentFacilityContext facilityContext,
            ILogger<FacilitySelectionController> logger)
        {
            _facilitySelectionService = facilitySelectionService ?? throw new ArgumentNullException(nameof(facilitySelectionService));
            _facilityContext = facilityContext ?? throw new ArgumentNullException(nameof(facilityContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult GetFacilitySelectorPartial()
        {
            if (!_facilityContext.IsFacilityContextSet)
            {
                _logger.LogWarning("GetFacilitySelectorPartial: Facility context not set. Rendering with default state.");
            }
            return PartialView("_FacilitySelectorPartial", _facilityContext);
        }

        [HttpPost]
        public async Task<IActionResult> SelectFacility(int selectedFacilityId)
        {
            var internalUserIdClaim = User.FindFirst("carestream_user_id");
            if (internalUserIdClaim == null || !int.TryParse(internalUserIdClaim.Value, out int internalUserId))
            {
                _logger.LogError("SelectFacility: User not authenticated or internalUserId claim missing. Cannot select facility.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Authentication error: User ID missing. Please re-login.\"}");
                return Ok();
            }

            var userAccessibleFacilitiesJson = User.FindFirstValue("carestream_user_facilities");
            List<FacilityDto> userAccessibleFacilities = new List<FacilityDto>();
            if (!string.IsNullOrEmpty(userAccessibleFacilitiesJson))
            {
                try
                {
                    userAccessibleFacilities = JsonSerializer.Deserialize<List<FacilityDto>>(userAccessibleFacilitiesJson) ?? new List<FacilityDto>();
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "SelectFacility: Failed to deserialize user accessible facilities from claims for user {UserId}.", internalUserId);
                    Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"System error: Could not retrieve facility list.\"}");
                    return Ok();
                }
            }

            var selectedFacilityDto = userAccessibleFacilities.FirstOrDefault(f => f.FacilityId == selectedFacilityId);
            if (selectedFacilityDto == null)
            {
                _logger.LogWarning("SelectFacility: User {UserId} attempted to select unauthorized facility ID: {FacilityId}", internalUserId, selectedFacilityId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Access denied to selected facility or facility is inactive.\"}");
                return Ok();
            }

            // Update the persistent cookie for the newly selected facility
            HttpContext.Response.Cookies.Append(
                "_CareStreamFacilityId",
                selectedFacilityId.ToString(),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = HttpContext.Request.IsHttps
                }
            );
            _logger.LogInformation("SelectFacility: User {UserId} selected and cookie set for Facility ID: {FacilityId}", internalUserId, selectedFacilityId);

            // Re-sign authentication cookie with updated current_facility_id claim
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                identity.TryRemoveClaim(identity.FindFirst("carestream_current_facility_id")); // Remove old claim
                identity.AddClaim(new Claim("carestream_current_facility_id", selectedFacilityId.ToString())); // Add new claim
                await HttpContext.SignInAsync(HttpContext.User); // Re-sign the cookie
                _logger.LogInformation("SelectFacility: Re-signed authentication cookie with new current_facility_id claim for user {UserId}.", internalUserId);
            }

            // Trigger a full page reload to re-establish the facility context via middleware
            Response.Headers.Append("HX-Redirect", Url.Action("Index", "Home"));
            Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Facility changed to " + selectedFacilityDto.Name + ".\" }");

            return Ok();
        }
    }
}