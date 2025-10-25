using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using carestream.core.dtos.admin.facility;
using carestream.core.dtos.shared;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;

namespace carestream.core.services
{
    /// <summary>
    /// Service for administrative management of Departments.
    /// </summary>
    public class DepartmentAdminService : IDepartmentAdminService
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IFacilityRepository _facilityRepository;
        private readonly ILogger<DepartmentAdminService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepartmentAdminService"/> class.
        /// </summary>
        /// <param name="departmentRepository">The repository for department data.</param>
        /// <param name="facilityRepository">The repository for facility data.</param>
        /// <param name="logger">The logger for this service.</param>
        public DepartmentAdminService(
            IDepartmentRepository departmentRepository,
            IFacilityRepository facilityRepository,
            ILogger<DepartmentAdminService> logger)
        {
            _departmentRepository = departmentRepository ?? throw new ArgumentNullException(nameof(departmentRepository));
            _facilityRepository = facilityRepository ?? throw new ArgumentNullException(nameof(facilityRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<DepartmentListViewModel> GetDepartmentsViewModelAsync(int facilityId, FilterAndPaginationOptions options)
        {
            if (facilityId <= 0)
            {
                return new DepartmentListViewModel();
            }

            var facility = await _facilityRepository.GetFacilityByIdAsync(facilityId);
            if (facility == null)
            {
                _logger.LogWarning("Service: Facility ID {FacilityId} not found for GetDepartmentsViewModelAsync.", facilityId);
                return new DepartmentListViewModel();
            }

            var (departments, totalCount) = await _departmentRepository.GetDepartmentsByFacilityAsync(facilityId, options);

            var viewModel = new DepartmentListViewModel
            {
                FacilityId = facilityId,
                FacilityName = facility.Name,
                Departments = departments.ToList(),
                PaginationInfo = new PaginationDto
                {
                    CurrentPage = options.PageNumber,
                    PageSize = options.PageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)options.PageSize)
                },
                CurrentFilters = options
            };

            return viewModel;
        }

        /// <inheritdoc/>
        public async Task<DepartmentDto?> GetDepartmentByIdAsync(int departmentId)
        {
            if (departmentId <= 0)
            {
                return null;
            }
            return await _departmentRepository.GetDepartmentByIdAsync(departmentId);
        }

        /// <inheritdoc/>
        public async Task<bool> CreateDepartmentAsync(CreateUpdateDepartmentDto departmentDto, int createdByUserId)
        {
            if (departmentDto.FacilityId <= 0)
            {
                _logger.LogWarning("Service: Invalid FacilityId for CreateDepartmentAsync: {FacilityId}", departmentDto.FacilityId);
                return false;
            }

            var existingDepartment = await _departmentRepository.GetDepartmentByNameAndFacilityAsync(departmentDto.FacilityId, departmentDto.Name);
            if (existingDepartment != null)
            {
                _logger.LogWarning("Service: Department with name '{Name}' already exists in Facility {FacilityId}.", departmentDto.Name, departmentDto.FacilityId);
                return false;
            }

            var facility = await _facilityRepository.GetFacilityByIdAsync(departmentDto.FacilityId);
            if (facility == null || !facility.IsActive)
            {
                _logger.LogWarning("Service: Target Facility {FacilityId} not found or not active for department creation.", departmentDto.FacilityId);
                return false;
            }

            try
            {
                int newDepartmentId = await _departmentRepository.CreateDepartmentAsync(departmentDto, createdByUserId);
                return newDepartmentId > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: Failed to create department '{Name}' in Facility {FacilityId}.", departmentDto.Name, departmentDto.FacilityId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateDepartmentAsync(CreateUpdateDepartmentDto departmentDto, int updatedByUserId)
        {
            if (!departmentDto.DepartmentId.HasValue || departmentDto.DepartmentId.Value <= 0 || departmentDto.FacilityId <= 0)
            {
                return false;
            }

            var existingDepartment = await _departmentRepository.GetDepartmentByIdAsync(departmentDto.DepartmentId.Value);
            if (existingDepartment == null || existingDepartment.FacilityId != departmentDto.FacilityId)
            {
                _logger.LogWarning("Service: Department ID {DepartmentId} not found or does not belong to Facility {FacilityId} for update.", departmentDto.DepartmentId, departmentDto.FacilityId);
                return false;
            }

            if (existingDepartment.Name != departmentDto.Name)
            {
                var duplicateNameCheck = await _departmentRepository.GetDepartmentByNameAndFacilityAsync(departmentDto.FacilityId, departmentDto.Name);
                if (duplicateNameCheck != null && duplicateNameCheck.DepartmentId != departmentDto.DepartmentId.Value)
                {
                    _logger.LogWarning("Service: Another department with name '{Name}' already exists in Facility {FacilityId} during update.", departmentDto.Name, departmentDto.FacilityId);
                    return false;
                }
            }

            try
            {
                return await _departmentRepository.UpdateDepartmentAsync(departmentDto, updatedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: Failed to update department ID {Id} in Facility {FacilityId}.", departmentDto.DepartmentId, departmentDto.FacilityId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeactivateDepartmentAsync(int departmentId, int deactivatedByUserId)
        {
            if (departmentId <= 0)
            {
                return false;
            }

            var department = await _departmentRepository.GetDepartmentByIdAsync(departmentId);
            if (department == null || !department.IsActive)
            {
                _logger.LogWarning("Service: Department ID {DepartmentId} not found or already inactive for deactivation.", departmentId);
                return false;
            }

            // Business rule: Check if department has active wards/users before deactivating.
            // For now, simple deactivation.
            try
            {
                return await _departmentRepository.DeactivateDepartmentAsync(departmentId, deactivatedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: Failed to deactivate department ID {Id}.", departmentId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsForFacilityAsync(int facilityId)
        {
            if (facilityId <= 0)
            {
                _logger.LogWarning("Service: Invalid FacilityId for GetAllDepartmentsForFacilityAsync: {FacilityId}", facilityId);
                return Enumerable.Empty<DepartmentDto>();
            }
            return await _departmentRepository.GetAllActiveDepartmentsByFacilityAsync(facilityId);
        }
    }
}