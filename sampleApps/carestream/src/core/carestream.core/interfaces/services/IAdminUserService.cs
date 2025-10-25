using carestream.core.dtos.admin;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines service operations for administrative user management.
    /// </summary>
    public interface IAdminUserService
    {
        /// <summary>
        /// Retrieves a list of users for admin management.
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<AdminUserManagementViewModel> GetUserManagementViewModelAsync(string? searchTerm = null, int pageNumber = 1, int pageSize = 25);

        /// <summary>
        /// Links a user to a Logto subject (sub) claim.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="logtoSub"></param>
        /// <returns></returns>
        Task<bool> LinkUserToLogtoAsync(int userId, string logtoSub);

        /// <summary>
        /// Retrieves a user for admin management by their ID (basic list item DTO).
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<AdminUserListItemDto?> GetUserForAdminByIdAsync(int userId);

        /// <summary>
        /// Sets or resets a user's verification code.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="newVerificationCode">The new plain-text verification code.</param>
        /// <returns>True if successful, false otherwise (e.g., user not found, validation failed).</returns>
        Task<bool> SetUserVerificationCodeAsync(int userId, string newVerificationCode);

        /// <summary>
        /// Retrieves comprehensive details for a user for administrative viewing and editing.
        /// Includes user's personal details and linked facilities.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>An <see cref="AdminUserDetailDto"/> if found; otherwise, null.</returns>
        Task<AdminUserDetailDto?> GetAdminUserDetailAsync(int userId);

        /// <summary>
        /// Updates a user's basic personal information (rank, department, active status) from the admin panel.
        /// </summary>
        /// <param name="userEditInput">The DTO containing updated user information.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateAdminUserPersonalInfoAsync(AdminUserEditInputDto userEditInput);

        /// <summary>
        /// Links a user to a specific facility in the admin panel.
        /// </summary>
        /// <param name="linkInput">DTO containing user and facility IDs.</param>
        /// <param name="adminUserId">The ID of the admin user performing the link.</param>
        /// <returns>True if the link was successfully created, false otherwise (e.g., already linked).</returns>
        Task<bool> LinkUserToFacilityAsync(AdminUserFacilityLinkInputDto linkInput, int adminUserId); 

        /// <summary>
        /// Unlinks a user from a specific facility in the admin panel.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="facilityId">The ID of the facility to unlink from.</param>
        /// <returns>True if the link was successfully removed, false otherwise (e.g., link not found, or it's their last facility).</returns>
        Task<bool> UnlinkUserFromFacilityAsync(int userId, int facilityId);

        /// <summary>
        /// Sets a specific facility as the default for a user from the admin panel.
        /// Automatically unsets any previous default for that user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="facilityId">The ID of the facility to set as default.</param>
        /// <returns>True if the default was successfully set, false otherwise.</returns>
        Task<bool> SetAdminUserDefaultFacilityAsync(int userId, int facilityId);

        /// <summary>
        /// Creates a new user in the application.
        /// </summary>
        /// <param name="userDto">The DTO containing the user's details.</param>
        /// <param name="createdByUserId">The ID of the admin user performing the creation.</param>
        /// <returns>The ID of the newly created user if successful; otherwise, null.</returns>
        Task<int?> CreateUserAsync(CreateUserInputDto userDto, int createdByUserId);
    }
}