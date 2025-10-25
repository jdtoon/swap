using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using habits.Services.Calendar;
using habits.Services.FileSystem;
using System.Text.Json;

namespace habits.Controllers
{
    [Authorize(Roles = "admin")]
    public class CalendarEventTypesController : Controller
    {
        private readonly ILogger<CalendarEventTypesController> _logger;
        private readonly ICalendarEventTypeService _eventTypeService;
        private readonly IFileSystemService _fileSystemService;

        public CalendarEventTypesController(
            ILogger<CalendarEventTypesController> logger,
            ICalendarEventTypeService eventTypeService,
            IFileSystemService fileSystemService)
        {
            _logger = logger;
            _eventTypeService = eventTypeService;
            _fileSystemService = fileSystemService;
        }

        public IActionResult Index()
        {
            ViewBag.IsHTMXRequest = HttpContext.Request.Headers.ContainsKey("HX-Request");
            // Store SVG files in session
            HttpContext.Session.SetString("CalendarTypeFiles",
                JsonSerializer.Serialize(_fileSystemService.GetCalendarTypeFiles()));

            if (HttpContext.Request.Headers.ContainsKey("HX-Request"))
                return PartialView();
            return View();
        }

        public IActionResult GetEventTypes(string search = "", int page = 1, int pageSize = 10)
        {
            var types = _eventTypeService.GetEventTypes(search, page, pageSize);
            return PartialView("_EventTypesList", types);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEventType(string name, string color, string iconPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest("Name is required");

                var eventType = await _eventTypeService.AddEventTypeAsync(name, color, iconPath);
                return PartialView("_EventTypeItem", eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding event type");
                return StatusCode(500, "An error occurred while adding the event type.");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateEventType(int id, string name, string color, string iconPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest("Name is required");

                // Remove the tilde from iconPath
                iconPath = iconPath.Substring(1);

                var updatedType = await _eventTypeService.UpdateEventTypeAsync(id, name, color, iconPath);
                return PartialView("_EventTypeItem", updatedType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event type");
                return StatusCode(500, "An error occurred while updating the event type.");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteEventType(int id)
        {
            var success = await _eventTypeService.DeleteEventTypeAsync(id);
            return success ? Ok() : BadRequest();
        }
    }
}
