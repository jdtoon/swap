using System.Collections.Generic;
using System.Linq;
using carestream.core.dtos.facility;

namespace carestream.core.infrastructure
{
    public class CurrentFacilityContext : ICurrentFacilityContext
    {
        private int _currentFacilityId;
        private string _currentFacilityName = string.Empty;
        private List<FacilityDto> _userAccessibleFacilities = new List<FacilityDto>();
        private bool _isFacilityContextSet = false; // Internal flag

        public int CurrentFacilityId
        {
            get => _currentFacilityId;
            set { _currentFacilityId = value; _isFacilityContextSet = true; } // Set flag when ID is set
        }

        public string CurrentFacilityName
        {
            get => _currentFacilityName;
            set => _currentFacilityName = value;
        }

        public bool IsFacilityContextSet => _isFacilityContextSet;

        public IEnumerable<FacilityDto> UserAccessibleFacilities
        {
            get => _userAccessibleFacilities;
            set => _userAccessibleFacilities = value?.ToList() ?? new List<FacilityDto>();
        }

        public void SetCurrentFacility(int facilityId, string facilityName, IEnumerable<FacilityDto> userAccessibleFacilities)
        {
            _currentFacilityId = facilityId;
            _currentFacilityName = facilityName;
            _userAccessibleFacilities = userAccessibleFacilities?.ToList() ?? new List<FacilityDto>();
            _isFacilityContextSet = true;
        }

        public void ClearContext()
        {
            _currentFacilityId = 0;
            _currentFacilityName = string.Empty;
            _userAccessibleFacilities.Clear();
            _isFacilityContextSet = false;
        }
    }
}