using carestream.core.dtos.vitals;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging; // Added for ILogger

namespace carestream.core.services
{
    public class NurseDashboardService : INurseDashboardService
    {
        private readonly IVisitRepository _visitRepository;
        private readonly ILogger<NurseDashboardService> _logger; // Added logger

        public NurseDashboardService(IVisitRepository visitRepository, ILogger<NurseDashboardService> logger) // Added logger to constructor
        {
            _visitRepository = visitRepository ?? throw new ArgumentNullException(nameof(visitRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Assign logger
        }

        public async Task<NurseDashboardViewModel> GetDashboardViewModelAsync()
        {
            _logger.LogInformation("NurseDashboardService.GetDashboardViewModelAsync called."); // Replaced Console.WriteLine

            // Fetch data pieces concurrently
            var statsTask = _visitRepository.GetVitalsDashboardStatsAsync();
            var queueTask = _visitRepository.GetVitalsQueueAsync();

            await Task.WhenAll(statsTask, queueTask);

            var viewModel = new NurseDashboardViewModel
            {
                Stats = await statsTask ?? new VitalsDashboardStatsDto(), // Ensure Stats is not null
                VitalsQueue = (await queueTask)?.ToList() ?? new List<VitalsQueueItemDto>() // Ensure Queue is not null
            };

            // TODO: Calculate Age and WaitTime for each VitalsQueueItemDto
            // For now, placeholder data from repo is used directly.
            // Example:
            // foreach (var item in viewModel.VitalsQueue)
            // {
            //    item.Age = CalculateAge(patient.DateOfBirth); // If patient DoB is fetched
            //    item.WaitTime = DateTime.UtcNow - item.CheckinTimestamp;
            // }

            return viewModel;
        }
    }
}