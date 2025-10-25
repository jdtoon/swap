using System.Collections.Generic;
using System.Threading.Tasks;
using carestream.core.dtos.admin.facility;
using carestream.core.dtos.shared;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines service operations for administrative management of Departments.
    /// </summary>
    public interface IDepartmentAdminService
    {
        /// <summary>
        /// Retrieves a paginated and filtered list of departments for a specific facility.
        /// </summary>
        /// <param name="facilityId">The ID of the facility for which to retrieve departments.</param>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A <see cref="DepartmentListViewModel"/> containing the departments.</returns>
        Task<DepartmentListViewModel> GetDepartmentsViewModelAsync(int facilityId, FilterAndPaginationOptions options);

        /// <summary>
        /// Retrieves a single department by its unique identifier for administrative purposes.
        /// </summary>
        /// <param name="departmentId">The unique ID of the department.</param>
        /// <returns>A <see cref="DepartmentDto"/> if found, otherwise null.</returns>
        Task<DepartmentDto?> GetDepartmentByIdAsync(int departmentId);

        /// <summary>
        /// Creates a new department within a specified facility.
        /// </summary>
        /// <param name="departmentDto">The DTO containing the data for the new department.</param>
        /// <param name="createdByUserId">The ID of the admin user creating the department.</param>
        /// <returns>True if the department was successfully created, false otherwise.</returns>
        Task<bool> CreateDepartmentAsync(CreateUpdateDepartmentDto departmentDto, int createdByUserId);

        /// <summary>
        /// Updates an existing department.
        /// </summary>
        /// <param name="departmentDto">The DTO containing the updated department data.</param>
        /// <param name="updatedByUserId">The ID of the admin user updating the department.</param>
        /// <returns>True if the department was successfully updated, false otherwise.</returns>
        Task<bool> UpdateDepartmentAsync(CreateUpdateDepartmentDto departmentDto, int updatedByUserId);

        /// <summary>
        /// Deactivates a department by its unique identifier.
        /// </summary>
        /// <param name="departmentId">The ID of the department to deactivate.</param>
        /// <param name="deactivatedByUserId">The ID of the admin user deactivating the department.</param>
        /// <returns>True if the department was successfully deactivated, false otherwise.</returns>
        Task<bool> DeactivateDepartmentAsync(int departmentId, int deactivatedByUserId);

        /// <summary>
        /// Retrieves all active departments for a specific facility, typically for dropdowns or selection lists.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to retrieve departments for.</param>
        /// <returns>An enumerable collection of active <see cref="DepartmentDto"/>.</returns>
        Task<IEnumerable<DepartmentDto>> GetAllDepartmentsForFacilityAsync(int facilityId);
    }
}