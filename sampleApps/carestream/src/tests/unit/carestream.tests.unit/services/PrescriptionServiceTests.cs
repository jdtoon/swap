using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.services;
using carestream.core.dtos.medication;
using carestream.core.dtos.prescription;
using carestream.core.dtos.consultation;

namespace carestream.tests.unit.services
{
    public class PrescriptionServiceTests
    {
        private readonly Mock<IMedicationRepository> _mockMedicationRepository;
        private readonly Mock<IPrescriptionRepository> _mockPrescriptionRepository;
        private readonly Mock<ILogger<PrescriptionService>> _mockLogger;
        private readonly IPrescriptionService _prescriptionService;

        public PrescriptionServiceTests()
        {
            _mockMedicationRepository = new Mock<IMedicationRepository>();
            _mockPrescriptionRepository = new Mock<IPrescriptionRepository>();
            _mockLogger = new Mock<ILogger<PrescriptionService>>();

            _prescriptionService = new PrescriptionService(
                _mockMedicationRepository.Object,
                _mockPrescriptionRepository.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task SearchMedicationsAsync_WithValidSearchTerm_ShouldCallRepositoryAndReturnResults()
        {
            // Arrange
            string searchTerm = "Amo";
            int limit = 7;
            var expectedResults = new List<MedicationSearchResultDto> { new MedicationSearchResultDto { MedicationId = 1, Name = "Amoxicillin" } };
            _mockMedicationRepository.Setup(repo => repo.SearchMedicationsAsync(searchTerm, limit, null, null))
                                     .ReturnsAsync(expectedResults);

            // Act
            var result = await _prescriptionService.SearchMedicationsAsync(searchTerm, limit);

            // Assert
            Assert.Same(expectedResults, result);
            _mockMedicationRepository.Verify(repo => repo.SearchMedicationsAsync(searchTerm, limit, null, null), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("A")] // Assuming min length is 2 as per service logic
        public async Task SearchMedicationsAsync_WithInvalidSearchTerm_ShouldReturnEmptyAndNotCallRepository(string? searchTerm)
        {
            // Act
            var result = await _prescriptionService.SearchMedicationsAsync(searchTerm!); // Use ! for nullable theory data

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockMedicationRepository.Verify(repo => repo.SearchMedicationsAsync(It.IsAny<string>(), It.IsAny<int>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GetMedicationsViewModelAsync_ShouldCallRepositoryAndReturnViewModel()
        {
            // Arrange
            int visitId = 1;
            int patientId = 101;
            // This is the list instance the mock will return
            var expectedPrescriptionItemsFromRepo = new List<PrescriptionItemDto>
    {
        new PrescriptionItemDto { PrescriptionItemId = 1, MedicationName = "Test Med Alpha" },
        new PrescriptionItemDto { PrescriptionItemId = 2, MedicationName = "Test Med Bravo" }
    };

            _mockPrescriptionRepository.Setup(repo => repo.GetPrescriptionItemsForVisitAsync(visitId, null, null))
                                       .ReturnsAsync(expectedPrescriptionItemsFromRepo);

            // Act
            var viewModel = await _prescriptionService.GetMedicationsViewModelAsync(visitId, patientId);

            // Assert
            Assert.NotNull(viewModel);
            Assert.Equal(visitId, viewModel.VisitId);
            Assert.Equal(patientId, viewModel.PatientId);
            Assert.NotNull(viewModel.NewPrescriptionItem);
            Assert.Equal(visitId, viewModel.NewPrescriptionItem.VisitId);

            // Assert the content of the list
            Assert.NotNull(viewModel.CurrentPrescriptionItems);
            // Assert.Equal checks if two collections have the same elements in the same order.
            // This requires PrescriptionItemDto to have a proper Equals implementation
            // or we check properties. For DTOs, checking properties is often clearer.
            Assert.Equal(expectedPrescriptionItemsFromRepo.Count, viewModel.CurrentPrescriptionItems.Count);

            if (expectedPrescriptionItemsFromRepo.Any() && viewModel.CurrentPrescriptionItems.Any())
            {
                // Example: Check properties of the first item if the list isn't empty
                // This assumes the order is preserved by .ToList() which it is.
                for (int i = 0; i < expectedPrescriptionItemsFromRepo.Count; i++)
                {
                    Assert.Equal(expectedPrescriptionItemsFromRepo[i].PrescriptionItemId, viewModel.CurrentPrescriptionItems[i].PrescriptionItemId);
                    Assert.Equal(expectedPrescriptionItemsFromRepo[i].MedicationName, viewModel.CurrentPrescriptionItems[i].MedicationName);
                    // Add more property checks as needed
                }
            }


            _mockPrescriptionRepository.Verify(repo => repo.GetPrescriptionItemsForVisitAsync(visitId, null, null), Times.Once);
        }

        [Fact]
        public async Task AddPrescriptionItemAsync_WhenRepositorySucceeds_ShouldReturnUpdatedList()
        {
            // Arrange
            var inputDto = new AddPrescriptionItemInputDto { VisitId = 1, MedicationId = 1, Dosage = "1 tab" };
            int prescribingUserId = 5;
            var addedItemDto = new PrescriptionItemDto { PrescriptionItemId = 10, MedicationId = 1, MedicationName = "Amoxicillin" }; // What AddPrescriptionItemAsync returns
            var updatedList = new List<PrescriptionItemDto> { addedItemDto }; // What GetPrescriptionItemsForVisitAsync returns after add

            _mockPrescriptionRepository.Setup(repo => repo.AddPrescriptionItemAsync(inputDto, prescribingUserId, null, null))
                                       .ReturnsAsync(addedItemDto);
            _mockPrescriptionRepository.Setup(repo => repo.GetPrescriptionItemsForVisitAsync(inputDto.VisitId, null, null))
                                       .ReturnsAsync(updatedList);

            // Act
            var result = await _prescriptionService.AddPrescriptionItemAsync(inputDto, prescribingUserId);

            // Assert
            Assert.Same(updatedList, result);
            _mockPrescriptionRepository.Verify(repo => repo.AddPrescriptionItemAsync(inputDto, prescribingUserId, null, null), Times.Once);
            _mockPrescriptionRepository.Verify(repo => repo.GetPrescriptionItemsForVisitAsync(inputDto.VisitId, null, null), Times.Once);
        }

        [Fact]
        public async Task AddPrescriptionItemAsync_WhenRepositoryAddFails_ShouldReturnCurrentListWithoutAdding()
        {
            // Arrange
            var inputDto = new AddPrescriptionItemInputDto { VisitId = 1, MedicationId = 1, Dosage = "1 tab" };
            int prescribingUserId = 5;
            var existingList = new List<PrescriptionItemDto>(); // No items initially

            _mockPrescriptionRepository.Setup(repo => repo.AddPrescriptionItemAsync(inputDto, prescribingUserId, null, null))
                                       .ReturnsAsync((PrescriptionItemDto?)null); // Simulate add failure
            _mockPrescriptionRepository.Setup(repo => repo.GetPrescriptionItemsForVisitAsync(inputDto.VisitId, null, null))
                                       .ReturnsAsync(existingList); // Returns the list as it was

            // Act
            var result = await _prescriptionService.AddPrescriptionItemAsync(inputDto, prescribingUserId);

            // Assert
            Assert.Same(existingList, result); // Should be the list *before* the attempted add
            _mockPrescriptionRepository.Verify(repo => repo.AddPrescriptionItemAsync(inputDto, prescribingUserId, null, null), Times.Once);
            _mockPrescriptionRepository.Verify(repo => repo.GetPrescriptionItemsForVisitAsync(inputDto.VisitId, null, null), Times.Once);
        }


        [Fact]
        public async Task RemovePrescriptionItemAsync_WhenRepositorySucceeds_ShouldReturnUpdatedList()
        {
            // Arrange
            int prescriptionItemId = 1;
            int visitId = 10;
            var updatedList = new List<PrescriptionItemDto>(); // Empty list after removal

            _mockPrescriptionRepository.Setup(repo => repo.RemovePrescriptionItemAsync(prescriptionItemId, null, null))
                                       .ReturnsAsync(true);
            _mockPrescriptionRepository.Setup(repo => repo.GetPrescriptionItemsForVisitAsync(visitId, null, null))
                                       .ReturnsAsync(updatedList);

            // Act
            var result = await _prescriptionService.RemovePrescriptionItemAsync(prescriptionItemId, visitId);

            // Assert
            Assert.Same(updatedList, result);
            _mockPrescriptionRepository.Verify(repo => repo.RemovePrescriptionItemAsync(prescriptionItemId, null, null), Times.Once);
            _mockPrescriptionRepository.Verify(repo => repo.GetPrescriptionItemsForVisitAsync(visitId, null, null), Times.Once);
        }

        [Fact]
        public async Task RemovePrescriptionItemAsync_WhenRepositoryFails_ShouldReturnCurrentList()
        {
            // Arrange
            int prescriptionItemId = 1;
            int visitId = 10;
            var currentList = new List<PrescriptionItemDto> { new PrescriptionItemDto { PrescriptionItemId = prescriptionItemId } };

            _mockPrescriptionRepository.Setup(repo => repo.RemovePrescriptionItemAsync(prescriptionItemId, null, null))
                                       .ReturnsAsync(false); // Simulate removal failure
            _mockPrescriptionRepository.Setup(repo => repo.GetPrescriptionItemsForVisitAsync(visitId, null, null))
                                       .ReturnsAsync(currentList); // Returns the list as it was

            // Act
            var result = await _prescriptionService.RemovePrescriptionItemAsync(prescriptionItemId, visitId);

            // Assert
            Assert.Same(currentList, result);
            _mockPrescriptionRepository.Verify(repo => repo.RemovePrescriptionItemAsync(prescriptionItemId, null, null), Times.Once);
            _mockPrescriptionRepository.Verify(repo => repo.GetPrescriptionItemsForVisitAsync(visitId, null, null), Times.Once);
        }


        [Fact]
        public async Task SendPrescriptionToPharmacyAsync_WhenRepositorySucceeds_ShouldReturnTrue()
        {
            // Arrange
            int visitId = 1;
            int sentByUserId = 101;
            _mockPrescriptionRepository
                .Setup(repo => repo.SendPrescriptionToPharmacyAsync(visitId, sentByUserId, null, null))
                .ReturnsAsync(true);

            // Act
            bool result = await _prescriptionService.SendPrescriptionToPharmacyAsync(visitId, sentByUserId);

            // Assert
            Assert.True(result);
            _mockPrescriptionRepository.Verify(repo => repo.SendPrescriptionToPharmacyAsync(visitId, sentByUserId, null, null), Times.Once);
        }

        [Fact]
        public async Task SendPrescriptionToPharmacyAsync_WhenRepositoryFails_ShouldReturnFalse()
        {
            // Arrange
            int visitId = 2;
            int sentByUserId = 102;
            _mockPrescriptionRepository
                .Setup(repo => repo.SendPrescriptionToPharmacyAsync(visitId, sentByUserId, null, null))
                .ReturnsAsync(false);

            // Act
            bool result = await _prescriptionService.SendPrescriptionToPharmacyAsync(visitId, sentByUserId);

            // Assert
            Assert.False(result);
            _mockPrescriptionRepository.Verify(repo => repo.SendPrescriptionToPharmacyAsync(visitId, sentByUserId, null, null), Times.Once);
        }

        [Fact]
        public async Task SearchMedicationsAsync_WithShortSearchTerm_ShouldReturnEmpty()
        {
            // Arrange
            string shortSearchTerm = "A";

            // Act
            var result = await _prescriptionService.SearchMedicationsAsync(shortSearchTerm);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockMedicationRepository.Verify(repo => repo.SearchMedicationsAsync(It.IsAny<string>(), It.IsAny<int>(), null, null), Times.Never);
        }
    }
}