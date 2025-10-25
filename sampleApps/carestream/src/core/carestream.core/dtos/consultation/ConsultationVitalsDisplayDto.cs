using System;

namespace carestream.core.dtos.consultation // lowercase
{
    /// <summary>
    /// Data Transfer Object for displaying recorded vital signs
    /// within the consultation view.
    /// </summary>
    public class ConsultationVitalsDisplayDto
    {
        // Vitals (FR-VS-006)
        public int? BloodPressureSystolic { get; set; }
        public int? BloodPressureDiastolic { get; set; }
        public int? HeartRate { get; set; }                // Beats per minute
        public decimal? Temperature { get; set; }      // e.g., 37.2
        public int? RespiratoryRate { get; set; }          // Breaths per minute
        public int? OxygenSaturation { get; set; }         // Percentage (SpO2)
        public int? PainLevel { get; set; }                // 0-10 scale

        // Urinalysis (FR-VS-007)
        public string? UrinalysisColor { get; set; }
        public string? UrinalysisClarity { get; set; }
        public decimal? UrinalysisSpecificGravity { get; set; } // e.g., 1.015
        public decimal? UrinalysisPh { get; set; }               // e.g., 6.5
        public string? UrinalysisProtein { get; set; }           // e.g., "Negative", "Trace", "+", "++"
        public string? UrinalysisGlucose { get; set; }           // e.g., "Negative", "Trace", "+"

        // Notes & Flags from Vitals Capture (FR-VS-008, FR-VS-009)
        public string? ClinicalNotesFromVitals { get; set; }
        public bool RequiresFollowUp { get; set; }
        public bool MarkAsUrgent { get; set; }

        public DateTimeOffset? RecordedAt { get; set; }
        public string? RecordedByUserName { get; set; } // Name of the nurse who took vitals

        // Calculated string representations for easy display
        public string BloodPressureDisplay => (BloodPressureSystolic.HasValue && BloodPressureDiastolic.HasValue) ? $"{BloodPressureSystolic}/{BloodPressureDiastolic} mmHg" : "N/A";
        public string TemperatureDisplay => Temperature.HasValue ? $"{Temperature}°C" : "N/A"; // Assuming Celsius
        public string HeartRateDisplay => HeartRate.HasValue ? $"{HeartRate} bpm" : "N/A";
        public string RespiratoryRateDisplay => RespiratoryRate.HasValue ? $"{RespiratoryRate} breaths/min" : "N/A";
        public string OxygenSaturationDisplay => OxygenSaturation.HasValue ? $"{OxygenSaturation}%" : "N/A";
        public string PainLevelDisplay => PainLevel.HasValue ? $"{PainLevel}/10" : "N/A";
    }
}