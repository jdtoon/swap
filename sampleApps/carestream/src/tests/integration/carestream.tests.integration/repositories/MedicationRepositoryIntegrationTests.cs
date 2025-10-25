using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System.Linq;
using carestream.persistence.repositories;
using carestream.core.interfaces.repositories;
using Dapper; // For direct seeding if necessary
using System; // For Guid

namespace carestream.tests.integration.repositories
{
    public class MedicationRepositoryIntegrationTests : BaseIntegrationTest
    {
        private readonly IMedicationRepository _repository;
        private int _medIdWithStock;
        private int _medIdWithoutStockEntry;
        private int _medIdActiveNoStockRecord;
        private int _medIdForDecrementTest; 
        private int _medIdNoStockRecord;

        public MedicationRepositoryIntegrationTests(DatabaseTestFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _repository = new MedicationRepository(Configuration, GetMockLogger<MedicationRepository>(), GetCurrentFacilityContext());
            // Seed initial medications directly here as part of test setup within transaction
            // This ensures data is present for these specific tests and isolated.
            // DbUp seed Script0009_AddMedication.sql runs only in Development for the app.
            // For tests, we want controlled, explicit data.
            SeedMedicationsForTestAsync().GetAwaiter().GetResult();
            SeedMedicationAndStockDataAsync().GetAwaiter().GetResult();
            SeedMedicationAndStockDataForTestsAsync().GetAwaiter().GetResult();
        }

        private async Task SeedMedicationAndStockDataForTestsAsync()
        {
            var med1 = new { Name = "StockMedA_IT", Strength = "10mg", Form = "Tablet", Category = "StockTestIT", IsActive = true };
            var med2 = new { Name = "StockMedB_Decrement", Strength = "20mg", Form = "Capsule", Category = "StockTestIT", IsActive = true };
            var med3 = new { Name = "StockMedC_NoRecord", Strength = "5mg", Form = "Syrup", Category = "StockTestIT", IsActive = true };

            await Connection.ExecuteAsync(
                @"INSERT INTO app.medications (name, strength, form, category, is_active) VALUES (@Name, @Strength, @Form, @Category, @IsActive) ON CONFLICT (name, strength, form) DO NOTHING;",
                new[] { med1, med2, med3 }, transaction: Transaction);

            _medIdWithStock = await Connection.QuerySingleAsync<int>("SELECT medication_id FROM app.medications WHERE name = 'StockMedA_IT'", transaction: Transaction);
            _medIdForDecrementTest = await Connection.QuerySingleAsync<int>("SELECT medication_id FROM app.medications WHERE name = 'StockMedB_Decrement'", transaction: Transaction);
            _medIdNoStockRecord = await Connection.QuerySingleAsync<int>("SELECT medication_id FROM app.medications WHERE name = 'StockMedC_NoRecord'", transaction: Transaction);

            // Seed stock for some
            await Connection.ExecuteAsync(
                @"INSERT INTO app.medication_stock (medication_id, quantity_on_hand) VALUES (@MedicationId, @Quantity) ON CONFLICT (medication_id) DO UPDATE SET quantity_on_hand = EXCLUDED.quantity_on_hand;",
                new[] {
                    new { MedicationId = _medIdWithStock, Quantity = 125 },
                    new { MedicationId = _medIdForDecrementTest, Quantity = 50 } // Initial stock for decrement test
                }, transaction: Transaction);

            // Ensure no stock record for _medIdNoStockRecord
            await Connection.ExecuteAsync("DELETE FROM app.medication_stock WHERE medication_id = @MedId", new { MedId = _medIdNoStockRecord }, transaction: Transaction);

            Fixture.Output?.WriteLine($"Seeded data for MedicationStockTests: MedWithStockId={_medIdWithStock} (125), MedForDecrementId={_medIdForDecrementTest} (50), MedNoStockRecId={_medIdNoStockRecord}");
        }

        private async Task SeedMedicationsForTestAsync()
        {
            // Using ON CONFLICT DO NOTHING to make seeding idempotent within test runs if needed,
            // though transactions should isolate. Best to ensure clean state.
            var medications = new[]
            {
                new { Name = "Amoxicillin", Strength = "250mg", Form = "Capsule", Category = "Antibiotic", IsActive = true },
                new { Name = "Amoxicillin", Strength = "500mg", Form = "Capsule", Category = "Antibiotic", IsActive = true },
                new { Name = "Paracetamol", Strength = "500mg", Form = "Tablet", Category = "Analgesic", IsActive = true },
                new { Name = "Ibuprofen", Strength = "200mg", Form = "Tablet", Category = "NSAID", IsActive = true },
                new { Name = "Aspirin", Strength = "75mg", Form = "Tablet", Category = "Antiplatelet", IsActive = true },
                new { Name = "InactiveMed", Strength = "100mg", Form = "Tablet", Category = "Test", IsActive = false }
            };
            const string sql = @"
                INSERT INTO app.medications (name, strength, form, category, is_active)
                VALUES (@Name, @Strength, @Form, @Category, @IsActive)
                ON CONFLICT (name, strength, form) DO NOTHING;"; // Unique constraint
            await Connection.ExecuteAsync(sql, medications, transaction: Transaction);
            Fixture.Output?.WriteLine($"Seeded {medications.Length} medications for MedicationRepositoryTests.");
        }

        private async Task SeedMedicationAndStockDataAsync()
        {
            // Seed base medications if not already covered by a broader seed in fixture or base test
            var med1 = new { Name = "StockMedA", Strength = "10mg", Form = "Tablet", Category = "StockTest", IsActive = true };
            var med2 = new { Name = "StockMedB", Strength = "20mg", Form = "Capsule", Category = "StockTest", IsActive = true };
            var med3 = new { Name = "StockMedC_NoStockRec", Strength = "5mg", Form = "Syrup", Category = "StockTest", IsActive = true };


            await Connection.ExecuteAsync(
                @"INSERT INTO app.medications (name, strength, form, category, is_active) VALUES (@Name, @Strength, @Form, @Category, @IsActive) ON CONFLICT (name, strength, form) DO NOTHING;",
                new[] { med1, med2, med3 }, transaction: Transaction);

            // Get IDs
            _medIdWithStock = await Connection.QuerySingleAsync<int>("SELECT medication_id FROM app.medications WHERE name = 'StockMedA'", transaction: Transaction);
            _medIdWithoutStockEntry = await Connection.QuerySingleAsync<int>("SELECT medication_id FROM app.medications WHERE name = 'StockMedB'", transaction: Transaction);
            _medIdActiveNoStockRecord = await Connection.QuerySingleAsync<int>("SELECT medication_id FROM app.medications WHERE name = 'StockMedC_NoStockRec'", transaction: Transaction);


            // Seed stock for one medication
            await Connection.ExecuteAsync(
                @"INSERT INTO app.medication_stock (medication_id, quantity_on_hand) VALUES (@MedicationId, @Quantity) ON CONFLICT (medication_id) DO UPDATE SET quantity_on_hand = EXCLUDED.quantity_on_hand;",
                new { MedicationId = _medIdWithStock, Quantity = 125 }, transaction: Transaction);

            // Ensure medIdWithoutStockEntry does NOT have a record in app.medication_stock
            await Connection.ExecuteAsync(
                @"DELETE FROM app.medication_stock WHERE medication_id = @MedicationId;",
                new { MedicationId = _medIdWithoutStockEntry }, transaction: Transaction);
            await Connection.ExecuteAsync(
                @"DELETE FROM app.medication_stock WHERE medication_id = @MedicationId;",
                new { MedicationId = _medIdActiveNoStockRecord }, transaction: Transaction);

            Fixture.Output?.WriteLine($"Seeded data for MedicationStockTests: MedWithStockId={_medIdWithStock}, MedWithoutStockEntryId={_medIdWithoutStockEntry}, MedActiveNoStockRecId={_medIdActiveNoStockRecord}");
        }

        [Theory]
        [InlineData("Amoxi", 2, "Amoxicillin")]
        [InlineData("para", 1, "Paracetamol")]
        [InlineData("aspirin", 1, "Aspirin")]
        [InlineData("Antibiotic", 2, "Amoxicillin")] // Category search, check for an item in that category
        [InlineData("NonExistent", 0, "")]
        [InlineData("InactiveMed", 1, "")]
        public async Task SearchMedicationsAsync_ShouldReturnMatchingActiveMedications(string searchTerm, int expectedCount, string partOfExpectedDisplayName)
        {
            // Act
            var results = (await _repository.SearchMedicationsAsync(searchTerm, connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(expectedCount, results.Count);
            if (expectedCount > 0)
            {
                // Check if ALL returned items contain the search term in their display name (case-insensitive)
                // OR if searching by category, check if at least one item matches a known name from that category.
                // This assertion is tricky because a category search returns multiple items.
                // A simpler assertion for category search might be to just check one expected item.
                if (searchTerm == "Antibiotic")
                {
                    Assert.Contains(results, item => item.DisplayName.Contains("Amoxicillin", StringComparison.OrdinalIgnoreCase));
                }
                else if (!string.IsNullOrEmpty(partOfExpectedDisplayName)) // For non-category searches
                {
                    Assert.All(results, item => Assert.Contains(searchTerm, item.DisplayName, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        [Fact]
        public async Task SearchMedicationsAsync_WithLimit_ShouldReturnLimitedResults()
        {
            // Act
            var results = (await _repository.SearchMedicationsAsync("Amoxi", limit: 1, connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.NotNull(results);
            Assert.Single(results); // Expects only one due to limit
        }

        [Fact]
        public async Task GetMedicationByIdAsync_ShouldReturnMedication_WhenExistsAndActive()
        {
            // Arrange: Need to get an ID of a seeded medication
            var seededMed = await Connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT medication_id FROM app.medications WHERE name = 'Paracetamol' AND strength = '500mg' AND form = 'Tablet'",
                transaction: Transaction);
            Assert.NotNull(seededMed);
            int paracetamolId = (int)seededMed.medication_id;

            // Act
            var result = await _repository.GetMedicationByIdAsync(paracetamolId, Connection, Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Paracetamol", result.Name);
            Assert.Equal("500mg", result.Strength);
        }

        [Fact]
        public async Task GetMedicationByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _repository.GetMedicationByIdAsync(-1, Connection, Transaction);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetMedicationByIdAsync_ShouldReturnNull_WhenExistsButInactive()
        {
            // Arrange: Need to get an ID of an inactive seeded medication
            var seededInactiveMed = await Connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT medication_id FROM app.medications WHERE name = 'InactiveMed'",
                transaction: Transaction);
            Assert.NotNull(seededInactiveMed);
            int inactiveMedId = (int)seededInactiveMed.medication_id;

            // Act
            var result = await _repository.GetMedicationByIdAsync(inactiveMedId, Connection, Transaction);

            // Assert
            Assert.Null(result); // Because the repo query filters by is_active = TRUE
        }

        [Fact]
        public async Task GetStockOnHandAsync_ShouldReturnQuantity_WhenStockRecordExists()
        {
            // Arrange: _medIdWithStock was seeded with quantity 125

            // Act
            int? stock = await _repository.GetStockOnHandAsync(_medIdWithStock, Connection, Transaction);

            // Assert
            Assert.NotNull(stock);
            Assert.Equal(125, stock.Value);
        }

        [Fact]
        public async Task GetStockOnHandAsync_ShouldReturnNull_WhenNoStockRecordExistsForMedication()
        {
            // Arrange: _medIdActiveNoStockRecord exists in app.medications but has no entry in app.medication_stock

            // Act
            int? stock = await _repository.GetStockOnHandAsync(_medIdActiveNoStockRecord, Connection, Transaction);

            // Assert
            Assert.Null(stock); // Dapper QueryFirstOrDefaultAsync<int?> returns null if no row
        }

        [Fact]
        public async Task GetStockOnHandAsync_ShouldReturnNull_WhenMedicationIdDoesNotExist()
        {
            // Arrange
            int nonExistentMedicationId = -999;

            // Act
            int? stock = await _repository.GetStockOnHandAsync(nonExistentMedicationId, Connection, Transaction);

            // Assert
            Assert.Null(stock);
        }

        [Fact]
        public async Task DecrementStockAsync_SufficientStock_ShouldDecrementAndReturnTrue()
        {
            // Arrange
            int initialStock = 50; // As seeded for _medIdForDecrementTest
            int quantityToDecrement = 10;
            int expectedStockAfter = initialStock - quantityToDecrement;

            // Act
            bool result = await _repository.DecrementStockAsync(_medIdForDecrementTest, quantityToDecrement, Connection, Transaction);

            // Assert
            Assert.True(result, "DecrementStockAsync should return true when stock is sufficient.");
            var currentStock = await Connection.QuerySingleOrDefaultAsync<int?>(
                "SELECT quantity_on_hand FROM app.medication_stock WHERE medication_id = @MedicationId",
                new { MedicationId = _medIdForDecrementTest }, transaction: Transaction);
            Assert.NotNull(currentStock);
            Assert.Equal(expectedStockAfter, currentStock.Value);
        }

        [Fact]
        public async Task DecrementStockAsync_ExactStock_ShouldDecrementToZeroAndReturnTrue()
        {
            // Arrange
            int initialStock = 50; // As seeded
            int quantityToDecrement = initialStock;
            int expectedStockAfter = 0;

            // Act
            bool result = await _repository.DecrementStockAsync(_medIdForDecrementTest, quantityToDecrement, Connection, Transaction);

            // Assert
            Assert.True(result);
            var currentStock = await Connection.QuerySingleOrDefaultAsync<int?>(
                "SELECT quantity_on_hand FROM app.medication_stock WHERE medication_id = @MedicationId",
                new { MedicationId = _medIdForDecrementTest }, transaction: Transaction);
            Assert.NotNull(currentStock);
            Assert.Equal(expectedStockAfter, currentStock.Value);
        }

        [Fact]
        public async Task DecrementStockAsync_InsufficientStock_ShouldNotDecrementAndReturnFalse()
        {
            // Arrange
            int initialStock = 50; // As seeded
            int quantityToDecrement = initialStock + 10; // More than available

            // Act
            bool result = await _repository.DecrementStockAsync(_medIdForDecrementTest, quantityToDecrement, Connection, Transaction);

            // Assert
            Assert.False(result, "DecrementStockAsync should return false when stock is insufficient.");
            var currentStock = await Connection.QuerySingleOrDefaultAsync<int?>(
                "SELECT quantity_on_hand FROM app.medication_stock WHERE medication_id = @MedicationId",
                new { MedicationId = _medIdForDecrementTest }, transaction: Transaction);
            Assert.NotNull(currentStock);
            Assert.Equal(initialStock, currentStock.Value); // Stock should remain unchanged
        }

        [Fact]
        public async Task DecrementStockAsync_NoStockRecord_ShouldReturnFalse()
        {
            // Arrange: _medIdNoStockRecord has no entry in app.medication_stock
            int quantityToDecrement = 5;

            // Act
            bool result = await _repository.DecrementStockAsync(_medIdNoStockRecord, quantityToDecrement, Connection, Transaction);

            // Assert
            Assert.False(result, "DecrementStockAsync should return false if no stock record exists for the medication.");
        }

        [Fact]
        public async Task DecrementStockAsync_NonExistentMedicationId_ShouldReturnFalse()
        {
            // Arrange
            int nonExistentMedicationId = -998;
            int quantityToDecrement = 5;

            // Act
            bool result = await _repository.DecrementStockAsync(nonExistentMedicationId, quantityToDecrement, Connection, Transaction);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DecrementStockAsync_DecrementByZero_ShouldReturnTrueAndNotChangeStock()
        {
            // Arrange
            int initialStock = 50; // As seeded for _medIdForDecrementTest
            int quantityToDecrement = 0;

            // Act
            bool result = await _repository.DecrementStockAsync(_medIdForDecrementTest, quantityToDecrement, Connection, Transaction);

            // Assert
            Assert.True(result, "Decrementing by zero should be considered a successful operation (no change).");
            var currentStock = await Connection.QuerySingleOrDefaultAsync<int?>(
                "SELECT quantity_on_hand FROM app.medication_stock WHERE medication_id = @MedicationId",
                new { MedicationId = _medIdForDecrementTest }, transaction: Transaction);
            Assert.NotNull(currentStock);
            Assert.Equal(initialStock, currentStock.Value); // Stock should remain unchanged
        }

        [Fact]
        public async Task DecrementStockAsync_DecrementByNegative_ShouldReturnFalseAndNotChangeStock()
        {
            // Arrange
            int initialStock = 50; // As seeded for _medIdForDecrementTest
            int quantityToDecrement = -5;

            // Act
            bool result = await _repository.DecrementStockAsync(_medIdForDecrementTest, quantityToDecrement, Connection, Transaction);

            // Assert
            Assert.False(result, "Decrementing by a negative value should fail.");
            var currentStock = await Connection.QuerySingleOrDefaultAsync<int?>(
                "SELECT quantity_on_hand FROM app.medication_stock WHERE medication_id = @MedicationId",
                new { MedicationId = _medIdForDecrementTest }, transaction: Transaction);
            Assert.NotNull(currentStock);
            Assert.Equal(initialStock, currentStock.Value); // Stock should remain unchanged
        }
    }
}