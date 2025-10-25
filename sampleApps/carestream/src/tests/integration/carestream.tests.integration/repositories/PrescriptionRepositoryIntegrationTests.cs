using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using carestream.persistence.repositories;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.prescription;
using Dapper;
using System;

namespace carestream.tests.integration.repositories
{
    public class PrescriptionRepositoryIntegrationTests : BaseIntegrationTest
    {
        private readonly IPrescriptionRepository _repository;
        private int _testUserIdDoctor;
        private int _testUserIdPharmacist; // If needed for 'sent_by_user_id'
        private int _testPatientId1;
        private int _testPatientId2;
        private int _testVisitId1;
        private int _testVisitId2;
        private int _testMedicationId1;
        private int _testMedicationId2;

        public PrescriptionRepositoryIntegrationTests(DatabaseTestFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _repository = new PrescriptionRepository(Configuration, GetMockLogger<PrescriptionRepository>(), GetCurrentFacilityContext());
            SetupInitialTestDataAsync().GetAwaiter().GetResult(); // Renamed for clarity
        }

        private async Task SetupInitialTestDataAsync()
        {
            _testUserIdDoctor = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.users (force_number, first_name, last_name, rank, department) VALUES (@FN, 'Test', 'Doctor', 'Dr', 'Clinic') RETURNING user_id;",
                new { FN = $"U_RXTEST_DOC_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);

            _testUserIdPharmacist = await Connection.ExecuteScalarAsync<int>( // For marking prescriptions as sent
                "INSERT INTO app.users (force_number, first_name, last_name, rank, department) VALUES (@FN, 'Test', 'Pharma', 'Pharm', 'Pharmacy') RETURNING user_id;",
                new { FN = $"U_RXTEST_PHARM_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);


            _testPatientId1 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.patients (force_number, first_name, last_name, rank) VALUES (@FN, 'John', 'Doe', 'Pte') RETURNING patient_id;",
                new { FN = $"P_RXTEST_JD_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);
            _testPatientId2 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.patients (force_number, first_name, last_name, rank) VALUES (@FN, 'Jane', 'Smith', 'Sgt') RETURNING patient_id;",
                new { FN = $"P_RXTEST_JS_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);


            _testVisitId1 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PatientId, 'Discharged', @UserId, @UserId, NOW() - INTERVAL '1 day') RETURNING visit_id;",
                new { PatientId = _testPatientId1, UserId = _testUserIdDoctor }, transaction: Transaction);
            _testVisitId2 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PatientId, 'Discharged', @UserId, @UserId, NOW() - INTERVAL '2 day') RETURNING visit_id;",
                new { PatientId = _testPatientId2, UserId = _testUserIdDoctor }, transaction: Transaction);


            _testMedicationId1 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.medications (name, strength, form, category) VALUES ('MedA', '10mg', 'Tab', 'CatA') ON CONFLICT (name, strength, form) DO NOTHING RETURNING medication_id;", transaction: Transaction);
            if (_testMedicationId1 == 0) _testMedicationId1 = await Connection.ExecuteScalarAsync<int>("SELECT medication_id FROM app.medications WHERE name='MedA'", transaction: Transaction);

            _testMedicationId2 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.medications (name, strength, form, category) VALUES ('MedB', '20mg', 'Cap', 'CatB') ON CONFLICT (name, strength, form) DO NOTHING RETURNING medication_id;", transaction: Transaction);
            if (_testMedicationId2 == 0) _testMedicationId2 = await Connection.ExecuteScalarAsync<int>("SELECT medication_id FROM app.medications WHERE name='MedB'", transaction: Transaction);

            Fixture.Output?.WriteLine($"Setup Rx Tests: Visit1={_testVisitId1}, Visit2={_testVisitId2}, Med1={_testMedicationId1}, Med2={_testMedicationId2}");
        }


        private async Task SeedPrescriptionItemAsync(int visitId, int medicationId, bool isSentToPharmacy, DateTime? pharmacySentAt = null)
        {
            const string sql = @"
                INSERT INTO app.prescription_items
                    (visit_id, medication_id, dosage, frequency, duration, quantity_prescribed, created_by_user_id, is_sent_to_pharmacy, pharmacy_sent_at)
                VALUES
                    (@VisitId, @MedicationId, '1 tab', 'OD', '7 days', '7', @CreatedByUserId, @IsSentToPharmacy, @PharmacySentAt);";
            await Connection.ExecuteAsync(sql, new
            {
                VisitId = visitId,
                MedicationId = medicationId,
                CreatedByUserId = _testUserIdDoctor,
                IsSentToPharmacy = isSentToPharmacy,
                PharmacySentAt = isSentToPharmacy ? (pharmacySentAt ?? DateTime.UtcNow) : (DateTime?)null
            }, transaction: Transaction);
        }

        private async Task<AddPrescriptionItemInputDto> CreateSampleAddItemDto(int medicationId)
        {
            return new AddPrescriptionItemInputDto
            {
                VisitId = _testVisitId1,
                MedicationId = medicationId,
                Dosage = "1 tablet",
                Frequency = "Once daily",
                Duration = "7 days",
                QuantityPrescribed = "7 tablets",
                SpecialInstructions = "Take with food"
            };
        }

        [Fact]
        public async Task AddPrescriptionItemAsync_ShouldInsertAndReturnItemDetails()
        {
            // Arrange
            var inputDto = await CreateSampleAddItemDto(_testMedicationId1);

            // Act
            var result = await _repository.AddPrescriptionItemAsync(inputDto, _testUserIdDoctor, Connection, Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.PrescriptionItemId > 0);
            Assert.Equal(inputDto.MedicationId, result.MedicationId);
            Assert.Equal(inputDto.Dosage, result.Dosage);
            Assert.Contains("MedA 10mg Tab", result.MedicationName); // Check if name got populated

            var dbItem = await Connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM app.prescription_items WHERE prescription_item_id = @Id",
                new { Id = result.PrescriptionItemId }, transaction: Transaction);
            Assert.NotNull(dbItem);
            Assert.Equal(inputDto.Frequency, dbItem.frequency);
        }

        [Fact]
        public async Task GetPrescriptionItemsForVisitAsync_ShouldReturnAddedItems()
        {
            // Arrange
            var input1 = await CreateSampleAddItemDto(_testMedicationId1);
            var input2 = await CreateSampleAddItemDto(_testMedicationId2);
            await _repository.AddPrescriptionItemAsync(input1, _testUserIdDoctor, Connection, Transaction);
            await _repository.AddPrescriptionItemAsync(input2, _testUserIdDoctor, Connection, Transaction);

            // Act
            var results = (await _repository.GetPrescriptionItemsForVisitAsync(_testVisitId1, Connection, Transaction)).ToList();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Contains(results, item => item.MedicationId == _testMedicationId1);
            Assert.Contains(results, item => item.MedicationId == _testMedicationId2 && item.MedicationName.Contains("MedB 20mg Cap"));
        }

        [Fact]
        public async Task RemovePrescriptionItemAsync_ShouldRemoveItem_IfNotSentToPharmacy()
        {
            // Arrange
            var input = await CreateSampleAddItemDto(_testMedicationId1);
            var addedItem = await _repository.AddPrescriptionItemAsync(input, _testUserIdDoctor, Connection, Transaction);
            Assert.NotNull(addedItem);

            // Act
            bool removed = await _repository.RemovePrescriptionItemAsync(addedItem.PrescriptionItemId, Connection, Transaction);

            // Assert
            Assert.True(removed);
            var itemsAfterRemove = (await _repository.GetPrescriptionItemsForVisitAsync(_testVisitId1, Connection, Transaction)).ToList();
            Assert.DoesNotContain(itemsAfterRemove, item => item.PrescriptionItemId == addedItem.PrescriptionItemId);
        }

        [Fact]
        public async Task RemovePrescriptionItemAsync_ShouldNotRemoveItem_IfAlreadySentToPharmacy()
        {
            // Arrange
            var input = await CreateSampleAddItemDto(_testMedicationId1);
            var addedItem = await _repository.AddPrescriptionItemAsync(input, _testUserIdDoctor, Connection, Transaction);
            Assert.NotNull(addedItem);
            // Manually mark as sent for testing this scenario
            await Connection.ExecuteAsync(
                "UPDATE app.prescription_items SET is_sent_to_pharmacy = TRUE WHERE prescription_item_id = @Id",
                new { Id = addedItem.PrescriptionItemId }, transaction: Transaction);

            // Act
            bool removed = await _repository.RemovePrescriptionItemAsync(addedItem.PrescriptionItemId, Connection, Transaction);

            // Assert
            Assert.False(removed); // Should not remove because it's marked as sent (repo logic prevents it)

            // Verify it's still in the DB (though GetPrescriptionItemsForVisitAsync filters out sent items)
            var dbItem = await Connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT is_sent_to_pharmacy FROM app.prescription_items WHERE prescription_item_id = @Id",
                new { Id = addedItem.PrescriptionItemId }, transaction: Transaction);
            Assert.NotNull(dbItem);
            Assert.True((bool)dbItem.is_sent_to_pharmacy);
        }

        [Fact]
        public async Task SendPrescriptionToPharmacyAsync_ShouldMarkItemsAsSent()
        {
            // Arrange
            var input1 = await CreateSampleAddItemDto(_testMedicationId1);
            var input2 = await CreateSampleAddItemDto(_testMedicationId2);
            var item1 = await _repository.AddPrescriptionItemAsync(input1, _testUserIdDoctor, Connection, Transaction);
            var item2 = await _repository.AddPrescriptionItemAsync(input2, _testUserIdDoctor, Connection, Transaction);
            Assert.NotNull(item1); Assert.NotNull(item2);

            // Act
            bool result = await _repository.SendPrescriptionToPharmacyAsync(_testVisitId1, _testUserIdDoctor, Connection, Transaction);

            // Assert
            Assert.True(result); // Or check affected rows if repo returns that
            var itemsAfterSend = await Connection.QueryAsync<dynamic>(
                "SELECT is_sent_to_pharmacy, pharmacy_sent_at FROM app.prescription_items WHERE visit_id = @VisitId",
                new { VisitId = _testVisitId1 }, transaction: Transaction);

            Assert.NotEmpty(itemsAfterSend);
            Assert.All(itemsAfterSend, item => Assert.True((bool)item.is_sent_to_pharmacy));
            Assert.All(itemsAfterSend, item => Assert.NotNull(item.pharmacy_sent_at));
        }

        [Fact]
        public async Task SendPrescriptionToPharmacyAsync_ShouldDoNothing_IfNoUnsentItems()
        {
            // Arrange: Visit exists, but no prescription items, or all are already sent.
            // For this test, let's ensure no unsent items.
            // (SetupTestDataAsync creates _testVisitId. We won't add items to it)

            // Act
            bool result = await _repository.SendPrescriptionToPharmacyAsync(_testVisitId1, _testUserIdDoctor, Connection, Transaction);

            // Assert
            // The repository method currently returns true if affectedRows >= 0.
            // So, if no rows are updated (because none were pending), it still returns true.
            Assert.True(result);
        }

        [Fact]
        public async Task GetPendingPrescriptionsSummaryAsync_ShouldReturnCorrectSummaries()
        {
            // Arrange
            // Visit 1: 2 items sent to pharmacy
            await SeedPrescriptionItemAsync(_testVisitId1, _testMedicationId1, true, DateTime.UtcNow.AddHours(-2));
            await SeedPrescriptionItemAsync(_testVisitId1, _testMedicationId2, true, DateTime.UtcNow.AddHours(-2));
            // Visit 2: 1 item sent to pharmacy
            await SeedPrescriptionItemAsync(_testVisitId2, _testMedicationId1, true, DateTime.UtcNow.AddHours(-1));
            // Visit 2: 1 item NOT sent to pharmacy (should not appear)
            await SeedPrescriptionItemAsync(_testVisitId2, _testMedicationId2, false);

            // Act
            var results = (await _repository.GetPendingPrescriptionsSummaryAsync(limit: 10, offset: 0, connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count); // Two distinct visits with items sent to pharmacy

            var visit1Summary = results.FirstOrDefault(r => r.VisitId == _testVisitId1);
            var visit2Summary = results.FirstOrDefault(r => r.VisitId == _testVisitId2);

            Assert.NotNull(visit1Summary);
            Assert.Equal(_testPatientId1, visit1Summary.PatientId);
            Assert.Contains("John Doe", visit1Summary.PatientName);
            Assert.Equal(2, visit1Summary.NumberOfMedications);
            Assert.Contains("Test Doctor", visit1Summary.PrescribingDoctorName); // Based on user_id used in SeedPrescriptionItemAsync
            Assert.Equal("Pending Dispense", visit1Summary.Status);

            Assert.NotNull(visit2Summary);
            Assert.Equal(_testPatientId2, visit2Summary.PatientId);
            Assert.Contains("Jane Smith", visit2Summary.PatientName);
            Assert.Equal(1, visit2Summary.NumberOfMedications); // Only one item was sent
        }

        [Fact]
        public async Task GetPendingPrescriptionsSummaryAsync_ShouldHandlePagination()
        {
            // Arrange: Seed more than one page worth of data
            // Create 3 visits, each with 1 item sent to pharmacy
            var visit3 = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PatientId, 'Discharged', @UserId, @UserId, NOW() - INTERVAL '3 day') RETURNING visit_id;", new { PatientId = _testPatientId1, UserId = _testUserIdDoctor }, transaction: Transaction);

            await SeedPrescriptionItemAsync(_testVisitId1, _testMedicationId1, true, DateTime.UtcNow.AddHours(-3));
            await SeedPrescriptionItemAsync(_testVisitId2, _testMedicationId1, true, DateTime.UtcNow.AddHours(-2));
            await SeedPrescriptionItemAsync(visit3, _testMedicationId2, true, DateTime.UtcNow.AddHours(-1)); // Newest

            // Act: Get page 1, size 2
            var page1Results = (await _repository.GetPendingPrescriptionsSummaryAsync(limit: 2, offset: 0, connection: Connection, transaction: Transaction)).ToList();
            // Act: Get page 2, size 2
            var page2Results = (await _repository.GetPendingPrescriptionsSummaryAsync(limit: 2, offset: 2, connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.Equal(2, page1Results.Count);
            Assert.Equal(_testVisitId1, page1Results[0].VisitId); // Oldest sent
            Assert.Equal(_testVisitId2, page1Results[1].VisitId);

            Assert.Single(page2Results);
            Assert.Equal(visit3, page2Results[0].VisitId); // Newest sent
        }


        [Fact]
        public async Task GetPharmacistDashboardStatsAsync_ShouldReturnCorrectCounts()
        {
            // Arrange
            // Visit 1: 2 items sent to pharmacy
            await SeedPrescriptionItemAsync(_testVisitId1, _testMedicationId1, true);
            await SeedPrescriptionItemAsync(_testVisitId1, _testMedicationId2, true);
            // Visit 2: 1 item sent to pharmacy
            await SeedPrescriptionItemAsync(_testVisitId2, _testMedicationId1, true);
            // These items affect PendingPrescriptionsCount. Other stats are currently placeholders.

            // Act
            var stats = await _repository.GetPharmacistDashboardStatsAsync(connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(2, stats.PendingPrescriptionsCount); // 2 distinct visits with items sent
            // Assert placeholder values for other stats as per current repo implementation
            Assert.Equal(0, stats.PatientsWaitingCollection);
            Assert.Equal(0, stats.DispensedTodayCount);
            Assert.Equal("12m", stats.AveragePreparationTime);
        }

        [Fact]
        public async Task GetPharmacistDashboardStatsAsync_ShouldReturnZeros_WhenNoPendingPrescriptions()
        {
            // Arrange: No items sent to pharmacy for _testVisitId1 or _testVisitId2 in this test's transaction
            await SeedPrescriptionItemAsync(_testVisitId1, _testMedicationId1, false); // Not sent

            // Act
            var stats = await _repository.GetPharmacistDashboardStatsAsync(connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(0, stats.PendingPrescriptionsCount);
        }

        [Fact]
        public async Task GetPrescriptionDetailItemsAsync_ShouldReturnCorrectItemsForVisit()
        {
            // Arrange: Ensure _testVisitId1 has specific prescription items seeded as sent to pharmacy
            // For example, seed 2 items for _testVisitId1
            await Connection.ExecuteAsync(
                @"INSERT INTO app.prescription_items (visit_id, medication_id, dosage, frequency, quantity_prescribed, created_by_user_id, is_sent_to_pharmacy, pharmacy_sent_at)
          VALUES (@VisitId, @MedId, '1 tab', 'OD', '7', @UserId, TRUE, NOW())",
                new { VisitId = _testVisitId1, MedId = _testMedicationId1, UserId = _testUserIdDoctor }, transaction: Transaction);
            await Connection.ExecuteAsync(
                @"INSERT INTO app.prescription_items (visit_id, medication_id, dosage, frequency, quantity_prescribed, created_by_user_id, is_sent_to_pharmacy, pharmacy_sent_at)
          VALUES (@VisitId, @MedId, '2 caps', 'BD', '14', @UserId, TRUE, NOW())",
                new { VisitId = _testVisitId1, MedId = _testMedicationId2, UserId = _testUserIdDoctor }, transaction: Transaction);


            // Act
            var results = (await _repository.GetPrescriptionDetailItemsAsync(_testVisitId1, Connection, Transaction)).ToList();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count); // Expecting the 2 items seeded above
            Assert.Contains(results, item => item.MedicationId == _testMedicationId1 && item.Dosage == "1 tab");
            Assert.Contains(results, item => item.MedicationId == _testMedicationId2 && item.MedicationName.Contains("MedB"));
        }

        [Fact]
        public async Task GetPrescriptionDetailHeaderAsync_ShouldReturnCorrectHeaderInfo()
        {
            // Arrange: Ensure _testVisitId1 has items sent to pharmacy by _testUserIdDoctor for _testPatientId1
            // The SetupInitialTestDataAsync should already create these base entities.
            // Seed a prescription item to establish created_by_user_id and pharmacy_sent_at for the header query.
            await Connection.ExecuteAsync(
               @"INSERT INTO app.prescription_items (visit_id, medication_id, dosage, frequency, quantity_prescribed, created_by_user_id, is_sent_to_pharmacy, pharmacy_sent_at)
          VALUES (@VisitId, @MedId, '1 tab', 'OD', '7', @UserId, TRUE, NOW() - interval '1 hour')", // Ensure pharmacy_sent_at is set
               new { VisitId = _testVisitId1, MedId = _testMedicationId1, UserId = _testUserIdDoctor }, transaction: Transaction);


            // Act
            var header = await _repository.GetPrescriptionDetailHeaderAsync(_testVisitId1, Connection, Transaction);

            // Assert
            Assert.NotNull(header);
            Assert.Equal(_testVisitId1, header.VisitId);
            Assert.Equal(_testPatientId1, header.PatientId);
            Assert.Contains("John Doe", header.PatientName); // From SetupInitialTestDataAsync
            Assert.Contains("Test Doctor", header.PrescriberName);
            Assert.NotNull(header.PrescriptionDate); // Should be populated from pharmacy_sent_at
            Assert.Equal($"Rx-{_testVisitId1}", header.PrescriptionIdentifier);
        }

        [Fact]
        public async Task GetPrescriptionDetailHeaderAsync_ShouldReturnNull_IfNoItemsSentToPharmacyForVisit()
        {
            // Arrange: _testVisitId2 might not have items sent to pharmacy yet in this test setup
            // Or ensure no items are sent for a specific visitId
            var newVisitIdWithoutRxItems = await Connection.ExecuteScalarAsync<int>(
                 "INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id) VALUES (@PatientId, 'Discharged', @UserId, @UserId) RETURNING visit_id;",
                 new { PatientId = _testPatientId2, UserId = _testUserIdDoctor }, transaction: Transaction);


            // Act
            var header = await _repository.GetPrescriptionDetailHeaderAsync(newVisitIdWithoutRxItems, Connection, Transaction);

            // Assert
            Assert.Null(header); // Query for header joins on prescription_items where is_sent_to_pharmacy = TRUE
        }

        [Fact]
        public async Task UpdatePrescriptionItemDispenseStatusAsync_ShouldUpdateStatusAndQuantities()
        {
            // Arrange
            // Seed a basic prescription item
            var inputDto = new AddPrescriptionItemInputDto { VisitId = _testVisitId1, MedicationId = _testMedicationId1, Dosage = "1", Frequency = "OD", QuantityPrescribed = "10" };
            var createdItem = await _repository.AddPrescriptionItemAsync(inputDto, _testUserIdDoctor, Connection, Transaction);
            Assert.NotNull(createdItem);
            int prescriptionItemId = createdItem.PrescriptionItemId;

            string newTotalQuantityDispensed = "5 tablets";
            bool isFullyDispensed = false;
            int pharmacistUserId = _testUserIdPharmacist; // Use a pharmacist user ID

            // Act
            bool result = await _repository.UpdatePrescriptionItemDispenseStatusAsync(
                prescriptionItemId, newTotalQuantityDispensed, isFullyDispensed, pharmacistUserId, Connection, Transaction);

            // Assert
            Assert.True(result);

            var dbItem = await Connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT quantity_dispensed, is_fully_dispensed, last_dispensed_at, last_dispensed_by_user_id FROM app.prescription_items WHERE prescription_item_id = @Id",
                new { Id = prescriptionItemId }, transaction: Transaction);

            Assert.NotNull(dbItem);
            Assert.Equal(newTotalQuantityDispensed, (string)dbItem.quantity_dispensed);
            Assert.Equal(isFullyDispensed, (bool)dbItem.is_fully_dispensed);
            Assert.NotNull(dbItem.last_dispensed_at);
            Assert.Equal(pharmacistUserId, (int)dbItem.last_dispensed_by_user_id);
        }

        [Fact]
        public async Task UpdatePrescriptionItemDispenseStatusAsync_ShouldMarkAsFullyDispensed()
        {
            // Arrange
            var inputDto = new AddPrescriptionItemInputDto { VisitId = _testVisitId1, MedicationId = _testMedicationId1, Dosage = "1", Frequency = "OD", QuantityPrescribed = "10" };
            var createdItem = await _repository.AddPrescriptionItemAsync(inputDto, _testUserIdDoctor, Connection, Transaction);
            Assert.NotNull(createdItem);
            int prescriptionItemId = createdItem.PrescriptionItemId;

            string newTotalQuantityDispensed = "10 tablets"; // Matching prescribed
            bool isFullyDispensed = true;
            int pharmacistUserId = _testUserIdPharmacist;

            // Act
            bool result = await _repository.UpdatePrescriptionItemDispenseStatusAsync(
                prescriptionItemId, newTotalQuantityDispensed, isFullyDispensed, pharmacistUserId, Connection, Transaction);

            // Assert
            Assert.True(result);
            var dbItem = await Connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT is_fully_dispensed FROM app.prescription_items WHERE prescription_item_id = @Id",
                new { Id = prescriptionItemId }, transaction: Transaction);
            Assert.NotNull(dbItem);
            Assert.True((bool)dbItem.is_fully_dispensed);
        }

        [Fact]
        public async Task UpdatePrescriptionItemDispenseStatusAsync_ShouldReturnFalse_WhenItemNotFound()
        {
            // Arrange
            int nonExistentItemId = -1;

            // Act
            bool result = await _repository.UpdatePrescriptionItemDispenseStatusAsync(
                nonExistentItemId, "5", false, _testUserIdPharmacist, Connection, Transaction);

            // Assert
            Assert.False(result);
        }

        private async Task SetupBaseTestDataAsync()
        {
            _testUserIdDoctor = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.users (force_number, first_name, last_name, rank, department) VALUES (@FN, 'Test', 'DoctorRx', 'Dr', 'ClinicRx') RETURNING user_id;",
                new { FN = $"U_RXINT_DOC_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);

            _testPatientId1 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.patients (force_number, first_name, last_name, rank) VALUES (@FN, 'RxPat1', 'Doe', 'Pte') RETURNING patient_id;",
                new { FN = $"P_RXINT_RP1_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);
            _testPatientId2 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.patients (force_number, first_name, last_name, rank) VALUES (@FN, 'RxPat2', 'Smith', 'Sgt') RETURNING patient_id;",
                new { FN = $"P_RXINT_RP2_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);

            _testVisitId1 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PatientId, 'Discharged', @UserId, @UserId, NOW() - INTERVAL '1 day') RETURNING visit_id;",
                new { PatientId = _testPatientId1, UserId = _testUserIdDoctor }, transaction: Transaction);
            _testVisitId2 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PatientId, 'Discharged', @UserId, @UserId, NOW() - INTERVAL '2 day') RETURNING visit_id;",
                new { PatientId = _testPatientId2, UserId = _testUserIdDoctor }, transaction: Transaction);

            _testMedicationId1 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.medications (name, strength, form, category) VALUES ('RxMedA', '10mg', 'Tab', 'CatRxA') ON CONFLICT (name, strength, form) DO NOTHING RETURNING medication_id;", transaction: Transaction);
            if (_testMedicationId1 == 0) _testMedicationId1 = await Connection.ExecuteScalarAsync<int>("SELECT medication_id FROM app.medications WHERE name='RxMedA'", transaction: Transaction);

            _testMedicationId2 = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.medications (name, strength, form, category) VALUES ('RxMedB', '20mg', 'Cap', 'CatRxB') ON CONFLICT (name, strength, form) DO NOTHING RETURNING medication_id;", transaction: Transaction);
            if (_testMedicationId2 == 0) _testMedicationId2 = await Connection.ExecuteScalarAsync<int>("SELECT medication_id FROM app.medications WHERE name='RxMedB'", transaction: Transaction);
            Fixture.Output?.WriteLine($"Setup Rx Integration Tests: DocId={_testUserIdDoctor}, P1={_testPatientId1}, P2={_testPatientId2}, V1={_testVisitId1}, V2={_testVisitId2}, M1={_testMedicationId1}, M2={_testMedicationId2}");
        }


        private async Task<int> SeedPrescriptionItemAsync(int visitId, int medicationId, bool isSentToPharmacy, bool isFullyDispensed, DateTime? pharmacySentAt = null)
        {
            pharmacySentAt ??= isSentToPharmacy ? DateTime.UtcNow : (DateTime?)null;
            const string sql = @"
                INSERT INTO app.prescription_items
                    (visit_id, medication_id, dosage, frequency, quantity_prescribed, created_by_user_id, 
                     is_sent_to_pharmacy, pharmacy_sent_at, is_fully_dispensed, quantity_dispensed)
                VALUES
                    (@VisitId, @MedicationId, '1 tab', 'OD', '7 tablets', @CreatedByUserId, 
                     @IsSentToPharmacy, @PharmacySentAt, @IsFullyDispensed, @QuantityDispensed)
                RETURNING prescription_item_id;";
            return await Connection.ExecuteScalarAsync<int>(sql, new
            {
                VisitId = visitId,
                MedicationId = medicationId,
                CreatedByUserId = _testUserIdDoctor,
                IsSentToPharmacy = isSentToPharmacy,
                PharmacySentAt = pharmacySentAt,
                IsFullyDispensed = isFullyDispensed,
                QuantityDispensed = isFullyDispensed ? "7 tablets" : (isSentToPharmacy ? "0 tablets" : null) // Example logic
            }, transaction: Transaction);
        }

        [Fact]
        public async Task GetPendingPrescriptionsSummaryAsync_ShouldOnlyReturnVisitsWithUnDispensedSentItems()
        {
            // Arrange
            // Visit 1: 1 item sent & not dispensed, 1 item sent & fully dispensed
            await SeedPrescriptionItemAsync(_testVisitId1, _testMedicationId1, true, false, DateTime.UtcNow.AddHours(-2)); // Pending
            await SeedPrescriptionItemAsync(_testVisitId1, _testMedicationId2, true, true, DateTime.UtcNow.AddHours(-2));  // Fully Dispensed

            // Visit 2: 1 item sent & not dispensed
            await SeedPrescriptionItemAsync(_testVisitId2, _testMedicationId1, true, false, DateTime.UtcNow.AddHours(-1)); // Pending

            // Visit 3 (new one for this test): All items fully dispensed
            var visit3 = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PatientId, 'Discharged', @UserId, @UserId, NOW() - INTERVAL '5 minutes') RETURNING visit_id;", new { PatientId = _testPatientId1, UserId = _testUserIdDoctor }, transaction: Transaction);
            await SeedPrescriptionItemAsync(visit3, _testMedicationId1, true, true, DateTime.UtcNow.AddMinutes(-5)); // Fully Dispensed

            // Visit 4: Item not sent to pharmacy
            var visit4 = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PatientId, 'Discharged', @UserId, @UserId, NOW() - INTERVAL '4 minutes') RETURNING visit_id;", new { PatientId = _testPatientId2, UserId = _testUserIdDoctor }, transaction: Transaction);
            await SeedPrescriptionItemAsync(visit4, _testMedicationId2, false, false); // Not sent

            // Act
            var results = (await _repository.GetPendingPrescriptionsSummaryAsync(limit: 10, offset: 0, connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count); // Only Visit 1 and Visit 2 should appear

            Assert.Contains(results, r => r.VisitId == _testVisitId1);
            Assert.Contains(results, r => r.VisitId == _testVisitId2);
            Assert.DoesNotContain(results, r => r.VisitId == visit3); // Should be excluded
            Assert.DoesNotContain(results, r => r.VisitId == visit4); // Should be excluded

            var visit1Summary = results.First(r => r.VisitId == _testVisitId1);
            // The NumberOfMedications in the query counts ALL items sent to pharmacy for that visit.
            // This is okay as it represents the whole prescription for that visit.
            Assert.Equal(2, visit1Summary.NumberOfMedications);
        }

        [Fact]
        public async Task GetPharmacistDashboardStatsAsync_ShouldCountOnlyVisitsWithUndispensedSentItems()
        {
            // Arrange
            // Visit 1: Has one pending item
            await SeedPrescriptionItemAsync(_testVisitId1, _testMedicationId1, true, false); // Sent, Not Dispensed
            // Visit 2: All items fully dispensed
            await SeedPrescriptionItemAsync(_testVisitId2, _testMedicationId1, true, true);  // Sent, Fully Dispensed
            // Visit 3: Item not sent to pharmacy
            var visit3 = await Connection.ExecuteScalarAsync<int>("INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, assigned_officer_user_id, visit_timestamp) VALUES (@PatientId, 'Discharged', @UserId, @UserId, NOW() - INTERVAL '5 minutes') RETURNING visit_id;", new { PatientId = _testPatientId1, UserId = _testUserIdDoctor }, transaction: Transaction);
            await SeedPrescriptionItemAsync(visit3, _testMedicationId2, false, false); // Not Sent

            // Act
            var stats = await _repository.GetPharmacistDashboardStatsAsync(connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(1, stats.PendingPrescriptionsCount); // Only Visit 1 has items pending dispense
        }
    }
}