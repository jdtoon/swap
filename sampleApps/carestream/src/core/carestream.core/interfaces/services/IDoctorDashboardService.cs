using carestream.core.dtos.doctor;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines service operations for the Doctor's dashboard.
    /// </summary>
    public interface IDoctorDashboardService
    {
        /// <summary>
        /// Gets the view model data for the Doctor's dashboard, including stats and patient queue.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation, containing the <see cref="DoctorDashboardViewModel"/>.</returns>
        Task<DoctorDashboardViewModel> GetDashboardViewModelAsync();
    }
}