# CareStream EHR - Database Schema

Version: As of [Current Date]
Schema: `app` (unless specified as `public`)

This document outlines the structure of the PostgreSQL database used by the CareStream EHR application.

## 1. Schema Versioning Table

*   **Table:** `public.schema_versions`
    *   **Purpose:** Used by DbUp to track which migration scripts have been applied to the database.
    *   **Columns:**
        *   `schemaversionsid SERIAL PRIMARY KEY`: Auto-incrementing ID for the version record.
        *   `scriptname VARCHAR(255) NOT NULL`: The name of the executed migration script.
        *   `applied TIMESTAMP NOT NULL`: Timestamp of when the script was applied.

## 2. Application Schema: `app`

### 2.1. Table: `app.users`

*   **Purpose:** Stores information about system users (staff members like doctors, nurses, admins).
*   **Columns:**
    *   `user_id SERIAL PRIMARY KEY`: Unique auto-incrementing identifier for the user.
    *   `logto_sub VARCHAR(100) UNIQUE`: The unique subject identifier from Logto. Nullable until the user is linked.
    *   `force_number VARCHAR(50) UNIQUE`: The user's military Force Number or other unique service identifier.
    *   `rank VARCHAR(100)`: User's rank (e.g., "Pte", "Sgt", "Dr", "WO").
    *   `first_name VARCHAR(150) NOT NULL`: User's first name.
    *   `last_name VARCHAR(150) NOT NULL`: User's last name.
    *   `department VARCHAR(100)`: Department the user belongs to.
    *   `created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP`: Timestamp of user record creation.
    *   `is_active BOOLEAN NOT NULL DEFAULT TRUE`: Flag indicating if the user account is active.
*   **Indexes:**
    *   `idx_users_logto_sub ON users (logto_sub)`
    *   `idx_users_force_number ON users (force_number)`
    *   `users_pkey ON users (user_id)` (Implicitly created by `PRIMARY KEY`)
    *   `users_logto_sub_key ON users (logto_sub)` (Implicitly created by `UNIQUE`)
    *   `users_force_number_key ON users (force_number)` (Implicitly created by `UNIQUE`)

### 2.2. Table: `app.patients`

*   **Purpose:** Stores demographic and identifying information for patients.
*   **Columns:**
    *   `patient_id SERIAL PRIMARY KEY`: Unique auto-incrementing identifier for the patient.
    *   `force_number VARCHAR(50) UNIQUE`: Patient's Force Number (if applicable, could be same as user or a different system ID).
    *   `rank VARCHAR(100)`: Patient's rank.
    *   `first_name VARCHAR(150) NOT NULL`: Patient's first name.
    *   `last_name VARCHAR(150) NOT NULL`: Patient's last name.
    *   `date_of_birth DATE`: Patient's date of birth.
    *   `gender VARCHAR(50)`: Patient's gender.
    *   `created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP`: Timestamp of patient record creation.
    *   `user_id INT NULL REFERENCES app.users(user_id) ON DELETE SET NULL`: Optional link to the `app.users` table if the patient is also a system user/staff member.
*   **Indexes:**
    *   `idx_patients_force_number ON patients (force_number)`
    *   `patients_pkey ON patients (patient_id)` (Implicit)
    *   `patients_force_number_key ON patients (force_number)` (Implicit)

### 2.3. Table: `app.visits`

*   **Purpose:** Tracks individual patient visits or encounters with the health service.
*   **Columns:**
    *   `visit_id SERIAL PRIMARY KEY`: Unique auto-incrementing identifier for the visit.
    *   `patient_id INT NOT NULL REFERENCES app.patients(patient_id) ON DELETE CASCADE`: Foreign key to `app.patients`. If a patient is deleted, their visits are also deleted.
    *   `visit_timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP`: Timestamp of when the visit was initiated/recorded (e.g., check-in time).
    *   `brief_reason TEXT`: A brief reason for the visit provided at check-in.
    *   `status VARCHAR(50) NOT NULL`: Current status of the visit in the workflow (e.g., "Pending Checkin", "Waiting for Vitals", "Vitals In Progress", "Ready for Doctor", "In Treatment", "Discharged", "Cancelled", "Administratively Closed").
    *   `checked_in_by_user_id INT NULL REFERENCES app.users(user_id) ON DELETE SET NULL`: Foreign key to `app.users` for the staff member who performed the check-in.
    *   `assigned_officer_user_id INT NULL REFERENCES app.users(user_id) ON DELETE SET NULL`: Foreign key to `app.users` for the medical officer assigned to this visit (if any).
    *   `created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP`: Timestamp of visit record creation.
*   **Indexes:**
    *   `idx_visits_patient_id ON visits (patient_id)`
    *   `idx_visits_status_timestamp ON visits (status, visit_timestamp DESC)`
    *   `idx_visits_timestamp ON visits (visit_timestamp DESC)`
    *   `visits_pkey ON visits (visit_id)` (Implicit)

### 2.4. Table: `app.staff_reports`

*   **Purpose:** Stores reports submitted by staff members.
*   **Columns:**
    *   `report_id SERIAL PRIMARY KEY`: Unique auto-incrementing identifier for the report.
    *   `author_user_id INT NOT NULL REFERENCES app.users(user_id) ON DELETE RESTRICT`: Foreign key to `app.users` for the author. `ON DELETE RESTRICT` prevents deleting a user if they have authored reports.
    *   `title VARCHAR(255) NOT NULL`: Title of the report.
    *   `department VARCHAR(100)`: Department associated with the report (could be author's department).
    *   `priority VARCHAR(50)`: Priority of the report (e.g., "Low", "Medium", "High").
    *   `content TEXT`: Full content of the report.
    *   `created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP`: Timestamp of report creation.
*   **Indexes:**
    *   `idx_staff_reports_author_user_id ON staff_reports (author_user_id)`
    *   `idx_staff_reports_created_at ON staff_reports (created_at DESC)`
    *   `staff_reports_pkey ON staff_reports (report_id)` (Implicit)

### 2.5. Table: `app.vital_signs`

*   **Purpose:** Stores vital signs and urinalysis results recorded for a patient during a visit.
*   **Columns:**
    *   `vital_signs_id SERIAL PRIMARY KEY`: Unique auto-incrementing identifier for the vitals record.
    *   `visit_id INT NOT NULL REFERENCES app.visits(visit_id) ON DELETE CASCADE`: Foreign key to `app.visits`.
    *   `patient_id INT NOT NULL REFERENCES app.patients(patient_id) ON DELETE CASCADE`: Denormalized/Foreign key to `app.patients`.
    *   `blood_pressure_systolic INT NULL`
    *   `blood_pressure_diastolic INT NULL`
    *   `heart_rate INT NULL` (Beats per minute)
    *   `temperature DECIMAL(4,1) NULL` (e.g., 37.2)
    *   `respiratory_rate INT NULL` (Breaths per minute)
    *   `oxygen_saturation INT NULL` (SpO2 Percentage)
    *   `pain_level INT NULL` (0-10 scale)
    *   `urinalysis_color VARCHAR(50) NULL`
    *   `urinalysis_clarity VARCHAR(50) NULL`
    *   `urinalysis_specific_gravity DECIMAL(5,3) NULL` (e.g., 1.015)
    *   `urinalysis_ph DECIMAL(3,1) NULL` (e.g., 6.5)
    *   `urinalysis_protein VARCHAR(50) NULL`
    *   `urinalysis_glucose VARCHAR(50) NULL`
    *   `clinical_notes TEXT NULL`
    *   `requires_follow_up BOOLEAN NOT NULL DEFAULT FALSE`
    *   `mark_as_urgent BOOLEAN NOT NULL DEFAULT FALSE`
    *   `recorded_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP`: Timestamp when vitals were recorded.
    *   `recorded_by_user_id INT NULL REFERENCES app.users(user_id) ON DELETE SET NULL`: Nurse who recorded vitals.
*   **Constraints:**
    *   `uq_vitals_per_visit UNIQUE (visit_id)`: Ensures only one primary set of vitals is recorded per visit (can be removed if multiple readings per visit workflow step are needed in this table).
*   **Indexes:**
    *   `idx_vital_signs_visit_id ON vital_signs (visit_id)`
    *   `idx_vital_signs_patient_id ON vital_signs (patient_id)`
    *   `idx_vital_signs_recorded_at ON vital_signs (recorded_at DESC)`
    *   `vital_signs_pkey ON vital_signs (vital_signs_id)` (Implicit)
    *   `uq_vitals_per_visit ON vital_signs (visit_id)` (Implicit index for unique constraint)

## 3. Future Considerations / Planned Tables (Not yet in schema scripts)

*   Consultation Details
*   Prescriptions
*   Medications (Inventory/Master List)
*   Dispensation Records
*   Sick Notes
*   Referrals
*   More detailed User Profile/Contact/Emergency Contact tables (linked to `app.users` and `app.patients`).
*   Role and Permission mapping tables (if not solely relying on Logto claims for fine-grained app permissions).

---