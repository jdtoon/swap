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
    /// Service for administrative management of Wards.
    /// </summary>
    public class WardAdminService : IWardAdminService
    {
        private readonly IWardRepository _wardRepository;
        private readonly IFacilityRepository _facilityRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ILogger<WardAdminService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WardAdminService"/> class.
        /// </summary>
        /// <param name="wardRepository">The repository for ward data.</param>
        /// <param name="facilityRepository">The repository for facility data.</param>
        /// <param name="departmentRepository">The repository for department data.</param>
        /// <param name="logger">The logger for this service.</param>
        public WardAdminService(
            IWardRepository wardRepository,
            IFacilityRepository facilityRepository,
            IDepartmentRepository departmentRepository,
            ILogger<WardAdminService> logger)
        {
            _wardRepository = wardRepository ?? throw new ArgumentNullException(nameof(wardRepository));
            _facilityRepository = facilityRepository ?? throw new ArgumentNullException(nameof(facilityRepository));
            _departmentRepository = departmentRepository ?? throw new ArgumentNullException(nameof(departmentRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<WardListViewModel> GetWardsViewModelAsync(int facilityId, FilterAndPaginationOptions options)
        {
            if (facilityId <= 0)
            {
                return new WardListViewModel();
            }

            var facility = await _facilityRepository.GetFacilityByIdAsync(facilityId);
            if (facility == null)
            {
                _logger.LogWarning("Service: Facility ID {FacilityId} not found for GetWardsViewModelAsync.", facilityId);
                return new WardListViewModel();
            }

            var departmentIdFilter = options.SearchTerm2;
            int? parsedDepartmentId = null;
            if (int.TryParse(departmentIdFilter, out int id))
            {
                parsedDepartmentId = id;
            }

            var (wards, totalCount) = await _wardRepository.GetWardsByFacilityAsync(facilityId, options);

            var viewModel = new WardListViewModel
            {
                FacilityId = facilityId,
                FacilityName = facility.Name,
                DepartmentId = parsedDepartmentId,
                Wards = wards.ToList(),
                PaginationInfo = new PaginationDto
                {
                    CurrentPage = options.PageNumber,
                    PageSize = options.PageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)options.PageSize)
                },
                CurrentFilters = options,
                AllFacilities = (await _facilityRepository.GetAllActiveFacilitiesAsync()).ToList(),
                AllDepartments = (await _departmentRepository.GetAllActiveDepartmentsByFacilityAsync(facilityId)).ToList() 
            };

            if (parsedDepartmentId.HasValue)
            {
                viewModel.DepartmentName = viewModel.AllDepartments.FirstOrDefault(d => d.DepartmentId == parsedDepartmentId)?.Name;
            }

            return viewModel;
        }

        /// <inheritdoc/>
        public async Task<WardDto?> GetWardByIdAsync(int wardId)
        {
            if (wardId <= 0)
            {
                return null;
            }
            return await _wardRepository.GetWardByIdAsync(wardId);
        }

        /// <inheritdoc/>
        public async Task<bool> CreateWardAsync(CreateUpdateWardDto wardDto, int createdByUserId)
        {
            if (wardDto.FacilityId <= 0)
            {
                _logger.LogWarning("Service: Invalid FacilityId for CreateWardAsync: {FacilityId}", wardDto.FacilityId);
                return false;
            }

            var existingWard = await _wardRepository.GetWardByNameAndFacilityAsync(wardDto.FacilityId, wardDto.Name, wardDto.DepartmentId);
            if (existingWard != null)
            {
                _logger.LogWarning("Service: Ward with name '{Name}' already exists in Facility {FacilityId} (Department {DepartmentId}).", wardDto.Name, wardDto.FacilityId, wardDto.DepartmentId);
                return false;
            }

            var facility = await _facilityRepository.GetFacilityByIdAsync(wardDto.FacilityId);
            if (facility == null || !facility.IsActive)
            {
                _logger.LogWarning("Service: Target Facility {FacilityId} not found or not active for ward creation.", wardDto.FacilityId);
                return false;
            }

            if (wardDto.DepartmentId.HasValue)
            {
                var department = await _departmentRepository.GetDepartmentByIdAsync(wardDto.DepartmentId.Value);
                if (department == null || department.FacilityId != wardDto.FacilityId || !department.IsActive)
                {
                    _logger.LogWarning("Service: Target Department {DepartmentId} not found, inactive, or does not belong to Facility {FacilityId} for ward creation.", wardDto.DepartmentId, wardDto.FacilityId);
                    return false;
                }
            }

            try
            {
                int newWardId = await _wardRepository.CreateWardAsync(wardDto, createdByUserId);
                return newWardId > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: Failed to create ward '{Name}' in Facility {FacilityId}.", wardDto.Name, wardDto.FacilityId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateWardAsync(CreateUpdateWardDto wardDto, int updatedByUserId)
        {
            if (!wardDto.WardId.HasValue || wardDto.WardId.Value <= 0 || wardDto.FacilityId <= 0)
            {
                return false;
            }

            var existingWard = await _wardRepository.GetWardByIdAsync(wardDto.WardId.Value);
            if (existingWard == null || existingWard.FacilityId != wardDto.FacilityId)
            {
                _logger.LogWarning("Service: Ward ID {WardId} not found or does not belong to Facility {FacilityId} for update.", wardDto.WardId, wardDto.FacilityId);
                return false;
            }

            if (existingWard.Name != wardDto.Name || existingWard.DepartmentId != wardDto.DepartmentId)
            {
                var duplicateNameCheck = await _wardRepository.GetWardByNameAndFacilityAsync(wardDto.FacilityId, wardDto.Name, wardDto.DepartmentId);
                if (duplicateNameCheck != null && duplicateNameCheck.WardId != wardDto.WardId.Value)
                {
                    _logger.LogWarning("Service: Another ward with name '{Name}' already exists in Facility {FacilityId} (Department {DepartmentId}) during update.", wardDto.Name, wardDto.FacilityId, wardDto.DepartmentId);
                    return false;
                }
            }

            if (wardDto.DepartmentId.HasValue)
            {
                var department = await _departmentRepository.GetDepartmentByIdAsync(wardDto.DepartmentId.Value);
                if (department == null || department.FacilityId != wardDto.FacilityId || !department.IsActive)
                {
                    _logger.LogWarning("Service: Target Department {DepartmentId} not found, inactive, or does not belong to Facility {FacilityId} for ward update.", wardDto.DepartmentId, wardDto.FacilityId);
                    return false;
                }
            }

            try
            {
                return await _wardRepository.UpdateWardAsync(wardDto, updatedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: Failed to update ward ID {Id} in Facility {FacilityId}.", wardDto.WardId, wardDto.FacilityId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeactivateWardAsync(int wardId, int deactivatedByUserId)
        {
            if (wardId <= 0)
            {
                return false;
            }

            var ward = await _wardRepository.GetWardByIdAsync(wardId);
            if (ward == null || !ward.IsActive)
            {
                _logger.LogWarning("Service: Ward ID {WardId} not found or already inactive for deactivation.", wardId);
                return false;
            }

            // Business rule: Check if ward has active patients/users before deactivating.
            // For now, simple deactivation.
            try
            {
                return await _wardRepository.DeactivateWardAsync(wardId, deactivatedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: Failed to deactivate ward ID {Id}.", wardId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<WardDto>> GetAllWardsForFacilityAndDepartmentAsync(int facilityId, int? departmentId = null)
        {
            if (facilityId <= 0)
            {
                _logger.LogWarning("Service: Invalid FacilityId for GetAllWardsForFacilityAndDepartmentAsync: {FacilityId}", facilityId);
                return Enumerable.Empty<WardDto>();
            }
            return await _wardRepository.GetAllActiveWardsByFacilityAsync(facilityId, departmentId);
        }
    }
}