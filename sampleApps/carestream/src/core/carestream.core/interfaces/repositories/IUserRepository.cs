using carestream.core.dtos.admin;
using carestream.core.dtos.user;
using System.Data;

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations related to system users.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves the internal application user ID based on the Logto subject (sub) claim.
        /// </summary>
        /// <param name="logtoSub">The Logto subject identifier.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The internal user ID if a link exists; otherwise, null.</returns>
        Task<int?> GetUserIdByLogtoSubAsync(string logtoSub, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves the Logto subject (sub) claim based on the internal application user ID.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<UserVerificationCodeInfo?> GetUserVerificationCodeInfoAsync(int userId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Sets the verification code for a user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="hashedCode"></param>
        /// <param name="salt"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<bool> SetUserVerificationCodeAsync(int userId, string hashedCode, string salt, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a list of users for admin management.
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<IEnumerable<AdminUserListItemDto>> GetAllUsersForAdminAsync(string? searchTerm = null, int pageSize = 25, int pageNumber = 1, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Links a user to a Logto subject (sub) claim.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="logtoSub"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<bool> LinkLogtoSubAsync(int userId, string logtoSub, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a specific user's basic information for admin purposes.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<AdminUserListItemDto?> GetUserForAdminByIdAsync(int userId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves all facilities a specific user is linked to.
        /// </summary>
        /// <param name="internalUserId">The ID of the user.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable collection of <see cref="UserFacilityLinkDto"/> representing the user's facility links.</returns>
        Task<IEnumerable<UserFacilityLinkDto>> GetUserFacilityLinksAsync(int internalUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves comprehensive details for a user for administrative viewing (e.g., in a detail panel).
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An <see cref="AdminUserDetailDto"/> if found; otherwise, null.</returns>
        Task<AdminUserDetailDto?> GetUserDetailForAdminAsync(int userId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates a user's basic personal information (rank, department, active status) from the admin panel.
        /// </summary>
        /// <param name="userEditInput">The DTO containing updated user information.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateUserPersonalInfoForAdminAsync(AdminUserEditInputDto userEditInput, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Links a user to a specific facility in the app.user_facilities junction table.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="facilityId">The ID of the facility.</param>
        /// <param name="createdByUserId">The ID of the admin user performing the link.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the link was successfully created, false otherwise (e.g., already linked).</returns>
        Task<bool> LinkUserToFacilityAsync(int userId, int facilityId, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Unlinks a user from a specific facility in the app.user_facilities junction table.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="facilityId">The ID of the facility to unlink from.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the link was successfully removed, false otherwise (e.g., link not found, or it's their last facility).</returns>
        Task<bool> UnlinkUserFromFacilityAsync(int userId, int facilityId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Sets a specific facility as the default for a user, automatically unsetting any previous default for that user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="facilityId">The ID of the facility to set as default.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the default was successfully set, false otherwise.</returns>
        Task<bool> SetUserDefaultFacilityLinkAsync(int userId, int facilityId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new user in the database.
        /// </summary>
        /// <param name="userDto">The DTO containing the user's details.</param>
        /// <param name="createdByUserId">The ID of the user performing the creation.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created user.</returns>
        Task<int> CreateUserAsync(CreateUserInputDto userDto, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}