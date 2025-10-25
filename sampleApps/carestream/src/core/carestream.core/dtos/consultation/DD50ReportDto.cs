using System;
using System.Collections.Generic; // For List (if needed for complex aggregations)
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.consultation
{
    /// <summary>
    /// DTO for generating the DD50 Military Medical Examination Report.
    /// This DTO consolidates data from various tables.
    /// </summary>
    public class DD50ReportDto
    {
        // Patient Personal Details (from app.patients)
        public int PatientId { get; set; }
        public string? PatientForceNumber { get; set; }
        public string? PatientRank { get; set; }
        public string? PatientFirstName { get; set; }
        public string? PatientLastName { get; set; }
        public DateTime? PatientDateOfBirth { get; set; }
        public string? PatientGender { get; set; }
        public string? PatientUnit { get; set; } // From app.patients

        // Patient Medical History (concatenated strings from app.patient_medical_history)
        public string? Allergies { get; set; }
        public string? PreviousMedicalConditions { get; set; }

        // Vital Signs (from app.vital_signs - latest for the visit)
        public int? VitalsBloodPressureSystolic { get; set; }
        public int? VitalsBloodPressureDiastolic { get; set; }
        public int? VitalsHeartRate { get; set; }
        public decimal? VitalsTemperature { get; set; }
        public int? VitalsRespiratoryRate { get; set; }
        public int? VitalsOxygenSaturation { get; set; }
        public int? VitalsPainLevel { get; set; }

        // Visit Assessment (from app.visit_assessments - latest for the visit)
        public string? AssessmentPhysicalExamFindings { get; set; }
        public string? AssessmentCardiovascularNotes { get; set; }
        public string? AssessmentRespiratoryNotes { get; set; }
        public string? AssessmentMusculoskeletalNotes { get; set; }
        public string? AssessmentNeurologicalNotes { get; set; }
        public string? AssessmentPsychologicalNotes { get; set; }
        public string? AssessmentOtherSystemsNotes { get; set; }
        public string? AssessmentMedicalClassification { get; set; }
        public string? AssessmentDeploymentStatus { get; set; }
        public int? AssessmentValidityPeriodMonths { get; set; }
        public string? AssessmentRestrictions { get; set; }

        // Diagnoses (concatenated string from app.visit_diagnoses)
        public string? Diagnoses { get; set; }

        // Procedures (concatenated string from app.visit_procedures)
        public string? Procedures { get; set; }

        // Examining Officer Details (from app.users - typically the assigned officer for the visit)
        public string? ExaminingOfficerName { get; set; }
        public string? ExaminingOfficerRank { get; set; }
        public string? ExaminingOfficerDepartment { get; set; }

        public DateTimeOffset ReportGeneratedDate { get; set; } // Current date/time when the report is generated
    }
}