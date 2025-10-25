//using Moq;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using Microsoft.AspNetCore.Http;
//using System.Security.Claims;
//using carestream.web.controllers; 
//using carestream.core.interfaces.repositories;
//using carestream.core.dtos.consultation;
//using carestream.core.dtos.patient;
//using carestream.core.dtos.vitals;
//using carestream.core.interfaces.services;
//using carestream.core.dtos.medication;
//using carestream.core.dtos.prescription;
//using Microsoft.AspNetCore.Routing;
//using Microsoft.AspNetCore.Mvc.Routing;

//namespace carestream.tests.unit.controllers 
//{
//    public class ConsultationControllerTests
//    {
//        private readonly Mock<IPatientRepository> _mockPatientRepository;
//        private readonly Mock<IVisitRepository> _mockVisitRepository;
//        private readonly Mock<IVitalsRepository> _mockVitalsRepository;
//        private readonly Mock<IUserRepository> _mockUserRepository;
//        private readonly Mock<ILogger<ConsultationController>> _mockLogger;
//        private readonly ConsultationController _controller;
//        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor; // Add this
//        private readonly Mock<IMedicationRepository> _mockMedicationRepository;
//        private readonly Mock<IPrescriptionRepository> _mockPrescriptionRepository;
//        private readonly Mock<IPrescriptionService> _mockPrescriptionService;
//        private readonly Mock<ISickNoteService> _mockSickNoteService;
//        private readonly Mock<IUrlHelper> _mockUrlHelper;

//        public ConsultationControllerTests()
//        {
//            _mockPatientRepository = new Mock<IPatientRepository>();
//            _mockVisitRepository = new Mock<IVisitRepository>();
//            _mockVitalsRepository = new Mock<IVitalsRepository>();
//            _mockUserRepository = new Mock<IUserRepository>();
//            _mockLogger = new Mock<ILogger<ConsultationController>>();
//            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>(); // Initialize
//            _mockMedicationRepository = new Mock<IMedicationRepository>();
//            _mockPrescriptionRepository = new Mock<IPrescriptionRepository>();
//            _mockPrescriptionService = new Mock<IPrescriptionService>();
//            _mockSickNoteService = new Mock<ISickNoteService>();
//            _mockUrlHelper = new Mock<IUrlHelper>();

//            // Setup default HttpContext for tests that need a logged-in user
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim("sub", "test-doctor-sub-id"),
//                new Claim(ClaimTypes.Role, "Doctor") // Assuming Doctor role for most tests here
//            }, "mock"));
//            var httpContext = new DefaultHttpContext { User = user };
//            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

//            _controller = new ConsultationController(
//                _mockPatientRepository.Object,
//                _mockVisitRepository.Object,
//                _mockVitalsRepository.Object,
//                _mockLogger.Object,
//                _mockUserRepository.Object,
//                _mockMedicationRepository.Object,
//                _mockPrescriptionRepository.Object,
//                _mockPrescriptionService.Object,
//                _mockSickNoteService.Object
//            )
//            {
//                // ControllerContext is needed for User property, TempData, Url etc.
//                ControllerContext = new ControllerContext
//                {
//                    HttpContext = httpContext,
//                    RouteData = new RouteData(),
//                    ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
//                },
//                Url = _mockUrlHelper.Object
//            };

//            _mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
//                          .Returns((UrlActionContext uac) => $"/{uac.Controller}/{uac.Action}");
//        }


//        //[Fact]
//        //public async Task StartConsultation_ShouldFetchDataAndReturnConsultationLayout()
//        //{
//        //    // Arrange
//        //    int visitId = 1;
//        //    int patientId = 101;
//        //    string doctorLogtoSub = "test-doctor-sub-id";
//        //    int doctorInternalId = 5; // Assume this is the linked internal ID
//        //    string expectedNotes = "Previous notes";

//        //    var patientInfo = new PatientBasicInfoDto { PatientId = patientId, FirstName = "John", LastName = "Doe", Rank = "Sgt" };
//        //    var visitInfo = new BasicVisitInfoDto { VisitId = visitId, Status = "Ready for Doctor", BriefReason = "Checkup", AssignedOfficerUserId = null }; // Initially not assigned to this doc
//        //    var vitalsData = new VitalsCaptureInputDto { VisitId = visitId, PatientId = patientId, BloodPressureSystolic = 120 };

//        //    _mockPatientRepository.Setup(r => r.GetPatientBasicInfoByIdAsync(patientId, null, null)).ReturnsAsync(patientInfo);
//        //    _mockVisitRepository.Setup(r => r.GetBasicVisitInfoByIdAsync(visitId, null, null)).ReturnsAsync(visitInfo);
//        //    _mockVitalsRepository.Setup(r => r.GetVitalsForVisitAsync(visitId, null, null)).ReturnsAsync(vitalsData);
//        //    _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync(doctorLogtoSub, null, null)).ReturnsAsync(doctorInternalId);
//        //    _mockVisitRepository.Setup(r => r.UpdateVisitStatusAsync(visitId, "Consultation In Progress", doctorInternalId, null, null)).ReturnsAsync(true);
//        //    _mockVisitRepository.Setup(r => r.GetDoctorNotesAsync(visitId, null, null)).ReturnsAsync(expectedNotes);

//        //    // Act
//        //    var result = await _controller.StartConsultation(visitId, patientId);

//        //    // Assert
//        //    var partialViewResult = Assert.IsType<PartialViewResult>(result);
//        //    Assert.Equal("_ConsultationLayout", partialViewResult.ViewName);
//        //    var model = Assert.IsType<ConsultationViewModel>(partialViewResult.Model);
//        //    Assert.NotNull(model.PatientBanner);
//        //    Assert.NotNull(model.VitalsData);
//        //    Assert.Equal(expectedNotes, model.DoctorNotes); // Verify notes are passed
//        //    Assert.Equal("Consultation In Progress", visitInfo.Status); // Verify status was updated in the DTO used by controller
//        //    _mockVisitRepository.Verify(r => r.UpdateVisitStatusAsync(visitId, "Consultation In Progress", doctorInternalId, null, null), Times.Once);
//        //    _mockVisitRepository.Verify(r => r.GetDoctorNotesAsync(visitId, null, null), Times.Once);
//        //}

//        [Fact]
//        public async Task SaveDoctorNotes_WithValidInput_ShouldCallRepositoryAndReturnSuccessMessage()
//        {
//            // Arrange
//            int visitId = 1;
//            string notesToSave = "Patient is improving.";
//            _mockVisitRepository.Setup(r => r.UpdateDoctorNotesAsync(visitId, notesToSave, null, null))
//                                .ReturnsAsync(true); // Simulate successful save

//            // Act
//            var result = await _controller.SaveDoctorNotes(visitId, notesToSave);

//            // Assert
//            var contentResult = Assert.IsType<OkResult>(result);
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
//            Assert.Contains("showToastSuccess", _controller.Response.Headers["HX-Trigger"].ToString());

//            _mockVisitRepository.Verify(r => r.UpdateDoctorNotesAsync(visitId, notesToSave, null, null), Times.Once);
//        }

//        [Fact]
//        public async Task SaveDoctorNotes_WhenRepositoryFails_ShouldReturnErrorMessage()
//        {
//            // Arrange
//            int visitId = 1;
//            string notesToSave = "Patient is improving.";
//            _mockVisitRepository.Setup(r => r.UpdateDoctorNotesAsync(visitId, notesToSave, null, null))
//                                .ReturnsAsync(false); // Simulate failed save

//            // Act
//            var result = await _controller.SaveDoctorNotes(visitId, notesToSave);

//            // Assert
//            var contentResult = Assert.IsType<ContentResult>(result);
//            Assert.Equal("text/html", contentResult.ContentType);
//            Assert.Contains("Failed to save notes.", contentResult.Content);
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
//            Assert.Contains("showToastError", _controller.Response.Headers["HX-Trigger"].ToString());

//            _mockVisitRepository.Verify(r => r.UpdateDoctorNotesAsync(visitId, notesToSave, null, null), Times.Once);
//        }

//        [Fact]
//        public async Task SaveDoctorNotes_WithInvalidVisitId_ShouldReturnBadRequest()
//        {
//            // Arrange
//            int invalidVisitId = 0;
//            string notesToSave = "Some notes.";

//            // Act
//            var result = await _controller.SaveDoctorNotes(invalidVisitId, notesToSave);

//            // Assert
//            Assert.IsType<BadRequestObjectResult>(result);
//            _mockVisitRepository.Verify(r => r.UpdateDoctorNotesAsync(It.IsAny<int>(), It.IsAny<string>(), null, null), Times.Never);
//        }

//        [Fact]
//        public async Task MedicationsTab_ValidIds_ShouldCallServiceAndReturnPartialWithViewModel()
//        {
//            // Arrange
//            int visitId = 1;
//            int patientId = 101;
//            var expectedViewModel = new ConsultationMedicationsViewModel { VisitId = visitId, PatientId = patientId };
//            _mockPrescriptionService.Setup(s => s.GetMedicationsViewModelAsync(visitId, patientId))
//                                    .ReturnsAsync(expectedViewModel);

//            // Act
//            var result = await _controller.MedicationsTab(visitId, patientId);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_ConsultationMedicationsTab", partialViewResult.ViewName);
//            Assert.Same(expectedViewModel, partialViewResult.Model);
//            _mockPrescriptionService.Verify(s => s.GetMedicationsViewModelAsync(visitId, patientId), Times.Once);
//        }

//        [Theory]
//        [InlineData(0, 1)]
//        [InlineData(1, 0)]
//        public async Task MedicationsTab_InvalidIds_ShouldReturnErrorPartial(int visitId, int patientId)
//        {
//            // Act
//            var result = await _controller.MedicationsTab(visitId, patientId);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_ErrorPartial", partialViewResult.ViewName);
//            Assert.Equal("Invalid visit or patient identifier for medications tab.", partialViewResult.Model as string);
//        }


//        [Fact]
//        public async Task SearchMedications_WithValidTerm_ShouldCallServiceAndReturnResultsPartial()
//        {
//            // Arrange
//            string searchTerm = "Amoxi";
//            int visitId = 1, patientId = 101;
//            var searchResults = new List<MedicationSearchResultDto> { new MedicationSearchResultDto { Name = "Amoxicillin" } };
//            _mockPrescriptionService.Setup(s => s.SearchMedicationsAsync(searchTerm, It.IsAny<int>()))
//                                    .ReturnsAsync(searchResults);

//            // Act
//            var result = await _controller.SearchMedications(searchTerm, visitId, patientId);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_MedicationSearchResults", partialViewResult.ViewName);
//            Assert.Same(searchResults, partialViewResult.Model);
//            Assert.Equal(visitId, partialViewResult.ViewData["VisitId"]);
//            Assert.Equal(patientId, partialViewResult.ViewData["PatientId"]);
//            _mockPrescriptionService.Verify(s => s.SearchMedicationsAsync(searchTerm, It.IsAny<int>()), Times.Once);
//        }

//        [Theory]
//        [InlineData(null)]
//        [InlineData("")]
//        [InlineData("A")]
//        public async Task SearchMedications_WithInvalidTerm_ShouldReturnEmptyResultsPartial(string? searchTerm)
//        {
//            // Act
//            var result = await _controller.SearchMedications(searchTerm!, 1, 101);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_MedicationSearchResults", partialViewResult.ViewName);
//            var model = Assert.IsAssignableFrom<IEnumerable<MedicationSearchResultDto>>(partialViewResult.Model);
//            Assert.Empty(model);
//            _mockPrescriptionService.Verify(s => s.SearchMedicationsAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never); // Service validation handles it
//        }

//        [Fact]
//        public async Task AddPrescriptionItem_ValidModel_ShouldCallServiceAndReturnCurrentItemsPartial()
//        {
//            // Arrange
//            var inputDto = new AddPrescriptionItemInputDto { VisitId = 1, MedicationId = 1, Dosage = "10mg" };
//            var updatedItems = new List<PrescriptionItemDto> { new PrescriptionItemDto { MedicationName = "TestMed" } };
//            int performingUserId = 123; // From mocked user

//            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-doctor-sub-id", null, null)).ReturnsAsync(performingUserId);
//            _mockPrescriptionService.Setup(s => s.AddPrescriptionItemAsync(inputDto, performingUserId))
//                                    .ReturnsAsync(updatedItems);
//            _controller.ModelState.Clear(); // Ensure model state is valid for this test

//            // Act
//            var result = await _controller.AddPrescriptionItem(inputDto);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_CurrentPrescriptionItems", partialViewResult.ViewName);
//            Assert.Same(updatedItems, partialViewResult.Model);
//            _mockPrescriptionService.Verify(s => s.AddPrescriptionItemAsync(inputDto, performingUserId), Times.Once);
//        }

//        [Fact]
//        public async Task AddPrescriptionItem_InvalidModel_ShouldReturnValidationErrorMessagesPartial()
//        {
//            // Arrange
//            var inputDto = new AddPrescriptionItemInputDto { VisitId = 1 }; // Missing MedicationId, Dosage etc.
//            _controller.ModelState.AddModelError("MedicationId", "Required"); // Simulate validation error

//            // Act
//            var result = await _controller.AddPrescriptionItem(inputDto);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_ValidationErrorMessages", partialViewResult.ViewName);
//            Assert.False(_controller.ModelState.IsValid);
//            _mockPrescriptionService.Verify(s => s.AddPrescriptionItemAsync(It.IsAny<AddPrescriptionItemInputDto>(), It.IsAny<int>()), Times.Never);
//        }


//        [Fact]
//        public async Task RemovePrescriptionItem_ValidInput_ShouldCallServiceAndReturnCurrentItemsPartial()
//        {
//            // Arrange
//            int prescriptionItemId = 1;
//            int visitId = 10;
//            var updatedItems = new List<PrescriptionItemDto>(); // E.g., empty list after removal
//            _mockPrescriptionService.Setup(s => s.RemovePrescriptionItemAsync(prescriptionItemId, visitId))
//                                    .ReturnsAsync(updatedItems);

//            // Act
//            var result = await _controller.RemovePrescriptionItem(prescriptionItemId, visitId);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_CurrentPrescriptionItems", partialViewResult.ViewName);
//            Assert.Same(updatedItems, partialViewResult.Model);
//            _mockPrescriptionService.Verify(s => s.RemovePrescriptionItemAsync(prescriptionItemId, visitId), Times.Once);
//        }

//        [Fact]
//        public async Task SendPrescriptionToPharmacy_ServiceSucceeds_ShouldReturnMedicationsTabWithSuccessTrigger()
//        {
//            // Arrange
//            int visitId = 1;
//            int sentByUserId = 123;
//            var reloadedViewModel = new ConsultationMedicationsViewModel { VisitId = visitId };

//            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-doctor-sub-id", null, null)).ReturnsAsync(sentByUserId);
//            _mockPrescriptionService.Setup(s => s.SendPrescriptionToPharmacyAsync(visitId, sentByUserId))
//                                    .ReturnsAsync(true);
//            _mockPrescriptionService.Setup(s => s.GetMedicationsViewModelAsync(visitId, 0)) // Assuming patientId 0 is ok for reload context
//                                    .ReturnsAsync(reloadedViewModel);

//            // Act
//            var result = await _controller.SendPrescriptionToPharmacy(visitId);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_ConsultationMedicationsTab", partialViewResult.ViewName);
//            Assert.Same(reloadedViewModel, partialViewResult.Model);
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
//            Assert.Contains("showToastSuccess", _controller.Response.Headers["HX-Trigger"].ToString());
//        }

//        [Fact]
//        public async Task SendPrescriptionToPharmacy_ServiceFails_ShouldReturnOkResultWithErrorTrigger() // Renamed for clarity
//        {
//            // Arrange
//            int visitId = 1;
//            int sentByUserId = 123;
//            // No need to mock GetMedicationsViewModelAsync here as it shouldn't be called by this path in controller

//            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-doctor-sub-id", null, null)).ReturnsAsync(sentByUserId);
//            _mockPrescriptionService.Setup(s => s.SendPrescriptionToPharmacyAsync(visitId, sentByUserId))
//                                    .ReturnsAsync(false); // Simulate failure

//            // Act
//            var result = await _controller.SendPrescriptionToPharmacy(visitId);

//            // Assert
//            Assert.IsType<OkResult>(result); // <-- VERIFY OkResult is returned
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
//            Assert.Contains("showToastError", _controller.Response.Headers["HX-Trigger"].ToString());
//            Assert.Contains("Failed to send prescription", _controller.Response.Headers["HX-Trigger"].ToString());

//            // Verify the service was called
//            _mockPrescriptionService.Verify(s => s.SendPrescriptionToPharmacyAsync(visitId, sentByUserId), Times.Once);
//            // Verify GetMedicationsViewModelAsync was NOT called in the failure path where Ok() is returned
//            _mockPrescriptionService.Verify(s => s.GetMedicationsViewModelAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
//        }

//        [Fact]
//        public async Task FinalizeConsultation_ValidVisit_UserLinked_ServiceSucceeds_ShouldReturnOkWithHxRedirectAndToast()
//        {
//            // Arrange
//            int visitId = 1;
//            int patientId = 101;
//            string logtoSub = "test-doctor-sub-id";
//            int performingUserId = 5;
//            string expectedRedirectUrl = "/Dashboard/DoctorDashboard"; // The string our mock will return

//            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync(logtoSub, null, null)).ReturnsAsync(performingUserId);
//            _mockVisitRepository
//                .Setup(r => r.UpdateVisitStatusAsync(visitId, "Discharged", performingUserId, null, null))
//                .ReturnsAsync(true);

//            // Configure the mock UrlHelper specifically for this test's expected call if necessary,
//            // or rely on the general setup in the constructor.
//            // The general setup in constructor should be fine here.
//            // _mockUrlHelper.Setup(x => x.Action(It.Is<UrlActionContext>(uac => uac.Action == "DoctorDashboard" && uac.Controller == "Dashboard")))
//            //               .Returns(expectedRedirectUrl);


//            // Act
//            var result = await _controller.FinalizeConsultation(visitId, patientId);

//            // Assert
//            Assert.IsType<OkResult>(result);

//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Redirect"));
//            Assert.Equal(expectedRedirectUrl, _controller.Response.Headers["HX-Redirect"].ToString()); // Assert against the expected mock output

//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
//            Assert.Contains("showToastSuccess", _controller.Response.Headers["HX-Trigger"].ToString());
//            Assert.Contains("Consultation finalized successfully!", _controller.Response.Headers["HX-Trigger"].ToString());

//            _mockVisitRepository.Verify(r => r.UpdateVisitStatusAsync(visitId, "Discharged", performingUserId, null, null), Times.Once);
//        }

//        [Fact]
//        public async Task FinalizeConsultation_ServiceFails_ShouldReturnOkWithHxErrorToast()
//        {
//            // Arrange
//            int visitId = 2;
//            int patientId = 102;
//            string logtoSub = "test-doctor-sub-id";
//            int performingUserId = 5;

//            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync(logtoSub, null, null)).ReturnsAsync(performingUserId);
//            _mockVisitRepository
//                .Setup(r => r.UpdateVisitStatusAsync(visitId, "Discharged", performingUserId, null, null))
//                .ReturnsAsync(false); // Simulate repository/service failure

//            // Act
//            var result = await _controller.FinalizeConsultation(visitId, patientId);

//            // Assert
//            Assert.IsType<OkResult>(result);
//            Assert.False(_controller.Response.Headers.ContainsKey("HX-Redirect")); // No redirect on failure
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
//            Assert.Contains("showToastError", _controller.Response.Headers["HX-Trigger"].ToString());
//            Assert.Contains("Failed to finalize consultation", _controller.Response.Headers["HX-Trigger"].ToString());

//            _mockVisitRepository.Verify(r => r.UpdateVisitStatusAsync(visitId, "Discharged", performingUserId, null, null), Times.Once);
//        }

//        [Fact]
//        public async Task FinalizeConsultation_UserNotLinked_ShouldReturnOkWithHxErrorToast()
//        {
//            // Arrange
//            int visitId = 3;
//            int patientId = 103;
//            string logtoSub = "test-doctor-sub-id";

//            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync(logtoSub, null, null)).ReturnsAsync((int?)null); // Simulate unlinked user

//            // Act
//            var result = await _controller.FinalizeConsultation(visitId, patientId);

//            // Assert
//            Assert.IsType<OkResult>(result);
//            Assert.False(_controller.Response.Headers.ContainsKey("HX-Redirect"));
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
//            Assert.Contains("showToastError", _controller.Response.Headers["HX-Trigger"].ToString());
//            Assert.Contains("User session invalid", _controller.Response.Headers["HX-Trigger"].ToString());

//            _mockVisitRepository.Verify(r => r.UpdateVisitStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), null, null), Times.Never);
//        }

//        [Theory]
//        [InlineData(0, 101)]
//        [InlineData(-1, 101)]
//        public async Task FinalizeConsultation_InvalidVisitId_ShouldReturnOkWithHxErrorToast(int invalidVisitId, int patientId)
//        {
//            // Act
//            var result = await _controller.FinalizeConsultation(invalidVisitId, patientId);

//            // Assert
//            Assert.IsType<OkResult>(result);
//            Assert.False(_controller.Response.Headers.ContainsKey("HX-Redirect"));
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
//            Assert.Contains("showToastError", _controller.Response.Headers["HX-Trigger"].ToString());
//            Assert.Contains("Invalid visit ID", _controller.Response.Headers["HX-Trigger"].ToString());

//            _mockVisitRepository.Verify(r => r.UpdateVisitStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), null, null), Times.Never);
//        }
//    }
//}