namespace carestream.core.dtos.prescription
{
    /// <summary>
    /// DTO to hold current dispense information for a prescription item.
    /// </summary>
    public class PrescriptionItemDispenseInfoDto
    {
        public string QuantityPrescribed { get; set; } = string.Empty;
        public string? QuantityDispensedSoFar { get; set; } // Current total from DB
        public bool IsAlreadyFullyDispensed { get; set; }
    }
}