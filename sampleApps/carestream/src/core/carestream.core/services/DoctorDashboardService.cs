using carestream.core.dtos.doctor;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace carestream.core.services
{
    /// <summary>
    /// Service implementation for providing data to the Doctor's dashboard.
    /// This is a placeholder implementation.
    /// </summary>
    public class DoctorDashboardService : IDoctorDashboardService
    {
        private readonly IVisitRepository _visitRepository;
        private readonly ILogger<DoctorDashboardService> _logger;
        private readonly IUserRepository _userRepository; 
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoctorDashboardService"/> class.
        /// </summary>
        /// <param name="visitRepository">The visit repository.</param>
        /// <param name="logger">The logger for this service.</param>
        public DoctorDashboardService(
            IVisitRepository visitRepository,
            IUserRepository userRepository,        
            IHttpContextAccessor httpContextAccessor,
            ILogger<DoctorDashboardService> logger)
        {
            _visitRepository = visitRepository ?? throw new ArgumentNullException(nameof(visitRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<DoctorDashboardViewModel> GetDashboardViewModelAsync()
        {
            _logger.LogInformation("DoctorDashboardService.GetDashboardViewModelAsync called.");

            var currentUser = _httpContextAccessor.HttpContext?.User;
            if (currentUser == null)
            {
                _logger.LogWarning("Current user context is null. Cannot determine doctor ID.");
                return new DoctorDashboardViewModel
                {
                    InProgressConsultations = new List<DoctorQueueItemDto>(),
                    PatientQueue = new List<DoctorQueueItemDto>(),
                    Stats = new DoctorDashboardStatsDto()
                }; // Return empty view model
            }

            if (!currentUser!.Identity!.IsAuthenticated)
            {
                _logger.LogWarning("Current user is not authenticated. Cannot determine doctor ID.");
                return new DoctorDashboardViewModel
                {
                    InProgressConsultations = new List<DoctorQueueItemDto>(),
                    PatientQueue = new List<DoctorQueueItemDto>(),
                    Stats = new DoctorDashboardStatsDto()
                }; // Return empty view model
            }

            var logtoSub = currentUser.FindFirst("sub");
            if (logtoSub == null)
            {
                _logger.LogWarning("Logto 'sub' claim not found for current user. Cannot determine doctor ID.");
                return new DoctorDashboardViewModel
                {
                    InProgressConsultations = new List<DoctorQueueItemDto>(),
                    PatientQueue = new List<DoctorQueueItemDto>(),
                    Stats = new DoctorDashboardStatsDto()
                }; // Return empty view model
            }

            if (string.IsNullOrEmpty(logtoSub.Value))
            {
                _logger.LogWarning("Logto 'sub' claim not found for current user. Cannot determine doctor ID.");
                return new DoctorDashboardViewModel
                {
                    InProgressConsultations = new List<DoctorQueueItemDto>(),
                    PatientQueue = new List<DoctorQueueItemDto>(),
                    Stats = new DoctorDashboardStatsDto()
                }; // Return empty view model
            }

            var doctorInternalId = await _userRepository.GetUserIdByLogtoSubAsync(logtoSub.Value);
            if (!doctorInternalId.HasValue)
            {
                _logger.LogWarning("No internal user ID found for Logto sub '{LogtoSub}'. Cannot fetch doctor-specific data.", logtoSub.Value);
                return new DoctorDashboardViewModel
                {
                    InProgressConsultations = new List<DoctorQueueItemDto>(),
                    PatientQueue = new List<DoctorQueueItemDto>(),
                    Stats = new DoctorDashboardStatsDto()
                }; // Return empty view model
            }

            _logger.LogInformation("Fetching dashboard data for Doctor UserID: {DoctorId}", doctorInternalId.Value);

            // Fetch data pieces concurrently
            var statsTask = _visitRepository.GetDoctorDashboardStatsAsync();
            var readyQueueTask = _visitRepository.GetDoctorPatientQueueAsync();
            var inProgressTask = _visitRepository.GetInProgressConsultationsForDoctorAsync(doctorInternalId.Value);

            await Task.WhenAll(statsTask, readyQueueTask, inProgressTask);

            var viewModel = new DoctorDashboardViewModel
            {
                Stats = await statsTask ?? new DoctorDashboardStatsDto(), // Ensure Stats is not null
                PatientQueue = (await readyQueueTask)?.ToList() ?? new List<DoctorQueueItemDto>(),
                InProgressConsultations = (await inProgressTask)?.ToList() ?? new List<DoctorQueueItemDto>()
            };

            return viewModel;
        }
    }
}