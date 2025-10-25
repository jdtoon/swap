using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using carestream.core.dtos.facility;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;
using System; // For ArgumentNullException

namespace carestream.core.services
{
    /// <summary>
    /// Service responsible for managing user facility assignments and selection.
    /// </summary>
    public class FacilitySelectionService : IFacilitySelectionService
    {
        private readonly IFacilityRepository _facilityRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<FacilitySelectionService> _logger;

        public FacilitySelectionService(
            IFacilityRepository facilityRepository,
            IUserRepository userRepository,
            ILogger<FacilitySelectionService> logger)
        {
            _facilityRepository = facilityRepository ?? throw new ArgumentNullException(nameof(facilityRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FacilityDto>> GetFacilitiesForUserAsync(int internalUserId)
        {
            _logger.LogInformation("Service: Getting facilities for internal user ID: {InternalUserId}", internalUserId);

            // Get all facility links for the user from the new junction table
            var userFacilityLinks = (await _userRepository.GetUserFacilityLinksAsync(internalUserId)).ToList();

            if (!userFacilityLinks.Any())
            {
                _logger.LogWarning("Service: User {InternalUserId} has no facility links in app.user_facilities. Returning empty list.", internalUserId);
                return Enumerable.Empty<FacilityDto>();
            }

            // Get details for all active facilities that the user is linked to
            var allActiveFacilities = (await _facilityRepository.GetAllActiveFacilitiesAsync()).ToList();

            var accessibleFacilities = allActiveFacilities
                .Where(f => userFacilityLinks.Any(link => link.FacilityId == f.FacilityId))
                .ToList();

            if (!accessibleFacilities.Any())
            {
                _logger.LogWarning("Service: User {InternalUserId} is linked to facilities, but none are active. Returning empty list.", internalUserId);
            }
            else
            {
                _logger.LogDebug("Service: User {InternalUserId} has access to {Count} active facilities.", internalUserId, accessibleFacilities.Count);
            }

            return accessibleFacilities;
        }

        /// <inheritdoc/>
        public async Task<FacilityDto?> SetActiveFacilityForUserAsync(int internalUserId, int facilityId)
        {
            _logger.LogInformation("Service: Attempting to set active facility for user {InternalUserId} to Facility ID: {FacilityId}", internalUserId, facilityId);

            var accessibleFacilities = (await GetFacilitiesForUserAsync(internalUserId)).ToList();
            var selectedFacility = accessibleFacilities.FirstOrDefault(f => f.FacilityId == facilityId);

            if (selectedFacility == null)
            {
                _logger.LogWarning("Service: User {InternalUserId} does not have access to Facility ID {FacilityId} or it is inactive.", internalUserId, facilityId);
                return null;
            }

            // Optional: Update the user's default facility preference in the database (app.user_facilities.is_default)
            // For now, we're simply validating access. If you need to persist the selection as the new default,
            // you'd add a method to IUserRepository/UserRepository to update the is_default flag in app.user_facilities.
            // Example (if method existed in IUserRepository): await _userRepository.SetUserDefaultFacilityAsync(internalUserId, facilityId);
            _logger.LogInformation("Service: User {InternalUserId} successfully validated access to {FacilityName} (ID: {FacilityId}).", internalUserId, selectedFacility.Name, selectedFacility.FacilityId);
            return selectedFacility;
        }

        /// <inheritdoc/>
        public async Task<FacilityDto?> GetDefaultFacilityForUserAsync(int internalUserId)
        {
            _logger.LogInformation("Service: Getting default facility for user {InternalUserId}.", internalUserId);

            var userFacilities = (await GetFacilitiesForUserAsync(internalUserId)).ToList();

            if (!userFacilities.Any())
            {
                _logger.LogWarning("Service: User {InternalUserId} has no accessible facilities.", internalUserId);
                return null;
            }

            // Prioritize a facility marked as default in app.user_facilities
            var defaultLink = (await _userRepository.GetUserFacilityLinksAsync(internalUserId))
                                .FirstOrDefault(link => link.IsDefault);

            if (defaultLink != null)
            {
                var defaultFacility = userFacilities.FirstOrDefault(f => f.FacilityId == defaultLink.FacilityId);
                if (defaultFacility != null)
                {
                    _logger.LogDebug("Service: User {InternalUserId} has a default facility preference: {FacilityName} (ID: {FacilityId}).", internalUserId, defaultFacility.Name, defaultFacility.FacilityId);
                    return defaultFacility;
                }
            }

            // Fallback: If no explicit default, or default is inactive/inaccessible, pick the first active one.
            var fallbackFacility = userFacilities.OrderBy(f => f.FacilityId).FirstOrDefault();
            if (fallbackFacility != null)
            {
                _logger.LogDebug("Service: User {InternalUserId} defaulting to first accessible facility: {FacilityName} (ID: {FacilityId}).", internalUserId, fallbackFacility.Name, fallbackFacility.FacilityId);
            }
            else
            {
                _logger.LogWarning("Service: No fallback facility found for user {InternalUserId}.", internalUserId);
            }
            return fallbackFacility;
        }
    }
}