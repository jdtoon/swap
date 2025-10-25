namespace carestream.core.enums
{
    public enum VisitStatus
    {
        // Initial state after check-in, awaiting vitals assessment
        WaitingForVitals,

        // Nurse is currently assessing vitals
        VitalsInProgress,

        // Vitals captured, patient is now in the doctor's queue
        ReadyForDoctor,

        // Doctor is currently with the patient
        ConsultationInProgress,

        // Patient in a general 'In Treatment' state (can be ongoing, awaiting labs, etc.)
        // A visit can be 'InTreatment' while also having a pending prescription.
        InTreatment,

        // Consultation/Treatment complete, patient has left the facility
        Discharged,

        // Visit was closed administratively, typically without full completion (e.g., patient left prematurely)
        AdministrativelyClosed,

        // A visit where prescriptions have been sent to the pharmacy and are pending dispense.
        // This is a *visit* status that overlaps with 'InTreatment' if a patient remains in the facility,
        // or can be a distinct 'waiting' state specific to pharmacy.
        // Given your clarification, a visit can be 'InTreatment' *and* have pending prescriptions.
        // So, this status should represent the state *after* doctor consult where pharmacy is the primary next step.
        // If a patient is still physically 'In Treatment' while waiting for pharmacy, this might be better
        // modeled by a flag on the visit or implicitly via prescription_items,
        // but since you have 'PendingPrescription' in seed data, I'll include it.
        PendingPrescription // This status indicates the visit is specifically at the pharmacy stage.
                            // If a patient can be "In Treatment" and also "Pending Prescription",
                            // you might need to adjust UI logic to show both (e.g., a patient in InTreatment might have a note "has pending prescription").
                            // For DB column purposes, it's one or the other. We'll use this if the *overall* visit state is 'waiting for pharmacy'.
    }
}