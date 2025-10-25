using System.Collections.Generic;

namespace carestream.core.dtos.pharmacy
{
    /// <summary>
    /// ViewModel for the detailed prescription view in the pharmacy module.
    /// </summary>
    public class ViewPrescriptionViewModel
    {
        public PrescriptionDetailHeaderDto Header { get; set; } = new PrescriptionDetailHeaderDto();
        public List<PrescriptionDetailItemDto> Items { get; set; } = new List<PrescriptionDetailItemDto>();
        // Add other properties if needed, e.g., for interaction buttons
    }
}