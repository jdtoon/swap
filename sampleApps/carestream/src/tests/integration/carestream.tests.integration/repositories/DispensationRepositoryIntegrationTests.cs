using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System;
using Dapper;
using carestream.persistence.repositories;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.pharmacy;
using carestream.core.dtos.shared;

namespace carestream.tests.integration.repositories
{
    public class DispensationRepositoryIntegrationTests : BaseIntegrationTest
    {
        private readonly IDispensationRepository _repository;
        private int _testUserId;
        private int _testPatientId;
        private int _testVisitId;
        private int _testMedicationId;
        private int _testPrescriptionItemId;
        private int _userPharmacist1Id, _userPharmacist2Id, _userDoctor1Id;
        private int _patient1Id, _patient2Id;
        private int _medication1Id, _medication2Id, _medication3Id;
        private int _visit1P1Id, _visit2P1Id, _visit1P2Id;
        private int _rxItem1V1M1Id, _rxItem2V1M2Id, _rxItem3V2M1Id;

        public DispensationRepositoryIntegrationTests(DatabaseTestFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _repository = new DispensationRepository(Configuration, GetMockLogger<DispensationRepository>(), GetCurrentFacilityContext());
            SetupTestDataAsync().GetAwaiter().GetResult();
            SeedComprehensiveTestDataAsync().GetAwaiter().GetResult();
        }

        private async Task SeedComprehensiveTestDataAsync()
        {
            // Users
            _userPharmacist1Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.users (force_number, first_name, last_name, rank) VALUES (@FN, 'Pharma', 'One', 'CPL') RETURNING user_id;", new { FN = $"U_DH_PH1_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);
            _userPharmacist2Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.users (force_number, first_name, last_name, rank) VALUES (@FN, 'Pharma', 'Two', 'SGT') RETURNING user_id;", new { FN = $"U_DH_PH2_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);
            _userDoctor1Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.users (force_number, first_name, last_name, rank) VALUES (@FN, 'Doc', 'Primary', 'MAJ') RETURNING user_id;", new { FN = $"U_DH_DR1_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);

            // Patients
            _patient1Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.patients (force_number, first_name, last_name) VALUES (@FN, 'Patient', 'Alpha') RETURNING patient_id;", new { FN = $"P_DH_PA_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);
            _patient2Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.patients (force_number, first_name, last_name) VALUES (@FN, 'Patient', 'Beta') RETURNING patient_id;", new { FN = $"P_DH_PB_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);

            // Medications
            _medication1Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.medications (name, strength, form, category) VALUES ('DispHistMedA', '10mg', 'Tab', 'Painkiller') ON CONFLICT (name, strength, form) DO NOTHING RETURNING medication_id;", transaction: Transaction);
            if (_medication1Id == 0) _medication1Id = await Connection.ExecuteScalarAsync<int>("SELECT medication_id FROM app.medications WHERE name='DispHistMedA'", transaction: Transaction);
            _medication2Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.medications (name, strength, form, category) VALUES ('DispHistMedB', '20mg', 'Cap', 'Antibiotic') ON CONFLICT (name, strength, form) DO NOTHING RETURNING medication_id;", transaction: Transaction);
            if (_medication2Id == 0) _medication2Id = await Connection.ExecuteScalarAsync<int>("SELECT medication_id FROM app.medications WHERE name='DispHistMedB'", transaction: Transaction);
            _medication3Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.medications (name, strength, form, category) VALUES ('DispHistMedC', '5ml', 'Syrup', 'Painkiller') ON CONFLICT (name, strength, form) DO NOTHING RETURNING medication_id;", transaction: Transaction);
            if (_medication3Id == 0) _medication3Id = await Connection.ExecuteScalarAsync<int>("SELECT medication_id FROM app.medications WHERE name='DispHistMedC'", transaction: Transaction);


            // Visits
            _visit1P1Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PId, 'Discharged', @UId, @UId, @Ts) RETURNING visit_id;", new { PId = _patient1Id, UId = _userDoctor1Id, Ts = DateTime.UtcNow.AddDays(-3) }, transaction: Transaction);
            _visit2P1Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PId, 'Discharged', @UId, @UId, @Ts) RETURNING visit_id;", new { PId = _patient1Id, UId = _userDoctor1Id, Ts = DateTime.UtcNow.AddDays(-1) }, transaction: Transaction);
            _visit1P2Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PId, 'Discharged', @UId, @UId, @Ts) RETURNING visit_id;", new { PId = _patient2Id, UId = _userDoctor1Id, Ts = DateTime.UtcNow.AddDays(-2) }, transaction: Transaction);

            // Prescription Items (linked to visits)
            _rxItem1V1M1Id = await SeedPrescriptionItemAsync(_visit1P1Id, _medication1Id, "7 tablets");
            _rxItem2V1M2Id = await SeedPrescriptionItemAsync(_visit1P1Id, _medication2Id, "1 bottle");
            _rxItem3V2M1Id = await SeedPrescriptionItemAsync(_visit2P1Id, _medication1Id, "14 tablets"); // Patient Alpha, second visit
            await SeedPrescriptionItemAsync(_visit1P2Id, _medication3Id, "100 ml"); // Patient Beta

            // Dispensation Log Items
            await SeedDispenseLogAsync(_rxItem1V1M1Id, _visit1P1Id, _medication1Id, "7 tablets", _userPharmacist1Id, DateTime.UtcNow.AddDays(-3).AddHours(1));
            await SeedDispenseLogAsync(_rxItem2V1M2Id, _visit1P1Id, _medication2Id, "1 bottle", _userPharmacist1Id, DateTime.UtcNow.AddDays(-3).AddHours(2));
            await SeedDispenseLogAsync(_rxItem3V2M1Id, _visit2P1Id, _medication1Id, "7 tablets", _userPharmacist2Id, DateTime.UtcNow.AddDays(-1).AddHours(1), "Partial dispense"); // Partial
            await SeedDispenseLogAsync(_rxItem3V2M1Id, _visit2P1Id, _medication1Id, "7 tablets", _userPharmacist1Id, DateTime.UtcNow.AddDays(-1).AddHours(3), "Remaining part"); // Completion
            Fixture.Output?.WriteLine($"Seeded Dispensation Data");
        }

        private async Task<int> SeedPrescriptionItemAsync(int visitId, int medicationId, string qtyPrescribed)
        {
            return await Connection.ExecuteScalarAsync<int>(@"
                INSERT INTO app.prescription_items (visit_id, medication_id, dosage, frequency, quantity_prescribed, created_by_user_id, is_sent_to_pharmacy, pharmacy_sent_at, is_fully_dispensed)
                VALUES (@VisitId, @MedId, '1', 'OD', @QtyP, @UserId, TRUE, NOW() - interval '1 hour', TRUE) RETURNING prescription_item_id;", // Assume fully dispensed for history
                new { VisitId = visitId, MedId = medicationId, QtyP = qtyPrescribed, UserId = _userDoctor1Id }, transaction: Transaction);
        }
        private async Task SeedDispenseLogAsync(int rxItemId, int visitId, int medId, string qtyDisp, int pharmId, DateTime dispensedAt, string? notes = null)
        {
            await Connection.ExecuteAsync(@"
                INSERT INTO app.dispensation_log_items (prescription_item_id, visit_id, medication_id, quantity_dispensed_transaction, dispensed_by_user_id, dispensed_at, pharmacist_notes)
                VALUES (@RxItemId, @VisitId, @MedId, @QtyD, @PharmId, @DispAt, @Notes);",
               new { RxItemId = rxItemId, VisitId = visitId, MedId = medId, QtyD = qtyDisp, PharmId = pharmId, DispAt = dispensedAt, Notes = notes }, transaction: Transaction);
        }

        private async Task SetupTestDataAsync()
        {
            _testUserId = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.users (force_number, first_name, last_name) VALUES (@FN, 'DispTest', 'User') RETURNING user_id;",
                new { FN = $"U_DISPTEST_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);

            _testPatientId = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.patients (force_number, first_name, last_name) VALUES (@FN, 'DispTest', 'Patient') RETURNING patient_id;",
                new { FN = $"P_DISPTEST_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);

            _testVisitId = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.visits (patient_id, status, checked_in_by_user_id) VALUES (@PatientId, 'SentToPharmacy', @UserId) RETURNING visit_id;",
                new { PatientId = _testPatientId, UserId = _testUserId }, transaction: Transaction);

            _testMedicationId = await Connection.ExecuteScalarAsync<int>(
                 "INSERT INTO app.medications (name, strength, form, category) VALUES ('DispMed', '10mg', 'Tab', 'Test') ON CONFLICT (name, strength, form) DO NOTHING RETURNING medication_id;", transaction: Transaction);
            if (_testMedicationId == 0) _testMedicationId = await Connection.ExecuteScalarAsync<int>("SELECT medication_id FROM app.medications WHERE name='DispMed'", transaction: Transaction);


            _testPrescriptionItemId = await Connection.ExecuteScalarAsync<int>(@"
                INSERT INTO app.prescription_items
                    (visit_id, medication_id, dosage, frequency, quantity_prescribed, created_by_user_id, is_sent_to_pharmacy, pharmacy_sent_at)
                VALUES
                    (@VisitId, @MedicationId, '1 tab', 'OD', '7', @CreatedByUserId, TRUE, NOW())
                RETURNING prescription_item_id;",
                new { VisitId = _testVisitId, MedicationId = _testMedicationId, CreatedByUserId = _testUserId },
                transaction: Transaction);
            Fixture.Output?.WriteLine($"Setup Dispensation Tests: VisitId={_testVisitId}, PrescriptionItemId={_testPrescriptionItemId}");
        }

        [Fact]
        public async Task LogDispenseActionAsync_ShouldInsertLogEntryAndReturnId()
        {
            // Arrange
            var logEntry = new DispenseLogEntryInputDto
            {
                PrescriptionItemId = _testPrescriptionItemId,
                VisitId = _testVisitId,
                MedicationId = _testMedicationId,
                QuantityDispensedInTransaction = "7 tablets",
                DispensedByUserId = _testUserId,
                PharmacistNotes = "Patient counselled.",
                BatchNumber = "B123",
                ExpiryDate = DateTime.UtcNow.Date.AddYears(1)
            };

            // Act
            int newLogId = await _repository.LogDispenseActionAsync(logEntry, Connection, Transaction);

            // Assert
            Assert.True(newLogId > 0);

            var dbLogEntry = await Connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM app.dispensation_log_items WHERE dispensation_log_item_id = @Id",
                new { Id = newLogId }, transaction: Transaction);

            Assert.NotNull(dbLogEntry);
            Assert.Equal(_testPrescriptionItemId, (int)dbLogEntry.prescription_item_id);
            Assert.Equal(logEntry.QuantityDispensedInTransaction, (string)dbLogEntry.quantity_dispensed_transaction);
            Assert.Equal(_testUserId, (int)dbLogEntry.dispensed_by_user_id);
            Assert.Equal(logEntry.PharmacistNotes, (string)dbLogEntry.pharmacist_notes);
            Assert.Equal(logEntry.BatchNumber, (string)dbLogEntry.batch_number);
            Assert.Equal(logEntry.ExpiryDate, (DateTime?)dbLogEntry.expiry_date);
        }

        [Fact]
        public async Task GetDispensedHistoryAsync_NoFilters_ShouldReturnAllDispensedItems_PaginatedAndOrdered()
        {
            // Arrange
            var options = new FilterAndPaginationOptions { PageNumber = 1, PageSize = 2 };

            // Act
            var (items, totalCount) = await _repository.GetDispensedHistoryAsync(options, Connection, Transaction);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(4, totalCount); // We seeded 4 dispense log entries
            Assert.Equal(2, items.Count());
            Assert.Equal(_rxItem3V2M1Id, items.ElementAt(0).PrescriptionItemId); // Most recent dispense (completion part)
            Assert.Equal("7 tablets", items.ElementAt(0).QuantityDispensedInTransaction);
            Assert.Contains("Pharma One", items.ElementAt(0).PharmacistName);
        }

        [Fact]
        public async Task GetDispensedHistoryAsync_FilterByPatientName_ShouldReturnMatchingItems()
        {
            // Arrange: Patient Alpha has 3 dispense log items (2 for visit1, 1 for visit2 for rxItem3's first part)
            var options = new FilterAndPaginationOptions { SearchTerm1 = "Alpha" }; // Patient Alpha

            // Act
            var (items, totalCount) = await _repository.GetDispensedHistoryAsync(options, Connection, Transaction);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(4, totalCount); // 2 from visit1P1, 1 from visit2P1 (the first dispense of rxItem3)
            Assert.All(items, item => Assert.Contains("Alpha", item.PatientName));
        }

        [Fact]
        public async Task GetDispensedHistoryAsync_FilterByMedicationName_ShouldReturnMatchingItems()
        {
            // Arrange: DispHistMedA was dispensed for visit1P1 (rxItem1) and visit2P1 (rxItem3)
            var options = new FilterAndPaginationOptions { SearchTerm2 = "DispHistMedA" };

            // Act
            var (items, totalCount) = await _repository.GetDispensedHistoryAsync(options, Connection, Transaction);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(3, totalCount); // rxItem1V1M1, and two logs for rxItem3V2M1
            Assert.All(items, item => Assert.Contains("DispHistMedA", item.MedicationName));
        }

        [Fact]
        public async Task GetDispensedHistoryAsync_FilterByDateRange_ShouldReturnMatchingItems()
        {
            // Arrange: Seeded with specific dates
            var options = new FilterAndPaginationOptions
            {
                StartDate = DateTime.UtcNow.Date.AddDays(-1), // Today and Yesterday's
                EndDate = DateTime.UtcNow.Date // Up to end of today
            };
            // This should catch the two dispensations for _rxItem3V2M1Id which happened on "yesterday"

            // Act
            var (items, totalCount) = await _repository.GetDispensedHistoryAsync(options, Connection, Transaction);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(2, totalCount); // The two log entries for rxItem3V2M1
            Assert.All(items, item => Assert.Equal(_rxItem3V2M1Id, item.PrescriptionItemId));
        }

        [Fact]
        public async Task GetDispensedHistoryAsync_NoMatchingFilters_ShouldReturnEmptyItemsAndZeroTotal()
        {
            // Arrange
            var options = new FilterAndPaginationOptions { SearchTerm1 = "NonExistentPatientName" };

            // Act
            var (items, totalCount) = await _repository.GetDispensedHistoryAsync(options, Connection, Transaction);

            // Assert
            Assert.NotNull(items);
            Assert.Empty(items);
            Assert.Equal(0, totalCount);
        }
    }
}