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
using carestream.core.dtos.pharmacy;
using carestream.core.dtos.prescription;
using carestream.core.dtos.user;
using carestream.core.dtos.shared; // Ensure this using is present

namespace carestream.tests.unit.services
{
    public class PharmacyServiceTests
    {
        private readonly Mock<IPrescriptionRepository> _mockPrescriptionRepository;
        private readonly Mock<IDispensationRepository> _mockDispensationRepository;
        private readonly Mock<ILogger<PharmacyService>> _mockLogger;
        private readonly IPharmacyService _pharmacyService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IPasswordHasherService> _mockPasswordHasherService;
        private readonly Mock<IMedicationRepository> _mockMedicationRepository;

        private const int DefaultPharmacistUserId = 101;
        private const string DefaultVerificationCode = "1234";
        private readonly UserVerificationCodeInfo _defaultPharmacistVerificationInfo =
            new UserVerificationCodeInfo { HashedVerificationCode = "hashedCode", VerificationCodeSalt = "salt" };

        public PharmacyServiceTests()
        {
            _mockPrescriptionRepository = new Mock<IPrescriptionRepository>();
            _mockDispensationRepository = new Mock<IDispensationRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockPasswordHasherService = new Mock<IPasswordHasherService>();
            _mockLogger = new Mock<ILogger<PharmacyService>>();
            _mockMedicationRepository = new Mock<IMedicationRepository>();

            _pharmacyService = new PharmacyService(
                _mockPrescriptionRepository.Object,
                _mockLogger.Object,
                _mockDispensationRepository.Object,
                _mockUserRepository.Object,
                _mockPasswordHasherService.Object,
                _mockMedicationRepository.Object
            );
        }

        [Fact]
        public async Task GetDashboardViewModelAsync_ShouldCallRepositoryMethodsAndAssembleViewModel()
        {
            int pageNumber = 1; int pageSize = 10; int offset = 0;
            var expectedStats = new PharmacistDashboardStatsDto { PendingPrescriptionsCount = 5 };
            var expectedPendingList = new List<PendingPrescriptionSummaryDto> { new PendingPrescriptionSummaryDto { VisitId = 1, PatientName = "John Doe" } };
            _mockPrescriptionRepository.Setup(repo => repo.GetPharmacistDashboardStatsAsync(null, null)).ReturnsAsync(expectedStats);
            _mockPrescriptionRepository.Setup(repo => repo.GetPendingPrescriptionsSummaryAsync(pageSize, offset, null, null)).ReturnsAsync(expectedPendingList);
            var viewModel = await _pharmacyService.GetDashboardViewModelAsync(pageNumber, pageSize);
            Assert.NotNull(viewModel); Assert.Same(expectedStats, viewModel.Stats); Assert.Equal(expectedPendingList.Count, viewModel.PendingPrescriptions.Count);
            _mockPrescriptionRepository.VerifyAll();
        }

        [Fact]
        public async Task GetDashboardViewModelAsync_WhenRepositoryReturnsNull_ShouldReturnViewModelWithDefaults()
        {
            // Arrange
            _mockPrescriptionRepository
                .Setup(repo => repo.GetPharmacistDashboardStatsAsync(null, null))!
                .ReturnsAsync((PharmacistDashboardStatsDto?)null); // Simulate repo returning null for stats
            _mockPrescriptionRepository
                .Setup(repo => repo.GetPendingPrescriptionsSummaryAsync(It.IsAny<int>(), It.IsAny<int>(), null, null))!
                .ReturnsAsync((IEnumerable<PendingPrescriptionSummaryDto>?)null); // Simulate repo returning null for list

            // Act
            var viewModel = await _pharmacyService.GetDashboardViewModelAsync();

            // Assert
            Assert.NotNull(viewModel);
            Assert.NotNull(viewModel.Stats); // Service should initialize a default Stats DTO
            Assert.Equal(0, viewModel.Stats.PendingPrescriptionsCount); // Check default value

            Assert.NotNull(viewModel.PendingPrescriptions);
            Assert.Empty(viewModel.PendingPrescriptions); // Service should initialize an empty list
        }

        [Theory]
        [InlineData(0, 10)]  // pageNumber < 1
        [InlineData(1, 0)]   // pageSize < 1
        [InlineData(-1, 5)]  // pageNumber < 1
        [InlineData(2, -5)]  // pageSize < 1
        public async Task GetDashboardViewModelAsync_WithInvalidPageParameters_ShouldUseDefaultsForRepositoryCall(int pageNumber, int pageSize)
        {
            // Arrange
            int expectedPageSize = pageSize < 1 ? 25 : pageSize; // Default pageSize in service is 25
            int expectedOffset = (pageNumber < 1 ? 1 : pageNumber - 1) * expectedPageSize;
            if (pageNumber < 1 && pageSize < 1) expectedOffset = 0 * 25; // Special case if both are invalid
            else if (pageNumber < 1) expectedOffset = 0 * expectedPageSize;
            else if (pageSize < 1) expectedOffset = (pageNumber - 1) * 25;


            _mockPrescriptionRepository
                .Setup(repo => repo.GetPharmacistDashboardStatsAsync(null, null))
                .ReturnsAsync(new PharmacistDashboardStatsDto());
            _mockPrescriptionRepository
                .Setup(repo => repo.GetPendingPrescriptionsSummaryAsync(expectedPageSize, expectedOffset, null, null))
                .ReturnsAsync(new List<PendingPrescriptionSummaryDto>());

            // Act
            await _pharmacyService.GetDashboardViewModelAsync(pageNumber, pageSize);

            // Assert
            _mockPrescriptionRepository.Verify(repo => repo.GetPendingPrescriptionsSummaryAsync(expectedPageSize, expectedOffset, null, null), Times.Once);
        }

        [Fact]
        public async Task GetPrescriptionDetailsAsync_ValidVisitId_RepositoryReturnsData_ShouldReturnViewModel()
        {
            int visitId = 1;
            var expectedHeader = new PrescriptionDetailHeaderDto { VisitId = visitId };
            var expectedItems = new List<PrescriptionDetailItemDto> { new PrescriptionDetailItemDto() };
            _mockPrescriptionRepository.Setup(repo => repo.GetPrescriptionDetailHeaderAsync(visitId, null, null)).ReturnsAsync(expectedHeader);
            _mockPrescriptionRepository.Setup(repo => repo.GetPrescriptionDetailItemsAsync(visitId, null, null)).ReturnsAsync(expectedItems);
            var resultViewModel = await _pharmacyService.GetPrescriptionDetailsAsync(visitId);
            Assert.NotNull(resultViewModel); Assert.Same(expectedHeader, resultViewModel.Header); Assert.Equal(expectedItems.Count, resultViewModel.Items.Count);
            _mockPrescriptionRepository.VerifyAll();
        }

        [Fact]
        public async Task GetPrescriptionDetailsAsync_WhenHeaderIsNull_ShouldReturnNull()
        {
            // Arrange
            int visitId = 2;
            // Simulate repository returning null for header (e.g., visit not found or not sent to pharmacy)
            _mockPrescriptionRepository
                .Setup(repo => repo.GetPrescriptionDetailHeaderAsync(visitId, null, null))
                .ReturnsAsync((PrescriptionDetailHeaderDto?)null);
            // Items mock doesn't matter much here as it should short-circuit, but set it up defensively
            _mockPrescriptionRepository
                .Setup(repo => repo.GetPrescriptionDetailItemsAsync(visitId, null, null))
                .ReturnsAsync(new List<PrescriptionDetailItemDto>());


            // Act
            var resultViewModel = await _pharmacyService.GetPrescriptionDetailsAsync(visitId);

            // Assert
            Assert.Null(resultViewModel); // Service should return null if header is null

            _mockPrescriptionRepository.Verify(repo => repo.GetPrescriptionDetailHeaderAsync(visitId, null, null), Times.Once);
            // GetPrescriptionDetailItemsAsync might still be called due to Task.WhenAll, but its result isn't used if header is null.
            // Or, if service logic is refined to not call itemsTask if headerTask is null, then verify Times.Never for itemsTask.
            // Current service implementation with Task.WhenAll will execute both.
            _mockPrescriptionRepository.Verify(repo => repo.GetPrescriptionDetailItemsAsync(visitId, null, null), Times.Once);
        }

        [Fact]
        public async Task GetPrescriptionDetailsAsync_WhenItemsAreNullOrEmpty_ShouldReturnViewModelWithEmptyItemsList()
        {
            // Arrange
            int visitId = 3;
            var expectedHeader = new PrescriptionDetailHeaderDto { VisitId = visitId, PatientName = "Jane Doe" };

            _mockPrescriptionRepository
                .Setup(repo => repo.GetPrescriptionDetailHeaderAsync(visitId, null, null))
                .ReturnsAsync(expectedHeader);
            _mockPrescriptionRepository
                .Setup(repo => repo.GetPrescriptionDetailItemsAsync(visitId, null, null))!
                .ReturnsAsync((IEnumerable<PrescriptionDetailItemDto>?)null); // Simulate repo returning null for items

            // Act
            var resultViewModelNullItems = await _pharmacyService.GetPrescriptionDetailsAsync(visitId);

            // Arrange for empty list
            _mockPrescriptionRepository
                .Setup(repo => repo.GetPrescriptionDetailItemsAsync(visitId, null, null))
                .ReturnsAsync(new List<PrescriptionDetailItemDto>()); // Simulate repo returning empty list for items

            var resultViewModelEmptyItems = await _pharmacyService.GetPrescriptionDetailsAsync(visitId);


            // Assert for null items
            Assert.NotNull(resultViewModelNullItems);
            Assert.Same(expectedHeader, resultViewModelNullItems.Header);
            Assert.NotNull(resultViewModelNullItems.Items);
            Assert.Empty(resultViewModelNullItems.Items); // Service should convert null from repo to empty list

            // Assert for empty items
            Assert.NotNull(resultViewModelEmptyItems);
            Assert.Same(expectedHeader, resultViewModelEmptyItems.Header);
            Assert.NotNull(resultViewModelEmptyItems.Items);
            Assert.Empty(resultViewModelEmptyItems.Items);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetPrescriptionDetailsAsync_WithInvalidVisitId_ShouldReturnNullAndNotCallRepository(int invalidVisitId)
        {
            // Act
            var resultViewModel = await _pharmacyService.GetPrescriptionDetailsAsync(invalidVisitId);

            // Assert
            Assert.Null(resultViewModel);
            _mockPrescriptionRepository.Verify(repo => repo.GetPrescriptionDetailHeaderAsync(It.IsAny<int>(), null, null), Times.Never);
            _mockPrescriptionRepository.Verify(repo => repo.GetPrescriptionDetailItemsAsync(It.IsAny<int>(), null, null), Times.Never);
        }

        private StartDispenseViewModel CreateValidDispenseInput(int visitId, List<DispenseItemDto> items)
        {
            return new StartDispenseViewModel
            {
                VisitId = visitId,
                PatientName = "Test Patient",
                PrescriptionIdentifier = $"Rx-{visitId}",
                ItemsToDispense = items
            };
        }

        [Fact]
        public async Task ProcessDispenseAsync_NoItemsSelected_ShouldReturnInformationalConfirmation()
        {
            // Arrange
            var input = CreateDispenseInput(1, new List<DispenseItemDto> {
                new DispenseItemDto { PrescriptionItemId = 1, IsSelectedForDispense = false, QuantityToDispense = "10" }
            });
            // This test is specifically for the "no items selected" path AFTER successful verification
            SetupSuccessfulVerificationMock(); // Use the helper for default pharmacist and code

            // Act
            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.OverallSuccess); // Should be true as per service logic for this case
            Assert.Null(result.ErrorMessage);
            Assert.Equal("No items were selected for dispensing.", result.NextStepMessage);
            Assert.Empty(result.DispensedItems);

            _mockDispensationRepository.Verify(repo => repo.LogDispenseActionAsync(It.IsAny<DispenseLogEntryInputDto>(), null, null), Times.Never);
            _mockPrescriptionRepository.Verify(repo => repo.UpdatePrescriptionItemDispenseStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), null, null), Times.Never);
        }

        private void SetupSuccessfulVerification(int pharmacistUserId, string verificationCode)
        {
            _mockUserRepository.Setup(r => r.GetUserVerificationCodeInfoAsync(pharmacistUserId, null, null))
                               .ReturnsAsync(_defaultPharmacistVerificationInfo);
            _mockPasswordHasherService.Setup(h => h.VerifyPassword(verificationCode, _defaultPharmacistVerificationInfo.HashedVerificationCode!, _defaultPharmacistVerificationInfo.VerificationCodeSalt!))
                                      .Returns(true);
        }

        // Helper to set up successful verification for the DefaultPharmacistUserId and DefaultVerificationCode
        private void SetupSuccessfulVerificationMock()
        {
            _mockUserRepository.Setup(r => r.GetUserVerificationCodeInfoAsync(DefaultPharmacistUserId, null, null))
                               .ReturnsAsync(_defaultPharmacistVerificationInfo);
            _mockPasswordHasherService.Setup(h => h.VerifyPassword(DefaultVerificationCode, _defaultPharmacistVerificationInfo.HashedVerificationCode!, _defaultPharmacistVerificationInfo.VerificationCodeSalt!))
                                      .Returns(true);
        }

        // Helper to create dispense input with a default verification code
        private StartDispenseViewModel CreateDispenseInput(int visitId, List<DispenseItemDto> items, string? verificationCode = DefaultVerificationCode)
        {
            return new StartDispenseViewModel
            {
                VisitId = visitId,
                PatientName = "Test Patient",
                PrescriptionIdentifier = $"Rx-{visitId}",
                ItemsToDispense = items,
                PharmacistVerificationCode = verificationCode
            };
        }

        [Fact]
        public async Task ProcessDispenseAsync_VerificationCodeMissing_ShouldReturnError()
        {
            var input = CreateDispenseInput(1, new List<DispenseItemDto> { new DispenseItemDto { IsSelectedForDispense = true, QuantityToDispense = "1" } }, verificationCode: "");
            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);
            Assert.False(result.OverallSuccess); Assert.Equal("Pharmacist verification code is required.", result.ErrorMessage);
        }

        [Fact]
        public async Task ProcessDispenseAsync_UserVerificationInfoNotFound_ShouldReturnError()
        {
            var input = CreateDispenseInput(1, new List<DispenseItemDto> { new DispenseItemDto { IsSelectedForDispense = true, QuantityToDispense = "1" } });
            _mockUserRepository.Setup(r => r.GetUserVerificationCodeInfoAsync(DefaultPharmacistUserId, null, null)).ReturnsAsync((UserVerificationCodeInfo?)null);
            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);
            Assert.False(result.OverallSuccess); Assert.Equal("Pharmacist verification setup is incomplete. Contact administrator.", result.ErrorMessage);
        }

        [Fact]
        public async Task ProcessDispenseAsync_IncorrectVerificationCode_ShouldReturnError()
        {
            var input = CreateDispenseInput(1, new List<DispenseItemDto> { new DispenseItemDto { IsSelectedForDispense = true, QuantityToDispense = "1" } }, verificationCode: "wrongCode");
            _mockUserRepository.Setup(r => r.GetUserVerificationCodeInfoAsync(DefaultPharmacistUserId, null, null)).ReturnsAsync(_defaultPharmacistVerificationInfo);
            _mockPasswordHasherService.Setup(h => h.VerifyPassword("wrongCode", _defaultPharmacistVerificationInfo.HashedVerificationCode!, _defaultPharmacistVerificationInfo.VerificationCodeSalt!)).Returns(false);
            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);
            Assert.False(result.OverallSuccess); Assert.Equal("Invalid pharmacist verification code.", result.ErrorMessage);
        }

        [Fact]
        public async Task ProcessDispenseAsync_ItemAlreadyFullyDispensed_ShouldSkipAndReflectInConfirmationButOverallSuccessTrueIfNoOtherErrors()
        {
            var items = new List<DispenseItemDto> { new DispenseItemDto { PrescriptionItemId = 1, IsSelectedForDispense = true, QuantityToDispense = "1", MedicationName = "MedA", QuantityPrescribed = "10 units" } };
            var input = CreateDispenseInput(1, items);
            SetupSuccessfulVerificationMock();
            var currentInfo = new PrescriptionItemDispenseInfoDto { IsAlreadyFullyDispensed = true, QuantityPrescribed = "10 units", QuantityDispensedSoFar = "10 units" };
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(1, null, null)).ReturnsAsync(currentInfo);

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.True(result.OverallSuccess); // Skipping an already dispensed item is not an error for OverallSuccess
            Assert.Single(result.DispensedItems);
            Assert.Contains(result.DispensedItems, i => i.PrescriptionItemId == 1 && i.Notes != null && i.Notes.Contains("Already fully dispensed."));
        }

        [Fact]
        public async Task ProcessDispenseAsync_QuantityParsingFails_ShouldSetOverallSuccessFalseAndReflectInConfirmation()
        {
            var items = new List<DispenseItemDto> { new DispenseItemDto { PrescriptionItemId = 1, IsSelectedForDispense = true, QuantityToDispense = "abc", QuantityPrescribed = "10 units" } };
            var input = CreateDispenseInput(1, items);
            SetupSuccessfulVerificationMock();
            var currentInfo = new PrescriptionItemDispenseInfoDto { IsAlreadyFullyDispensed = false, QuantityPrescribed = "10 units", QuantityDispensedSoFar = "0 units" };
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(1, null, null)).ReturnsAsync(currentInfo);

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess);
            Assert.Single(result.DispensedItems);
            Assert.Contains(result.DispensedItems, i => i.PrescriptionItemId == 1 && i.Notes != null && i.Notes.Contains("Error: Invalid quantity format for processing."));
        }

        [Fact]
        public async Task ProcessDispenseAsync_OverDispenseAttempt_ShouldSetOverallSuccessFalseAndReflectInConfirmation()
        {
            var items = new List<DispenseItemDto> { new DispenseItemDto { PrescriptionItemId = 1, IsSelectedForDispense = true, QuantityToDispense = "15 tablets", QuantityPrescribed = "10 tablets" } };
            var input = CreateDispenseInput(1, items);
            SetupSuccessfulVerificationMock();
            var currentInfo = new PrescriptionItemDispenseInfoDto { IsAlreadyFullyDispensed = false, QuantityPrescribed = "10 tablets", QuantityDispensedSoFar = "0 tablets" };
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(1, null, null)).ReturnsAsync(currentInfo);

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess);
            Assert.Single(result.DispensedItems);
            Assert.Contains(result.DispensedItems, i => i.PrescriptionItemId == 1 && i.Notes != null && i.Notes.Contains("Error: Dispense quantity exceeds remaining prescribed amount."));
        }

        [Fact]
        public async Task ProcessDispenseAsync_LogFails_ShouldSetOverallSuccessFalseAndNotUpdateStatus()
        {
            var itemToDispense = new DispenseItemDto { PrescriptionItemId = 1, MedicationId = 101, MedicationName = "MedX", QuantityPrescribed = "10 units", IsSelectedForDispense = true, QuantityToDispense = "10 units" };
            var input = CreateDispenseInput(1, new List<DispenseItemDto> { itemToDispense });
            SetupSuccessfulVerificationMock();
            var currentInfo = new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "10 units", QuantityDispensedSoFar = "0 units", IsAlreadyFullyDispensed = false };
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(itemToDispense.PrescriptionItemId, null, null)).ReturnsAsync(currentInfo);
            _mockDispensationRepository.Setup(r => r.LogDispenseActionAsync(It.IsAny<DispenseLogEntryInputDto>(), null, null)).ReturnsAsync(0); // Log fails

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess);
            Assert.Single(result.DispensedItems);
            Assert.Contains("Error: Failed to log this dispense action.", result.DispensedItems.First().Notes);
            _mockPrescriptionRepository.Verify(r => r.UpdatePrescriptionItemDispenseStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), null, null), Times.Never);
        }

        [Fact]
        public async Task ProcessDispenseAsync_UpdateStatusFailsAfterLog_ShouldSetOverallSuccessFalse()
        {
            var itemToDispense = new DispenseItemDto { PrescriptionItemId = 1, MedicationId = 101, MedicationName = "MedX", QuantityPrescribed = "10 units", IsSelectedForDispense = true, QuantityToDispense = "10 units" };
            var input = CreateDispenseInput(1, new List<DispenseItemDto> { itemToDispense });
            SetupSuccessfulVerificationMock();
            var currentInfo = new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "10 units", QuantityDispensedSoFar = "0 units", IsAlreadyFullyDispensed = false };
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(itemToDispense.PrescriptionItemId, null, null)).ReturnsAsync(currentInfo);
            _mockDispensationRepository.Setup(r => r.LogDispenseActionAsync(It.IsAny<DispenseLogEntryInputDto>(), null, null)).ReturnsAsync(1); // Log succeeds
            _mockPrescriptionRepository.Setup(r => r.UpdatePrescriptionItemDispenseStatusAsync(itemToDispense.PrescriptionItemId, "10 units", true, DefaultPharmacistUserId, null, null)).ReturnsAsync(false); // Update fails

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess);
            Assert.Single(result.DispensedItems);
            Assert.Contains("Error: Failed to update prescription item status after logging dispense.", result.DispensedItems.First().Notes);
        }

        [Fact]
        public async Task ProcessDispenseAsync_ItemNotFoundInRepo_ShouldSetOverallSuccessFalseAndReflectInConfirmation()
        {
            var items = new List<DispenseItemDto> { new DispenseItemDto { PrescriptionItemId = 1, IsSelectedForDispense = true, QuantityToDispense = "1 unit", QuantityPrescribed = "1 unit" } };
            var input = CreateDispenseInput(1, items);
            SetupSuccessfulVerificationMock();
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(1, null, null)).ReturnsAsync((PrescriptionItemDispenseInfoDto?)null);

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess);
            Assert.Single(result.DispensedItems);
            Assert.Contains(result.DispensedItems, i => i.PrescriptionItemId == 1 && i.Notes != null && i.Notes.Contains("Error: Original prescription item not found."));
        }

        [Fact]
        public async Task ProcessDispenseAsync_ItemWithEmptyQuantityToDispense_ShouldSkipItemAndSetOverallSuccessFalse()
        {
            var items = new List<DispenseItemDto> {
                new DispenseItemDto { PrescriptionItemId = 1, MedicationName = "Med A", IsSelectedForDispense = true, QuantityToDispense = "" },
                new DispenseItemDto { PrescriptionItemId = 2, MedicationName = "Med B", IsSelectedForDispense = true, QuantityToDispense = "5 units", QuantityPrescribed="5 units", MedicationId = 102 }
            };
            var input = CreateDispenseInput(1, items);
            SetupSuccessfulVerificationMock();
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(2, null, null))
                .ReturnsAsync(new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "5 units", QuantityDispensedSoFar = "0 units", IsAlreadyFullyDispensed = false });
            _mockDispensationRepository.Setup(r => r.LogDispenseActionAsync(It.Is<DispenseLogEntryInputDto>(d => d.PrescriptionItemId == 2), null, null)).ReturnsAsync(1);
            _mockPrescriptionRepository.Setup(r => r.UpdatePrescriptionItemDispenseStatusAsync(2, "5 units", true, DefaultPharmacistUserId, null, null)).ReturnsAsync(true);

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess); // OverallSuccess is false because one item was skipped
            Assert.Equal(2, result.DispensedItems.Count);
            Assert.Contains(result.DispensedItems, i => i.PrescriptionItemId == 1 && i.Notes != null && i.Notes.Contains("Skipped: No dispense quantity entered."));
            Assert.Contains(result.DispensedItems, i => i.PrescriptionItemId == 2 && i.Notes != null && i.Notes.Contains("Fully dispensed."));
        }

        [Fact]
        public async Task ProcessDispenseAsync_ItemWithEmptyQuantityToDispense_ShouldSkipItemAndMarkOverallFailure()
        {
            var items = new List<DispenseItemDto> {
                new DispenseItemDto { PrescriptionItemId = 1, MedicationName = "Med A", IsSelectedForDispense = true, QuantityToDispense = "" },
                new DispenseItemDto { PrescriptionItemId = 2, MedicationName = "Med B", IsSelectedForDispense = true, QuantityToDispense = "5 units", QuantityPrescribed="5 units", MedicationId = 102 }
            };
            var input = CreateDispenseInput(1, items);
            SetupSuccessfulVerification(DefaultPharmacistUserId, input.PharmacistVerificationCode!);
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(2, null, null))
                .ReturnsAsync(new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "5 units", QuantityDispensedSoFar = "0 units", IsAlreadyFullyDispensed = false });
            _mockDispensationRepository.Setup(r => r.LogDispenseActionAsync(It.Is<DispenseLogEntryInputDto>(d => d.PrescriptionItemId == 2), null, null)).ReturnsAsync(1);
            _mockPrescriptionRepository.Setup(r => r.UpdatePrescriptionItemDispenseStatusAsync(2, "5 units", true, DefaultPharmacistUserId, null, null)).ReturnsAsync(true);

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess);
            Assert.Equal(2, result.DispensedItems.Count);
            Assert.Contains(result.DispensedItems, i => i.PrescriptionItemId == 1 && i.Notes != null && i.Notes.Contains("Skipped: No dispense quantity entered."));
            Assert.Contains(result.DispensedItems, i => i.PrescriptionItemId == 2 && i.Notes != null && i.Notes.Contains("Fully dispensed."));
        }

        [Fact]
        public async Task ProcessDispenseAsync_LogActionFails_ShouldMarkItemAsFailedAndOverallFailure()
        {
            var items = new List<DispenseItemDto> { new DispenseItemDto { PrescriptionItemId = 1, MedicationId = 101, IsSelectedForDispense = true, QuantityToDispense = "10 units", QuantityPrescribed = "10 units" } };
            var input = CreateDispenseInput(1, items);
            SetupSuccessfulVerification(DefaultPharmacistUserId, input.PharmacistVerificationCode!);
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(1, null, null)).ReturnsAsync(new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "10 units", IsAlreadyFullyDispensed = false });
            _mockDispensationRepository.Setup(repo => repo.LogDispenseActionAsync(It.IsAny<DispenseLogEntryInputDto>(), null, null)).ReturnsAsync(0); // Log failure

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess);
            Assert.Single(result.DispensedItems);
            Assert.Contains("Error: Failed to log this dispense action.", result.DispensedItems[0].Notes);
            _mockPrescriptionRepository.Verify(repo => repo.UpdatePrescriptionItemDispenseStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), DefaultPharmacistUserId, null, null), Times.Never);
        }

        [Fact]
        public async Task ProcessDispenseAsync_UpdateStatusFailsAfterLog_ShouldReflectInConfirmation()
        {
            var items = new List<DispenseItemDto> { new DispenseItemDto { PrescriptionItemId = 1, MedicationId = 101, MedicationName = "MedX", QuantityPrescribed = "10 units", IsSelectedForDispense = true, QuantityToDispense = "10 units" } };
            var input = CreateDispenseInput(1, items);
            SetupSuccessfulVerification(DefaultPharmacistUserId, input.PharmacistVerificationCode!);
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(1, null, null)).ReturnsAsync(new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "10 units", IsAlreadyFullyDispensed = false });
            _mockDispensationRepository.Setup(repo => repo.LogDispenseActionAsync(It.IsAny<DispenseLogEntryInputDto>(), null, null)).ReturnsAsync(1); // Log success
            _mockPrescriptionRepository.Setup(repo => repo.UpdatePrescriptionItemDispenseStatusAsync(1, "10 units", true, DefaultPharmacistUserId, null, null)).ReturnsAsync(false); // Update fails

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess);
            Assert.Single(result.DispensedItems);
            Assert.Contains("Error: Failed to update prescription item status after logging dispense.", result.DispensedItems[0].Notes);
        }

        [Fact]
        public async Task ProcessDispenseAsync_LogFails_ShouldSetOverallSuccessFalseAndReflectInConfirmation() // Renamed slightly
        {
            // Arrange
            var itemToDispense = new DispenseItemDto { PrescriptionItemId = 1, MedicationId = 101, MedicationName = "MedX", QuantityPrescribed = "10 units", IsSelectedForDispense = true, QuantityToDispense = "10 units" };
            var input = CreateDispenseInput(1, new List<DispenseItemDto> { itemToDispense });
            SetupSuccessfulVerificationMock(); // Ensure verification passes

            // Mock GetPrescriptionItemCurrentDispenseInfoAsync to allow processing to reach the log step
            var currentInfo = new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "10 units", QuantityDispensedSoFar = "0 units", IsAlreadyFullyDispensed = false };
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(itemToDispense.PrescriptionItemId, null, null)).ReturnsAsync(currentInfo);

            _mockDispensationRepository.Setup(r => r.LogDispenseActionAsync(It.IsAny<DispenseLogEntryInputDto>(), null, null)).ReturnsAsync(0); // Log fails

            // Act
            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            // Assert
            Assert.False(result.OverallSuccess);
            Assert.Single(result.DispensedItems); // An item confirmation should be added
            Assert.NotNull(result.DispensedItems[0].Notes);
            Assert.Contains("Error: Failed to log this dispense action.", result.DispensedItems[0].Notes);
            _mockPrescriptionRepository.Verify(r => r.UpdatePrescriptionItemDispenseStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GetStartDispenseViewModelAsync_ValidVisitId_ShouldPopulateViewModelWithStock()
        {
            // Arrange
            int visitId = 1;
            var headerDto = new PrescriptionDetailHeaderDto { VisitId = visitId, PatientName = "Test Patient", PrescriptionIdentifier = "Rx-1" };
            var itemsFromRepo = new List<DispenseItemDto>
            {
                new DispenseItemDto { PrescriptionItemId = 10, MedicationId = 1001, MedicationName = "MedA", QuantityPrescribed = "10" },
                new DispenseItemDto { PrescriptionItemId = 11, MedicationId = 1002, MedicationName = "MedB", QuantityPrescribed = "20" }
            };
            int stockForMedA = 50;
            int stockForMedB = 5; // Low stock example

            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionDetailHeaderAsync(visitId, null, null)).ReturnsAsync(headerDto);
            _mockPrescriptionRepository.Setup(r => r.GetItemsForDispensingAsync(visitId, null, null)).ReturnsAsync(itemsFromRepo);

            _mockMedicationRepository.Setup(r => r.GetStockOnHandAsync(1001, null, null)).ReturnsAsync(stockForMedA);
            _mockMedicationRepository.Setup(r => r.GetStockOnHandAsync(1002, null, null)).ReturnsAsync(stockForMedB);
            _mockMedicationRepository.Setup(r => r.GetStockOnHandAsync(It.IsNotIn(1001, 1002), null, null)).ReturnsAsync((int?)null); // Default for others


            // Act
            var result = await _pharmacyService.GetStartDispenseViewModelAsync(visitId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(visitId, result.VisitId);
            Assert.Equal(headerDto.PatientName, result.PatientName);
            Assert.Equal(headerDto.PrescriptionIdentifier, result.PrescriptionIdentifier);

            Assert.NotNull(result.ItemsToDispense);
            Assert.Equal(2, result.ItemsToDispense.Count);

            var itemA = result.ItemsToDispense.FirstOrDefault(i => i.MedicationId == 1001);
            Assert.NotNull(itemA);
            Assert.Equal(stockForMedA, itemA.StockOnHand);
            Assert.Equal(itemA.QuantityPrescribed, itemA.QuantityToDispense); // Check default
            Assert.True(itemA.IsSelectedForDispense); // Check default

            var itemB = result.ItemsToDispense.FirstOrDefault(i => i.MedicationId == 1002);
            Assert.NotNull(itemB);
            Assert.Equal(stockForMedB, itemB.StockOnHand);

            _mockPrescriptionRepository.Verify(r => r.GetPrescriptionDetailHeaderAsync(visitId, null, null), Times.Once);
            _mockPrescriptionRepository.Verify(r => r.GetItemsForDispensingAsync(visitId, null, null), Times.Once);
            _mockMedicationRepository.Verify(r => r.GetStockOnHandAsync(1001, null, null), Times.Once);
            _mockMedicationRepository.Verify(r => r.GetStockOnHandAsync(1002, null, null), Times.Once);
        }

        [Fact]
        public async Task GetStartDispenseViewModelAsync_HeaderNotFound_ShouldReturnNull()
        {
            // Arrange
            int visitId = 2;
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionDetailHeaderAsync(visitId, null, null)).ReturnsAsync((PrescriptionDetailHeaderDto?)null);
            // Items mock doesn't matter much as it should return null early
            _mockPrescriptionRepository.Setup(r => r.GetItemsForDispensingAsync(visitId, null, null)).ReturnsAsync(new List<DispenseItemDto>());


            // Act
            var result = await _pharmacyService.GetStartDispenseViewModelAsync(visitId);

            // Assert
            Assert.Null(result);
            _mockMedicationRepository.Verify(r => r.GetStockOnHandAsync(It.IsAny<int>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GetStartDispenseViewModelAsync_NoItemsToDispense_ShouldReturnViewModelWithEmptyItemsList()
        {
            // Arrange
            int visitId = 3;
            var headerDto = new PrescriptionDetailHeaderDto { VisitId = visitId, PatientName = "Test Patient B", PrescriptionIdentifier = "Rx-3" };
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionDetailHeaderAsync(visitId, null, null)).ReturnsAsync(headerDto);
            _mockPrescriptionRepository.Setup(r => r.GetItemsForDispensingAsync(visitId, null, null)).ReturnsAsync(new List<DispenseItemDto>()); // Empty list

            // Act
            var result = await _pharmacyService.GetStartDispenseViewModelAsync(visitId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(visitId, result.VisitId);
            Assert.NotNull(result.ItemsToDispense);
            Assert.Empty(result.ItemsToDispense);
            _mockMedicationRepository.Verify(r => r.GetStockOnHandAsync(It.IsAny<int>(), null, null), Times.Never); // No items, so no stock lookups
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetStartDispenseViewModelAsync_InvalidVisitId_ShouldReturnNull(int invalidVisitId)
        {
            // Act
            var result = await _pharmacyService.GetStartDispenseViewModelAsync(invalidVisitId);

            // Assert
            Assert.Null(result);
            _mockPrescriptionRepository.Verify(r => r.GetPrescriptionDetailHeaderAsync(It.IsAny<int>(), null, null), Times.Never);
            _mockPrescriptionRepository.Verify(r => r.GetItemsForDispensingAsync(It.IsAny<int>(), null, null), Times.Never);
            _mockMedicationRepository.Verify(r => r.GetStockOnHandAsync(It.IsAny<int>(), null, null), Times.Never);
        }

        [Fact]
        public async Task ProcessDispenseAsync_AllItemsSuccessful_IncludingStockDecrement_ShouldReturnSuccessConfirmation()
        {
            // Arrange
            var itemsToDispense = new List<DispenseItemDto> {
                new DispenseItemDto { PrescriptionItemId = 1, MedicationId = 101, MedicationName="Med1", QuantityPrescribed="10 units", IsSelectedForDispense = true, QuantityToDispense = "10 units" },
                new DispenseItemDto { PrescriptionItemId = 2, MedicationId = 102, MedicationName="Med2", QuantityPrescribed="5 units", IsSelectedForDispense = true, QuantityToDispense = "5 units" }
            };
            var input = CreateDispenseInput(1, itemsToDispense);
            SetupSuccessfulVerificationMock();

            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(1, null, null)).ReturnsAsync(new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "10 units", IsAlreadyFullyDispensed = false });
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(2, null, null)).ReturnsAsync(new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "5 units", IsAlreadyFullyDispensed = false });

            _mockMedicationRepository.Setup(r => r.DecrementStockAsync(101, 10, null, null)).ReturnsAsync(true); // Stock success for Med1
            _mockMedicationRepository.Setup(r => r.DecrementStockAsync(102, 5, null, null)).ReturnsAsync(true);  // Stock success for Med2

            _mockDispensationRepository.Setup(repo => repo.LogDispenseActionAsync(It.Is<DispenseLogEntryInputDto>(d => d.PrescriptionItemId == 1), null, null)).ReturnsAsync(1);
            _mockDispensationRepository.Setup(repo => repo.LogDispenseActionAsync(It.Is<DispenseLogEntryInputDto>(d => d.PrescriptionItemId == 2), null, null)).ReturnsAsync(2);
            _mockPrescriptionRepository.Setup(repo => repo.UpdatePrescriptionItemDispenseStatusAsync(1, "10 units", true, DefaultPharmacistUserId, null, null)).ReturnsAsync(true);
            _mockPrescriptionRepository.Setup(repo => repo.UpdatePrescriptionItemDispenseStatusAsync(2, "5 units", true, DefaultPharmacistUserId, null, null)).ReturnsAsync(true);

            // Act
            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            // Assert
            Assert.True(result.OverallSuccess, result.ErrorMessage ?? "OverallSuccess was false. Error: " + result.ErrorMessage + " Notes: " + string.Join(" | ", result.DispensedItems.Select(i => i.Notes)));
            Assert.Null(result.ErrorMessage);
            Assert.Equal(2, result.DispensedItems.Count);
            Assert.All(result.DispensedItems, item => Assert.True(item.Notes?.Contains("Fully dispensed."), $"Item {item.PrescriptionItemId} notes: {item.Notes}")); // Updated to specific success message
            Assert.All(result.DispensedItems, item => Assert.True(item.IsFullyDispensedNow));
            _mockMedicationRepository.Verify(r => r.DecrementStockAsync(It.IsAny<int>(), It.IsAny<int>(), null, null), Times.Exactly(2));
        }


        [Fact]
        public async Task ProcessDispenseAsync_StockDecrementFailsForItem_ShouldSetOverallSuccessFalseAndNoteItem()
        {
            // Arrange
            var itemToDispense = new DispenseItemDto { PrescriptionItemId = 1, MedicationId = 101, MedicationName = "MedX", QuantityPrescribed = "10 units", IsSelectedForDispense = true, QuantityToDispense = "10 units" };
            var input = CreateDispenseInput(1, new List<DispenseItemDto> { itemToDispense });
            SetupSuccessfulVerificationMock();
            var currentInfo = new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "10 units", QuantityDispensedSoFar = "0 units", IsAlreadyFullyDispensed = false };
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(itemToDispense.PrescriptionItemId, null, null)).ReturnsAsync(currentInfo);

            _mockMedicationRepository.Setup(r => r.DecrementStockAsync(itemToDispense.MedicationId, 10, null, null))
                                     .ReturnsAsync(false); // Simulate stock decrement failure

            _mockDispensationRepository.Setup(r => r.LogDispenseActionAsync(It.IsAny<DispenseLogEntryInputDto>(), null, null)).ReturnsAsync(1); // Log still happens
            _mockPrescriptionRepository.Setup(r => r.UpdatePrescriptionItemDispenseStatusAsync(itemToDispense.PrescriptionItemId, "10 units", true, DefaultPharmacistUserId, null, null)).ReturnsAsync(true); // Status update still happens

            // Act
            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            // Assert
            Assert.False(result.OverallSuccess); // Because stock decrement failed
            Assert.Single(result.DispensedItems);
            var dispensedItemConfirm = result.DispensedItems.First();
            Assert.NotNull(dispensedItemConfirm.Notes);
            Assert.Contains("Fully dispensed.", dispensedItemConfirm.Notes);
            Assert.True(dispensedItemConfirm.IsFullyDispensedNow); // Item is still considered dispensed in terms of prescription status
            Assert.Equal("10 units", dispensedItemConfirm.TotalQuantityDispensedSoFar);

            _mockMedicationRepository.Verify(r => r.DecrementStockAsync(itemToDispense.MedicationId, 10, null, null), Times.Once);
            _mockDispensationRepository.Verify(r => r.LogDispenseActionAsync(It.IsAny<DispenseLogEntryInputDto>(), null, null), Times.Once);
            _mockPrescriptionRepository.Verify(r => r.UpdatePrescriptionItemDispenseStatusAsync(itemToDispense.PrescriptionItemId, "10 units", true, DefaultPharmacistUserId, null, null), Times.Once);
        }


        [Fact]
        public async Task ProcessDispenseAsync_LogActionFails_ShouldSetOverallSuccessFalseAndNotDecrementStock()
        {
            var itemToDispense = new DispenseItemDto { PrescriptionItemId = 1, MedicationId = 101, MedicationName = "MedX", QuantityPrescribed = "10 units", IsSelectedForDispense = true, QuantityToDispense = "10 units" };
            var input = CreateDispenseInput(1, new List<DispenseItemDto> { itemToDispense });
            SetupSuccessfulVerificationMock();
            var currentInfo = new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "10 units", QuantityDispensedSoFar = "0 units", IsAlreadyFullyDispensed = false };
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(itemToDispense.PrescriptionItemId, null, null)).ReturnsAsync(currentInfo);
            _mockDispensationRepository.Setup(r => r.LogDispenseActionAsync(It.IsAny<DispenseLogEntryInputDto>(), null, null)).ReturnsAsync(0); // Log fails

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess);
            Assert.Single(result.DispensedItems);
            Assert.Contains("Error: Failed to log this dispense action.", result.DispensedItems.First().Notes);
            _mockMedicationRepository.Verify(r => r.DecrementStockAsync(It.IsAny<int>(), It.IsAny<int>(), null, null), Times.AtLeastOnce); // Stock decrement should NOT happen
            _mockPrescriptionRepository.Verify(r => r.UpdatePrescriptionItemDispenseStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), null, null), Times.Never);
        }

        [Fact]
        public async Task ProcessDispenseAsync_UpdateStatusFailsAfterLogAndStock_ShouldSetOverallSuccessFalse()
        {
            var itemToDispense = new DispenseItemDto { PrescriptionItemId = 1, MedicationId = 101, MedicationName = "MedX", QuantityPrescribed = "10 units", IsSelectedForDispense = true, QuantityToDispense = "10 units" };
            var input = CreateDispenseInput(1, new List<DispenseItemDto> { itemToDispense });
            SetupSuccessfulVerificationMock();
            var currentInfo = new PrescriptionItemDispenseInfoDto { QuantityPrescribed = "10 units", QuantityDispensedSoFar = "0 units", IsAlreadyFullyDispensed = false };
            _mockPrescriptionRepository.Setup(r => r.GetPrescriptionItemCurrentDispenseInfoAsync(itemToDispense.PrescriptionItemId, null, null)).ReturnsAsync(currentInfo);
            _mockMedicationRepository.Setup(r => r.DecrementStockAsync(itemToDispense.MedicationId, 10, null, null)).ReturnsAsync(true); // Stock success
            _mockDispensationRepository.Setup(r => r.LogDispenseActionAsync(It.IsAny<DispenseLogEntryInputDto>(), null, null)).ReturnsAsync(1); // Log succeeds
            _mockPrescriptionRepository.Setup(r => r.UpdatePrescriptionItemDispenseStatusAsync(itemToDispense.PrescriptionItemId, "10 units", true, DefaultPharmacistUserId, null, null)).ReturnsAsync(false); // Update status fails

            var result = await _pharmacyService.ProcessDispenseAsync(input, DefaultPharmacistUserId);

            Assert.False(result.OverallSuccess);
            Assert.Single(result.DispensedItems);
            Assert.Contains("Error: Failed to update prescription item status after logging dispense.", result.DispensedItems.First().Notes);
        }

        [Fact]
        public async Task GetDispensedHistoryViewModelAsync_ShouldCallRepositoryAndAssembleViewModel()
        {
            // Arrange
            var options = new FilterAndPaginationOptions { PageNumber = 1, PageSize = 10 };
            var itemsFromRepo = new List<DispensedHistoryItemDto> { new DispensedHistoryItemDto { DispensationLogItemId = 1, PatientName = "Test" } };
            int totalCountFromRepo = 15;

            _mockDispensationRepository
                .Setup(repo => repo.GetDispensedHistoryAsync(options, null, null))
                .ReturnsAsync((itemsFromRepo, totalCountFromRepo));

            // Act
            var viewModel = await _pharmacyService.GetDispensedHistoryViewModelAsync(options);

            // Assert
            Assert.NotNull(viewModel);
            Assert.NotNull(viewModel.PaginationInfo);
            Assert.Equal(options.PageNumber, viewModel.PaginationInfo.CurrentPage);
            Assert.Equal(options.PageSize, viewModel.PaginationInfo.PageSize);
            Assert.Equal(totalCountFromRepo, viewModel.PaginationInfo.TotalItems);
            Assert.Equal(2, viewModel.PaginationInfo.TotalPages); // Ceiling(15/10)

            // Pass through filter options
            Assert.Equal(options.StartDate, viewModel.FilterStartDate);
            Assert.Equal(options.EndDate, viewModel.FilterEndDate);
            Assert.Equal(options.SearchTerm1, viewModel.FilterPatientSearch);
            Assert.Equal(options.SearchTerm2, viewModel.FilterMedicationSearch);

            _mockDispensationRepository.Verify(repo => repo.GetDispensedHistoryAsync(options, null, null), Times.Once);
        }

        [Fact]
        public async Task GetDispensedHistoryViewModelAsync_WithInvalidPageOptions_ShouldUseDefaults()
        {
            // Arrange
            var options = new FilterAndPaginationOptions { PageNumber = 0, PageSize = 0 }; // Invalid
            var itemsFromRepo = new List<DispensedHistoryItemDto>();
            int totalCountFromRepo = 0;

            // Service will correct pageNumber to 1 and pageSize to 25 (default)
            _mockDispensationRepository
                .Setup(repo => repo.GetDispensedHistoryAsync(
                    It.Is<FilterAndPaginationOptions>(opt => opt.PageNumber == 1 && opt.PageSize == 25),
                    null, null))
                .ReturnsAsync((itemsFromRepo, totalCountFromRepo));

            // Act
            var viewModel = await _pharmacyService.GetDispensedHistoryViewModelAsync(options);

            // Assert
            Assert.NotNull(viewModel);
            Assert.NotNull(viewModel.PaginationInfo);
            Assert.Equal(1, viewModel.PaginationInfo.CurrentPage);
            Assert.Equal(25, viewModel.PaginationInfo.PageSize);

            _mockDispensationRepository.Verify(repo => repo.GetDispensedHistoryAsync(
                It.Is<FilterAndPaginationOptions>(opt => opt.PageNumber == 1 && opt.PageSize == 25),
                null, null), Times.Once);
        }

        [Fact]
        public async Task GetDispensedHistoryViewModelAsync_WhenRepositoryReturnsNullItems_ShouldReturnEmptyList()
        {
            // Arrange
            var options = new FilterAndPaginationOptions { PageNumber = 1, PageSize = 10 };
            int totalCountFromRepo = 0;

            _mockDispensationRepository
                .Setup(repo => repo.GetDispensedHistoryAsync(options, null, null))!
                .ReturnsAsync(((IEnumerable<DispensedHistoryItemDto>?)null, totalCountFromRepo)); // Items are null

            // Act
            var viewModel = await _pharmacyService.GetDispensedHistoryViewModelAsync(options);

            // Assert
            Assert.NotNull(viewModel);
            Assert.NotNull(viewModel.DispensedItems);
            Assert.Empty(viewModel.DispensedItems); // Service should handle null and return empty list
            Assert.Equal(totalCountFromRepo, viewModel.PaginationInfo.TotalItems);
        }
    }
}