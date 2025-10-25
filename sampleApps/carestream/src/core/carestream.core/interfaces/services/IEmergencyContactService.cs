using System.Collections.Generic;
using System.Threading.Tasks;
using carestream.core.dtos.emergency; // For EmergencyContactViewModel

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines service operations for retrieving emergency contact information.
    /// </summary>
    public interface IEmergencyContactService
    {
        /// <summary>
        /// Retrieves the view model containing all emergency contacts and important notices.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.
        /// The task result contains an <see cref="EmergencyContactViewModel"/>.</returns>
        Task<EmergencyContactViewModel> GetEmergencyContactsAsync();
    }
}