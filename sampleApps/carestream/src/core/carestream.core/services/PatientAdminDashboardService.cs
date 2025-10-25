using carestream.core.dtos.dashboard;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;

namespace carestream.core.services
{
    public class PatientAdminDashboardService : IPatientAdminDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ILogger<PatientAdminDashboardService> _logger;

        public PatientAdminDashboardService(
            IDashboardRepository dashboardRepository,
            ILogger<PatientAdminDashboardService> logger)
        {
            _dashboardRepository = dashboardRepository ?? throw new ArgumentNullException(nameof(dashboardRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PatientAdminDashboardViewModel> GetDashboardViewModelAsync()
        {
            // Fetch data pieces concurrently
            var statsTask = _dashboardRepository.GetDashboardStatsAsync();
            var patientsTask = _dashboardRepository.GetRecentPatientsAsync(); // Using default limit
            var reportsTask = _dashboardRepository.GetRecentStaffReportsAsync(); // Using default limit

            // Await all tasks
            await Task.WhenAll(statsTask, patientsTask, reportsTask);

            // Construct the view model
            var viewModel = new PatientAdminDashboardViewModel
            {
                Stats = await statsTask,
                RecentPatients = (await patientsTask).ToList(),
                RecentStaffReports = (await reportsTask).ToList()
            };

            return viewModel;
        }

        /// <inheritdoc/>
        public async Task<PatientAdminDashboardViewModel> GetPatientAdminDashboardViewModelAsync() // RENAMED FROM GetDashboardViewModelAsync
        {
            _logger.LogInformation("Service: Getting Patient Admin Dashboard ViewModel.");

            DashboardStatsDto stats = await _dashboardRepository.GetDashboardStatsAsync();
            IEnumerable<RecentPatientDto> recentPatients = await _dashboardRepository.GetRecentPatientsAsync(5);
            IEnumerable<RecentStaffReportDto> recentStaffReports = await _dashboardRepository.GetRecentStaffReportsAsync(5);

            return new PatientAdminDashboardViewModel
            {
                Stats = stats,
                RecentPatients = recentPatients.ToList(),
                RecentStaffReports = recentStaffReports.ToList(),
                // PatientQueue related properties are no longer part of this Dashboard ViewModel directly.
                // They will be loaded separately by the PatientQueueController.
            };
        }
    }
}
