using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.services;
using carestream.core.dtos.consultation;

namespace carestream.tests.unit.services
{
    public class SickNoteServiceTests
    {
        private readonly Mock<ISickNoteRepository> _mockSickNoteRepository;
        private readonly Mock<ILogger<SickNoteService>> _mockLogger;
        private readonly ISickNoteService _sickNoteService;

        public SickNoteServiceTests()
        {
            _mockSickNoteRepository = new Mock<ISickNoteRepository>();
            _mockLogger = new Mock<ILogger<SickNoteService>>();
            _sickNoteService = new SickNoteService(
                _mockSickNoteRepository.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetSickNoteForVisitAsync_WhenNoteExists_ShouldReturnNote()
        {
            // Arrange
            int visitId = 1;
            var expectedNote = new SickNoteInputDto { VisitId = visitId, Diagnosis = "Flu" };
            _mockSickNoteRepository.Setup(repo => repo.GetSickNoteByVisitIdAsync(visitId, null, null))
                                   .ReturnsAsync(expectedNote);

            // Act
            var result = await _sickNoteService.GetSickNoteForVisitAsync(visitId);

            // Assert
            Assert.Same(expectedNote, result);
            _mockSickNoteRepository.Verify(repo => repo.GetSickNoteByVisitIdAsync(visitId, null, null), Times.Once);
        }

        [Fact]
        public async Task GetSickNoteForVisitAsync_WhenNoteDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            int visitId = 2;
            _mockSickNoteRepository.Setup(repo => repo.GetSickNoteByVisitIdAsync(visitId, null, null))
                                   .ReturnsAsync((SickNoteInputDto?)null);

            // Act
            var result = await _sickNoteService.GetSickNoteForVisitAsync(visitId);

            // Assert
            Assert.Null(result);
            _mockSickNoteRepository.Verify(repo => repo.GetSickNoteByVisitIdAsync(visitId, null, null), Times.Once);
        }

        [Fact]
        public async Task SaveSickNoteAsync_WhenCreatingNewNote_ShouldCallCreateRepositoryMethod()
        {
            // Arrange
            var inputNote = new SickNoteInputDto { VisitId = 1, Diagnosis = "Cold", SickNoteId = null }; // New note has null ID
            int performingUserId = 101;
            var createdNoteDto = new SickNoteInputDto { SickNoteId = 5, VisitId = 1, Diagnosis = "Cold", IssuedAt = DateTime.UtcNow };
            _mockSickNoteRepository.Setup(repo => repo.CreateSickNoteAsync(inputNote, performingUserId, null, null))
                                   .ReturnsAsync(createdNoteDto);

            // Act
            var result = await _sickNoteService.SaveSickNoteAsync(inputNote, performingUserId);

            // Assert
            Assert.Same(createdNoteDto, result);
            _mockSickNoteRepository.Verify(repo => repo.CreateSickNoteAsync(inputNote, performingUserId, null, null), Times.Once);
            _mockSickNoteRepository.Verify(repo => repo.UpdateSickNoteAsync(It.IsAny<SickNoteInputDto>(), It.IsAny<int>(), null, null), Times.Never);
        }

        [Fact]
        public async Task SaveSickNoteAsync_WhenUpdatingExistingNote_ShouldCallUpdateRepositoryMethod()
        {
            // Arrange
            var inputNote = new SickNoteInputDto { SickNoteId = 5, VisitId = 1, Diagnosis = "Updated Flu Diagnosis" };
            int performingUserId = 102;
            var updatedNoteDto = new SickNoteInputDto { SickNoteId = 5, VisitId = 1, Diagnosis = "Updated Flu Diagnosis", IssuedAt = DateTime.UtcNow };
            _mockSickNoteRepository.Setup(repo => repo.UpdateSickNoteAsync(inputNote, performingUserId, null, null))
                                  .ReturnsAsync(updatedNoteDto);

            // Act
            var result = await _sickNoteService.SaveSickNoteAsync(inputNote, performingUserId);

            // Assert
            Assert.Same(updatedNoteDto, result);
            _mockSickNoteRepository.Verify(repo => repo.UpdateSickNoteAsync(inputNote, performingUserId, null, null), Times.Once);
            _mockSickNoteRepository.Verify(repo => repo.CreateSickNoteAsync(It.IsAny<SickNoteInputDto>(), It.IsAny<int>(), null, null), Times.Never);
        }

        [Fact]
        public async Task SaveSickNoteAsync_WhenRepositoryFails_ShouldReturnNull()
        {
            // Arrange
            var inputNote = new SickNoteInputDto { VisitId = 1, Diagnosis = "Cold" }; // New note
            int performingUserId = 103;
            _mockSickNoteRepository.Setup(repo => repo.CreateSickNoteAsync(inputNote, performingUserId, null, null))
                                   .ReturnsAsync((SickNoteInputDto?)null); // Simulate repository failure

            // Act
            var result = await _sickNoteService.SaveSickNoteAsync(inputNote, performingUserId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SaveSickNoteAsync_WithInvalidDates_ShouldProceedToRepository_IfServiceValidationIsMinimal()
        {
            // Arrange
            var inputNote = new SickNoteInputDto { VisitId = 1, StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow }; // EndDate < StartDate
            int performingUserId = 104;
            // Service currently doesn't throw/block for this, relies on DTO validation attributes or downstream checks
            // For this test, we assume it passes through to the repository if no service-level exception is thrown.
            _mockSickNoteRepository.Setup(repo => repo.CreateSickNoteAsync(inputNote, performingUserId, null, null))
                                   .ReturnsAsync(new SickNoteInputDto { SickNoteId = 1 });


            // Act
            var result = await _sickNoteService.SaveSickNoteAsync(inputNote, performingUserId);

            // Assert
            // Assert that the repository method was called, implying service didn't block it.
            // The actual success/failure of the DB operation would be an integration test.
            Assert.NotNull(result); // Because the mock repo returns a DTO
            _mockSickNoteRepository.Verify(repo => repo.CreateSickNoteAsync(inputNote, performingUserId, null, null), Times.Once);
        }
    }
}