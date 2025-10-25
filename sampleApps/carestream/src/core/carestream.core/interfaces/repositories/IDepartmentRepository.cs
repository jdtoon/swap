using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data; // For IDbConnection, IDbTransaction
using carestream.core.dtos.admin.facility; // For DepartmentDto, CreateUpdateDepartmentDto, DepartmentDetailWithChildrenDto
using carestream.core.dtos.shared; // For FilterAndPaginationOptions

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for Departments.
    /// </summary>
    public interface IDepartmentRepository
    {
        /// <summary>
        /// Retrieves a department by its unique identifier.
        /// </summary>
        /// <param name="departmentId">The unique ID of the department.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="DepartmentDto"/> if found, otherwise null.</returns>
        Task<DepartmentDto?> GetDepartmentByIdAsync(int departmentId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a department by its name within a specific facility.
        /// </summary>
        /// <param name="facilityId">The ID of the facility.</param>
        /// <param name="departmentName">The name of the department.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="DepartmentDto"/> if found, otherwise null.</returns>
        Task<DepartmentDto?> GetDepartmentByNameAndFacilityAsync(int facilityId, string departmentName, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated and filtered list of departments for a specific facility.
        /// </summary>
        /// <param name="facilityId">The ID of the facility.</param>
        /// <param name="options">Filtering and pagination options.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A tuple containing the list of departments and the total count.</returns>
        Task<(IEnumerable<DepartmentDto> Items, int TotalCount)> GetDepartmentsByFacilityAsync(int facilityId, FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves all active departments for a specific facility.
        /// </summary>
        /// <param name="facilityId">The ID of the facility.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable collection of <see cref="DepartmentDto"/>.</returns>
        Task<IEnumerable<DepartmentDto>> GetAllActiveDepartmentsByFacilityAsync(int facilityId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new department.
        /// </summary>
        /// <param name="department">The DTO containing the department data.</param>
        /// <param name="createdByUserId">The ID of the user creating the department.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created department, or 0 if creation failed.</returns>
        Task<int> CreateDepartmentAsync(CreateUpdateDepartmentDto department, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates an existing department.
        /// </summary>
        /// <param name="department">The DTO containing the updated department data.</param>
        /// <param name="updatedByUserId">The ID of the user updating the department.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateDepartmentAsync(CreateUpdateDepartmentDto department, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Deactivates a department by its unique identifier.
        /// </summary>
        /// <param name="departmentId">The ID of the department to deactivate.</param>
        /// <param name="deactivatedByUserId">The ID of the user deactivating the department.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the department was successfully deactivated, false otherwise.</returns>
        Task<bool> DeactivateDepartmentAsync(int departmentId, int deactivatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves comprehensive details for a department, including its associated wards.
        /// </summary>
        /// <param name="departmentId">The ID of the department.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="DepartmentDetailWithChildrenDto"/> if found; otherwise, null.</returns>
        Task<DepartmentDetailWithChildrenDto?> GetDepartmentDetailsWithChildrenAsync(int departmentId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}