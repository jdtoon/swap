using carestream.core.interfaces.repositories;
using carestream.core.dtos.patient;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql; // For NpgsqlException if specific error handling needed
using System.Data;
using System;
using carestream.core.infrastructure; // Added for ICurrentFacilityContext
using System.Text; // For StringBuilder
using System.Linq; // For .Any(), Enumerable.Empty()
using carestream.core.dtos.shared; // For FilterAndPaginationOptions

namespace carestream.persistence.repositories
{
    public class PatientRepository : BaseRepository, IPatientRepository
    {
        public PatientRepository(IConfiguration configuration, ILogger<PatientRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<PatientDetailDto?> FindByForceNumberAsync(string forceNumber, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    patient_id AS PatientId,
                    force_number AS ForceNumber,
                    rank AS Rank,
                    first_name AS FirstName,
                    last_name AS LastName,
                    date_of_birth AS DateOfBirth,
                    gender AS Gender,
                    unit AS Unit,
                    email_address AS EmailAddress,
                    primary_phone_number AS PrimaryPhoneNumber,
                    emergency_contact_name AS EmergencyContactName,
                    emergency_contact_phone AS EmergencyContactPhone,
                    address_line1 AS AddressLine1,
                    address_line2 AS AddressLine2,
                    city AS City,
                    province AS Province,
                    postal_code AS PostalCode,
                    country AS Country,
                    next_of_kin_name AS NextOfKinName,
                    next_of_kin_phone AS NextOfKinPhone,
                    next_of_kin_relationship AS NextOfKinRelationship,
                    religion AS Religion,
                    occupation AS Occupation,
                    marital_status AS MaritalStatus,
                    nationality AS Nationality
                FROM app.patients
                WHERE force_number = @ForceNumberParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var patient = await conn.QueryFirstOrDefaultAsync<PatientDetailDto>(sql, new { ForceNumberParam = forceNumber }, transaction: trans);
                return patient;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<PatientBasicInfoDto?> GetPatientBasicInfoByIdAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    patient_id AS PatientId,
                    first_name AS FirstName,
                    last_name AS LastName,
                    rank AS Rank,
                    force_number AS ForceNumber,
                    date_of_birth AS DateOfBirth,
                    gender AS Gender,
                    unit AS Unit
                FROM app.patients
                WHERE patient_id = @PatientIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var patientInfo = await conn.QueryFirstOrDefaultAsync<PatientBasicInfoDto>(sql, new { PatientIdParam = patientId }, transaction: trans);
                return patientInfo;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdatePatientPersonalInfoAsync(EditPatientPersonalInfoDto patientInfo, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.patients
                SET
                    rank = @Rank,
                    first_name = @FirstName,
                    last_name = @LastName,
                    date_of_birth = @DateOfBirth,
                    gender = @Gender,
                    unit = @Unit,
                    updated_at = NOW()
                WHERE patient_id = @PatientId;";

            var parameters = new DynamicParameters(patientInfo);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<PatientDetailDto?> GetPatientDetailByIdAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    patient_id AS PatientId,
                    force_number AS ForceNumber,
                    rank AS Rank,
                    first_name AS FirstName,
                    last_name AS LastName,
                    date_of_birth AS DateOfBirth,
                    gender AS Gender,
                    unit AS Unit,
                    email_address AS EmailAddress,
                    primary_phone_number AS PrimaryPhoneNumber,
                    emergency_contact_name AS EmergencyContactName,
                    emergency_contact_phone AS EmergencyContactPhone,
                    address_line1 AS AddressLine1,
                    address_line2 AS AddressLine2,
                    city AS City,
                    province AS Province,
                    postal_code AS PostalCode,
                    country AS Country,
                    next_of_kin_name AS NextOfKinName,
                    next_of_kin_phone AS NextOfKinPhone,
                    next_of_kin_relationship AS NextOfKinRelationship,
                    religion AS Religion,
                    occupation AS Occupation,
                    marital_status AS MaritalStatus,
                    nationality AS Nationality
                FROM app.patients
                WHERE patient_id = @PatientIdParam;";
            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<PatientDetailDto>(sql, new { PatientIdParam = patientId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<EditPatientContactInfoDto?> GetPatientContactInfoForEditAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    patient_id AS PatientId,
                    email_address AS EmailAddress,
                    primary_phone_number AS PrimaryPhoneNumber,
                    address_line1 AS AddressLine1,
                    address_line2 AS AddressLine2,
                    city AS City,
                    province AS Province,
                    postal_code AS PostalCode,
                    country AS Country
                FROM app.patients
                WHERE patient_id = @PatientIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<EditPatientContactInfoDto>(sql, new { PatientIdParam = patientId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdatePatientContactInfoAsync(EditPatientContactInfoDto contactInfo, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.patients
                SET
                    email_address = @EmailAddress,
                    primary_phone_number = @PrimaryPhoneNumber,
                    updated_at = NOW()
                WHERE patient_id = @PatientId;";

            var parameters = new DynamicParameters(contactInfo);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<EditPatientEmergencyContactInfoDto?> GetPatientEmergencyContactInfoForEditAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    patient_id AS PatientId,
                    emergency_contact_name AS EmergencyContactName,
                    emergency_contact_phone AS EmergencyContactPhone,
                    next_of_kin_name AS NextOfKinName,
                    next_of_kin_phone AS NextOfKinPhone,
                    next_of_kin_relationship AS NextOfKinRelationship
                FROM app.patients
                WHERE patient_id = @PatientIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<EditPatientEmergencyContactInfoDto>(sql, new { PatientIdParam = patientId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdatePatientEmergencyContactInfoAsync(EditPatientEmergencyContactInfoDto emergencyContactInfo, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.patients
                SET
                    emergency_contact_name = @EmergencyContactName,
                    emergency_contact_phone = @EmergencyContactPhone,
                    updated_at = NOW()
                WHERE patient_id = @PatientId;";

            var parameters = new DynamicParameters(emergencyContactInfo);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<int> CreatePatientAsync(CreatePatientInputDto newPatientData, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.patients (
                    force_number, rank, first_name, last_name, date_of_birth, gender,
                    unit, email_address, primary_phone_number,
                    emergency_contact_name, emergency_contact_phone,
                    address_line1, address_line2, city, province, postal_code, country,
                    next_of_kin_name, next_of_kin_phone, next_of_kin_relationship,
                    religion, occupation, marital_status, nationality,
                    created_at, user_id
                ) VALUES (
                    @ForceNumber, @Rank, @FirstName, @LastName, @DateOfBirth, @Gender,
                    @Unit, @EmailAddress, @PrimaryPhoneNumber,
                    @EmergencyContactName, @EmergencyContactPhone,
                    @AddressLine1, @AddressLine2, @City, @Province, @PostalCode, @Country,
                    @NextOfKinName, @NextOfKinPhone, @NextOfKinRelationship,
                    @Religion, @Occupation, @MaritalStatus, @Nationality,
                    NOW(), @CreatedByUserId
                )
                RETURNING patient_id;";

            var parameters = new DynamicParameters(newPatientData);
            parameters.Add("CreatedByUserId", createdByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var newId = await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans);
                return newId;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<PatientBasicInfoDto> Items, int TotalCount)> GetAllPatientsAsync(FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.patients p
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(options.SearchTerm1))
            {
                whereClauses.Add("(p.first_name ILIKE @SearchPattern OR p.last_name ILIKE @SearchPattern OR p.force_number ILIKE @SearchPattern)");
                parameters.Add("SearchPattern", $"%{options.SearchTerm1}%");
            }

            if (whereClauses.Any())
            {
                baseSql.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(p.patient_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    p.patient_id AS PatientId,
                    p.first_name AS FirstName,
                    p.last_name AS LastName,
                    p.rank AS Rank,
                    p.force_number AS ForceNumber,
                    p.date_of_birth AS DateOfBirth,
                    p.gender AS Gender,
                    p.unit AS Unit
                {baseSql.ToString()}
                ORDER BY p.last_name, p.first_name
                LIMIT @PageSize OFFSET @Offset;
            ");
            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<PatientBasicInfoDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<PatientBasicInfoDto>(), totalCount);
            }, connection, transaction);
        }
    }
}