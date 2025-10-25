using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using carestream.core.dtos.admin.facility;
using carestream.core.dtos.shared;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using carestream.core.dtos.facility;

namespace carestream.core.services
{
    /// <summary>
    /// Service for administrative management of Facilities.
    /// </summary>
    public class FacilityAdminService : IFacilityAdminService
    {
        private readonly IFacilityRepository _facilityRepository;
        private readonly ILogger<FacilityAdminService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityAdminService"/> class.
        /// </summary>
        /// <param name="facilityRepository">The repository for facility data.</param>
        /// <param name="logger">The logger for this service.</param>
        public FacilityAdminService(
            IFacilityRepository facilityRepository,
            ILogger<FacilityAdminService> logger)
        {
            _facilityRepository = facilityRepository ?? throw new ArgumentNullException(nameof(facilityRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<FacilityListViewModel> GetFacilitiesViewModelAsync(FilterAndPaginationOptions options)
        {
            var (facilities, totalCount) = await _facilityRepository.GetFacilitiesForAdminAsync(options);

            var viewModel = new FacilityListViewModel
            {
                Facilities = facilities.ToList(),
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
        public async Task<FacilityDto?> GetFacilityByIdAsync(int facilityId)
        {
            if (facilityId <= 0)
            {
                return null;
            }
            return await _facilityRepository.GetFacilityByIdAsync(facilityId);
        }

        /// <inheritdoc/>
        public async Task<bool> CreateFacilityAsync(CreateUpdateFacilityDto facilityDto, int createdByUserId)
        {
            var existingByName = await _facilityRepository.GetFacilityByNameAsync(facilityDto.Name);
            if (existingByName != null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(facilityDto.ShortCode))
            {
                var existingByShortCode = await _facilityRepository.GetFacilityByShortCodeAsync(facilityDto.ShortCode);
                if (existingByShortCode != null)
                {
                    return false;
                }
            }

            try
            {
                int newFacilityId = await _facilityRepository.CreateFacilityAsync(facilityDto, createdByUserId);
                return newFacilityId > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create facility '{Name}'.", facilityDto.Name);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateFacilityAsync(CreateUpdateFacilityDto facilityDto, int updatedByUserId)
        {
            if (!facilityDto.FacilityId.HasValue || facilityDto.FacilityId.Value <= 0)
            {
                return false;
            }

            var existingFacility = await _facilityRepository.GetFacilityByIdAsync(facilityDto.FacilityId.Value);
            if (existingFacility == null)
            {
                return false;
            }

            if (existingFacility.Name != facilityDto.Name)
            {
                var existingByName = await _facilityRepository.GetFacilityByNameAsync(facilityDto.Name);
                if (existingByName != null && existingByName.FacilityId != facilityDto.FacilityId.Value)
                {
                    return false;
                }
            }
            if (!string.IsNullOrEmpty(facilityDto.ShortCode) && existingFacility.ShortCode != facilityDto.ShortCode)
            {
                var existingByShortCode = await _facilityRepository.GetFacilityByShortCodeAsync(facilityDto.ShortCode);
                if (existingByShortCode != null && existingByShortCode.FacilityId != facilityDto.FacilityId.Value)
                {
                    return false;
                }
            }

            try
            {
                return await _facilityRepository.UpdateFacilityAsync(facilityDto, updatedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update facility ID {Id}.", facilityDto.FacilityId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeactivateFacilityAsync(int facilityId, int deactivatedByUserId)
        {
            if (facilityId <= 0)
            {
                return false;
            }

            var facility = await _facilityRepository.GetFacilityByIdAsync(facilityId);
            if (facility == null || !facility.IsActive)
            {
                return false;
            }

            try
            {
                return await _facilityRepository.DeactivateFacilityAsync(facilityId, deactivatedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate facility ID {Id}.", facilityId);
                return false;
            }
        }
    }
}