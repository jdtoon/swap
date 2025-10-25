using carestream.core.dtos.dashboard;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Service responsible for retrieving data required for the
    /// Patient Administration Dashboard.
    /// </summary>
    public interface IPatientAdminDashboardService
    {
        /// <summary>
        /// Gets the consolidated view model data for the Patient Admin dashboard.
        /// </summary>
        /// <returns>A view model containing dashboard statistics and recent items.</returns>
        Task<PatientAdminDashboardViewModel> GetDashboardViewModelAsync();

        /// <summary>
        /// Retrieves the view model for the Patient Admin Dashboard, including statistics and recent activity.
        /// </summary>
        /// <returns>A <see cref="PatientAdminDashboardViewModel"/>.</returns>
        Task<PatientAdminDashboardViewModel> GetPatientAdminDashboardViewModelAsync();
    }
}
