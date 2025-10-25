using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using habits.Dtos;
using habits.Services.Calendar;
using System.Text.Json;

namespace habits.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly ILogger<CalendarController> _logger;
        private readonly ICalendarService _calendarService;
        private readonly ICalendarEventTypeService _eventTypeService;

        public CalendarController(
            ILogger<CalendarController> logger,
            ICalendarService calendarService,
            ICalendarEventTypeService eventTypeService)
        {
            _logger = logger;
            _calendarService = calendarService;
            _eventTypeService = eventTypeService;
        }

        public IActionResult Index(string? date = null, string? eventId = null)
        {
            ViewBag.IsHTMXRequest = HttpContext.Request.Headers.ContainsKey("HX-Request");
            var eventTypes = _eventTypeService.GetAllEventTypes();

            // Store event types in session
            HttpContext.Session.SetString("EventTypes", JsonSerializer.Serialize(eventTypes));

            if (!string.IsNullOrWhiteSpace(date))
                HttpContext.Session.SetString("SelectedSearchDate", date);

            if (!string.IsNullOrWhiteSpace(eventId))
                HttpContext.Session.SetString("SelectedSearchEventId", eventId);

            if (HttpContext.Request.Headers.ContainsKey("HX-Request"))
                return PartialView();

            return View();
        }

        public IActionResult GetCalendar(string? date = null)
        {
            DateTime? parsedDate = null;
            if (!string.IsNullOrEmpty(date))
            {
                DateTime.TryParse(date, out var tempDate);
                parsedDate = tempDate;
            }
            
            var monthData = _calendarService.GetMonthData(parsedDate ?? DateTime.Now);
            var eventTypes = _eventTypeService.GetAllEventTypes();

            var viewModel = new CalendarDto
            {
                MonthData = monthData,
                EventTypes = eventTypes
            };

            return PartialView("_InitialCalendar", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent([FromForm] CreateCalendarEventDto dto)
        {
            try
            {
                var newEvent = await _calendarService.AddEventAsync(dto);
                var updatedDay = _calendarService.GetDayData(dto.StartDate);

                Response.Headers.Append("HX-Trigger-After-Settle",
                    JsonSerializer.Serialize(new
                    {
                        calendarUpdated = new
                        {
                            date = dto.StartDate
                        },
                        eventUpdated = true
                    }));

                var dayEvents = _calendarService.GetEventsForDate(dto.StartDate);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding calendar event");
                return BadRequest();
            }
        }

        [HttpGet]
        public IActionResult GetDayViewer(DateTime date)
        {
            if (HttpContext?.Session.Keys.Contains("SelectedSearchEventId") == true)
            {
                HttpContext.Session.Remove("SelectedSearchEventId");
            }

            if (HttpContext?.Session.Keys.Contains("SelectedSearchDate") == true)
            {
                HttpContext.Session.Remove("SelectedSearchDate");
            }

            var events = _calendarService.GetEventsForDate(date);
            var viewModel = new DayViewerDto
            {
                Date = date,
                Events = events
            };
            return PartialView("_DayViewer", viewModel);
        }

        [HttpGet]
        public IActionResult GetEvent(int id)
        {
            var evt = _calendarService.GetEvent(id);
            if (evt == null)
                return NotFound();

            return PartialView("_EditEventForm", evt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEvent(int id, [FromForm] UpdateCalendarEventDto dto)
        {
            try
            {
                var updatedEvent = await _calendarService.UpdateEventAsync(id, dto);
                var dayEvents = _calendarService.GetEventsForDate(updatedEvent.StartDate);

                Response.Headers.Append("HX-Trigger-After-Settle",
                    JsonSerializer.Serialize(new
                    {
                        calendarUpdated = new
                        {
                            date = dto.StartDate
                        },
                        eventUpdated = true,
                    }));

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating calendar event");
                return BadRequest();
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var calendarEvent = _calendarService.GetEvent(id);
                await _calendarService.DeleteEventAsync(id);

                Response.Headers.Append("HX-Trigger-After-Settle",
                   JsonSerializer.Serialize(new
                   {
                       calendarUpdated = new
                       {
                           date = calendarEvent!.StartDate
                       },
                       eventUpdated = true
                   }));

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calendar event");
                return BadRequest();
            }
        }
    }
}