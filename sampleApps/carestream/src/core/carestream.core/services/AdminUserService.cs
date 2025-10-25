using Microsoft.Extensions.Logging;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.admin;

namespace carestream.core.services
{
    /// <summary>
    /// Service for administrative user management.
    /// </summary>
    public class AdminUserService : IAdminUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AdminUserService> _logger;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IFacilityRepository _facilityRepository;

        public AdminUserService(
            IUserRepository userRepository,
            ILogger<AdminUserService> logger,
            IPasswordHasherService passwordHasherService,
            IFacilityRepository facilityRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _passwordHasherService = passwordHasherService ?? throw new ArgumentNullException(nameof(passwordHasherService));
            _facilityRepository = facilityRepository ?? throw new ArgumentNullException(nameof(facilityRepository));
        }

        /// <inheritdoc/>
        public async Task<AdminUserManagementViewModel> GetUserManagementViewModelAsync(string? searchTerm = null, int pageNumber = 1, int pageSize = 25)
        {
            _logger.LogInformation("Service: Getting user management view model. Search: '{SearchTerm}', Page: {Page}, Size: {Size}", searchTerm, pageNumber, pageSize);
            var users = await _userRepository.GetAllUsersForAdminAsync(searchTerm, pageSize, pageNumber);
            return new AdminUserManagementViewModel
            {
                Users = users.ToList(),
                SearchTerm = searchTerm
            };
        }

        /// <inheritdoc/>
        public async Task<bool> LinkUserToLogtoAsync(int userId, string logtoSub)
        {
            _logger.LogInformation("Service: Attempting to link UserId {UserId} to LogtoSub {LogtoSub}", userId, logtoSub);
            if (userId <= 0 || string.IsNullOrWhiteSpace(logtoSub))
            {
                _logger.LogWarning("Service: Invalid parameters for LinkUserToLogtoAsync. UserId: {UserId}, LogtoSub: {LogtoSub}", userId, logtoSub);
                return false;
            }

            return await _userRepository.LinkLogtoSubAsync(userId, logtoSub);
        }

        /// <inheritdoc/>
        public async Task<AdminUserListItemDto?> GetUserForAdminByIdAsync(int userId)
        {
            _logger.LogInformation("Service: Getting user by ID {UserId} for admin list view.", userId);
            return await _userRepository.GetUserForAdminByIdAsync(userId);
        }

        /// <inheritdoc/>
        public async Task<bool> SetUserVerificationCodeAsync(int userId, string newVerificationCode)
        {
            _logger.LogInformation("Service: Attempting to set verification code for UserId {UserId}", userId);

            if (userId <= 0 || string.IsNullOrWhiteSpace(newVerificationCode))
            {
                _logger.LogWarning("Service: Invalid input for SetUserVerificationCodeAsync. UserId: {UserId}", userId);
                return false;
            }

            var userExists = await _userRepository.GetUserForAdminByIdAsync(userId);
            if (userExists == null)
            {
                _logger.LogWarning("Service: User not found for SetUserVerificationCodeAsync. UserId: {UserId}", userId);
                return false;
            }

            string salt;
            string hashedPassword = _passwordHasherService.HashPassword(newVerificationCode, out salt);

            bool success = await _userRepository.SetUserVerificationCodeAsync(userId, hashedPassword, salt);

            if (success)
            {
                _logger.LogInformation("Service: Successfully set verification code for UserId {UserId}", userId);
            }
            else
            {
                _logger.LogError("Service: Failed to save verification code hash and salt for UserId {UserId}", userId);
            }
            return success;
        }

        /// <inheritdoc/>
        public async Task<AdminUserDetailDto?> GetAdminUserDetailAsync(int userId)
        {
            _logger.LogInformation("Service: Getting detailed user info for admin view, UserId: {UserId}", userId);
            var userDetail = await _userRepository.GetUserDetailForAdminAsync(userId);

            if (userDetail == null)
            {
                _logger.LogWarning("Service: AdminUserDetailDto not found for UserId: {UserId}", userId);
                return null;
            }

            userDetail.LinkedFacilities = (await _userRepository.GetUserFacilityLinksAsync(userId)).ToList();
            userDetail.AllActiveFacilities = (await _facilityRepository.GetAllActiveFacilitiesAsync()).ToList();

            _logger.LogDebug("Service: User {UserId} has {LinkedCount} linked facilities and {AllFacilitiesCount} total active facilities.",
                userId, userDetail.LinkedFacilities.Count, userDetail.AllActiveFacilities.Count);

            return userDetail;
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateAdminUserPersonalInfoAsync(AdminUserEditInputDto userEditInput)
        {
            _logger.LogInformation("Service: Updating personal info for UserId: {UserId}", userEditInput.UserId);

            if (userEditInput.UserId <= 0)
            {
                _logger.LogWarning("Service: Invalid UserId for UpdateAdminUserPersonalInfoAsync: {UserId}", userEditInput.UserId);
                return false;
            }

            return await _userRepository.UpdateUserPersonalInfoForAdminAsync(userEditInput);
        }

        /// <inheritdoc/>
        public async Task<bool> LinkUserToFacilityAsync(AdminUserFacilityLinkInputDto linkInput, int adminUserId)
        {
            _logger.LogInformation("Service: Admin user {AdminUserId} attempting to link user {UserId} to facility {FacilityId}.",
                adminUserId, linkInput.UserId, linkInput.FacilityId);

            if (linkInput.UserId <= 0 || linkInput.FacilityId <= 0 || adminUserId <= 0)
            {
                _logger.LogWarning("Service: Invalid input for LinkUserToFacilityAsync. UserId: {UserId}, FacilityId: {FacilityId}, AdminUserId: {AdminUserId}",
                    linkInput.UserId, linkInput.FacilityId, adminUserId);
                return false;
            }

            // Business rule: Check if user already linked (repository's ON CONFLICT handles it, but explicit check for feedback)
            var existingLinks = await _userRepository.GetUserFacilityLinksAsync(linkInput.UserId);
            if (existingLinks.Any(l => l.FacilityId == linkInput.FacilityId))
            {
                _logger.LogWarning("Service: User {UserId} is already linked to facility {FacilityId}.", linkInput.UserId, linkInput.FacilityId);
                return false;
            }

            // Business rule: Ensure facility exists and is active (optional, but good)
            var facilityExists = await _facilityRepository.GetFacilityByIdAsync(linkInput.FacilityId);
            if (facilityExists == null || !facilityExists.IsActive)
            {
                _logger.LogWarning("Service: Attempted to link user {UserId} to non-existent or inactive facility {FacilityId}.", linkInput.UserId, linkInput.FacilityId);
                return false;
            }

            // If it's intended to be default, ensure user doesn't already have one, or handle the update.
            // For now, if linkInput.IsDefault is true, SetAdminUserDefaultFacilityAsync should be called separately or handled by repo.
            // The LinkUserToFacilityAsync simply creates the link. SetDefault handles the default flag.
            return await _userRepository.LinkUserToFacilityAsync(linkInput.UserId, linkInput.FacilityId, adminUserId);
        }

        /// <inheritdoc/>
        public async Task<bool> UnlinkUserFromFacilityAsync(int userId, int facilityId)
        {
            _logger.LogInformation("Service: Attempting to unlink user {UserId} from facility {FacilityId}.", userId, facilityId);

            if (userId <= 0 || facilityId <= 0)
            {
                _logger.LogWarning("Service: Invalid input for UnlinkUserFromFacilityAsync. UserId: {UserId}, FacilityId: {FacilityId}", userId, facilityId);
                return false;
            }

            // Business rule: Prevent unlinking if it's the user's last facility.
            var linkedFacilities = await _userRepository.GetUserFacilityLinksAsync(userId);
            if (linkedFacilities.Count() <= 1)
            {
                _logger.LogWarning("Service: Cannot unlink user {UserId} from facility {FacilityId}: it is their last linked facility. A user must belong to at least one facility.", userId, facilityId);
                return false;
            }

            // If the facility to be unlinked is their current default, the system will re-evaluate default via GetDefaultFacilityForUserAsync upon next login/context refresh.
            // If the facility to be unlinked is the _only_ default, the constraint will fail.
            // The GetDefaultFacilityForUserAsync logic will pick a new default.

            return await _userRepository.UnlinkUserFromFacilityAsync(userId, facilityId);
        }

        /// <inheritdoc/>
        public async Task<bool> SetAdminUserDefaultFacilityAsync(int userId, int facilityId)
        {
            _logger.LogInformation("Service: Attempting to set default facility for user {UserId} to {FacilityId}.", userId, facilityId);

            if (userId <= 0 || facilityId <= 0)
            {
                _logger.LogWarning("Service: Invalid input for SetAdminUserDefaultFacilityAsync. UserId: {UserId}, FacilityId: {FacilityId}", userId, facilityId);
                return false;
            }

            // Business rule: Ensure the user is actually linked to this facility before setting it as default.
            var linkedFacilities = await _userRepository.GetUserFacilityLinksAsync(userId);
            if (!linkedFacilities.Any(l => l.FacilityId == facilityId))
            {
                _logger.LogWarning("Service: User {UserId} is not linked to facility {FacilityId}. Cannot set it as default.", userId, facilityId);
                return false;
            }

            return await _userRepository.SetUserDefaultFacilityLinkAsync(userId, facilityId);
        }

        /// <inheritdoc/>
        public async Task<int?> CreateUserAsync(CreateUserInputDto userDto, int createdByUserId)
        {
            _logger.LogInformation("Service: Attempting to create new user with ForceNumber: {ForceNumber}", userDto.ForceNumber);

            // Basic validation (more comprehensive validation via DataAnnotations in DTO and ModelState.IsValid in Controller)
            if (string.IsNullOrWhiteSpace(userDto.ForceNumber) || string.IsNullOrWhiteSpace(userDto.FirstName) || string.IsNullOrWhiteSpace(userDto.LastName) || userDto.InitialFacilityId <= 0 || createdByUserId <= 0)
            {
                _logger.LogWarning("Service: Invalid input for CreateUserAsync. Missing required fields or invalid Facility ID/CreatedByUserId.");
                return null;
            }

            // Business rule: Ensure initial facility exists and is active
            var initialFacility = await _facilityRepository.GetFacilityByIdAsync(userDto.InitialFacilityId);
            if (initialFacility == null || !initialFacility.IsActive)
            {
                _logger.LogWarning("Service: Attempted to create user with non-existent or inactive initial facility {FacilityId}.", userDto.InitialFacilityId);
                return null;
            }

            try
            {
                var newUserId = await _userRepository.CreateUserAsync(userDto, createdByUserId);
                _logger.LogInformation("Service: Successfully created new user with UserId: {UserId}", newUserId);
                return newUserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: Failed to create user with ForceNumber: {ForceNumber}", userDto.ForceNumber);
                return null;
            }
        }
    }
}