using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories; // Needed for IUserRepository
using carestream.core.dtos.messaging; // For ConversationDto, MessageDto, CreateMessageInputDto
using System.Security.Claims;
using carestream.core.dtos.admin;

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for managing internal messaging operations.
    /// Accessible by all authorized users with a Messages link.
    /// </summary>
    [Authorize] // As per FRS, available to all roles
    public class MessagesController : Controller
    {
        private readonly IMessagingService _messagingService;
        private readonly IUserRepository _userRepository; // To get internal user ID for actions
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(
            IMessagingService messagingService,
            IUserRepository userRepository,
            ILogger<MessagesController> logger)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Messages/Index
        /// Displays the main container for the messaging UI, including conversations list and a message panel placeholder (FR-MSG-001).
        /// </summary>
        /// <returns>A partial view for the messaging page.</returns>
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Controller: Messages/Index requested.");
            // The conversation list and message panel will be loaded by HTMX
            return PartialView(); // Renders Views/Messages/Index.cshtml
        }

        /// <summary>
        /// GET: /Messages/ConversationListPartial
        /// Fetches and returns the partial view containing the list of conversations for the current user (FR-MSG-002).
        /// </summary>
        /// <returns>A partial view displaying the conversations list.</returns>
        [HttpGet]
        public async Task<IActionResult> ConversationListPartial()
        {
            _logger.LogInformation("Controller: Fetching ConversationListPartial.");

            var currentUserId = await GetInternalUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("Controller: ConversationListPartial called without valid user ID.");
                return PartialView("~/Views/Shared/_ErrorPartial", "User session invalid for messages.");
            }

            var conversations = await _messagingService.GetUserConversationsAsync(currentUserId.Value);
            return PartialView("_ConversationListPartial", conversations);
        }

        /// <summary>
        /// GET: /Messages/MessagePanelPartial/{conversationId}
        /// Fetches and returns the partial view containing the messages within a specific conversation.
        /// This action now also marks the conversation as read for the current user.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to display messages from.</param>
        /// <returns>A partial view displaying the message panel.</returns>
        [HttpGet]
        public async Task<IActionResult> MessagePanelPartial(int conversationId)
        {
            _logger.LogInformation("Controller: Fetching MessagePanelPartial for ConversationId: {ConversationId}", conversationId);

            var currentUserId = await GetInternalUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("Controller: MessagePanelPartial called without valid user ID.");
                return PartialView("~/Views/Shared/_ErrorPartial", "User session invalid for messages.");
            }

            if (conversationId <= 0)
            {
                // This might happen if no conversation is selected yet, return an empty/instructional panel
                return PartialView("_MessagePanelPartial", new List<MessageDto>());
            }

            // Mark the conversation as read for the current user when they open it
            await _messagingService.MarkConversationAsReadAsync(conversationId, currentUserId.Value);

            var messages = await _messagingService.GetConversationMessagesAsync(conversationId, currentUserId.Value);
            ViewData["ConversationId"] = conversationId; // Pass context for sending messages
            ViewData["CurrentUserId"] = currentUserId.Value; // Pass current user for styling sent messages
            return PartialView("_MessagePanelPartial", messages);
        }

        /// <summary>
        /// POST: /Messages/SendMessage
        /// Handles sending a new message within a conversation (FR-MSG-003).
        /// </summary>
        /// <param name="dto">The DTO containing the message content and conversation ID.</param>
        /// <returns>Triggers a refresh of the message panel and a toast.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromForm] CreateMessageInputDto dto)
        {
            _logger.LogInformation("Controller: Sending message in ConversationId: {ConversationId}", dto.ConversationId);

            var currentUserId = await GetInternalUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogError("Controller: SendMessage - Could not identify sending user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot send message.\"}");
                return Ok();
            }
            dto.SenderUserId = currentUserId.Value; // Ensure sender is current user

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: Validation failed for sending message in ConversationId: {ConversationId}.", dto.ConversationId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Message content is required.\"}");
                return Ok();
            }

            var sentMessage = await _messagingService.SendMessageAsync(dto);

            if (sentMessage != null)
            {
                _logger.LogInformation("Controller: Message sent successfully in ConversationId: {ConversationId}", dto.ConversationId);
                // Trigger refresh for message panel (to show new message) and conversation list (to update last message time/unread)
                // Use HX-Trigger-After-Swap to ensure content is appended before triggering refresh
                Response.Headers.Append("HX-Trigger-After-Swap", $"{{ \"showToastSuccess\": \"Message sent!\", \"refreshMessagePanel\": {dto.ConversationId}, \"refreshConversationList\": {dto.ConversationId} }}");
            }
            else
            {
                _logger.LogError("Controller: Failed to send message in ConversationId: {ConversationId}.", dto.ConversationId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to send message. Please try again.\"}");
            }
            return Ok(); // HTMX expects Ok
        }

        /// <summary>
        /// GET: /Messages/UserSearchPartial
        /// Displays the UI for searching and selecting users to start a new conversation.
        /// </summary>
        /// <returns>A partial view for user search.</returns>
        [HttpGet]
        public async Task<IActionResult> UserSearchPartial()
        {
            _logger.LogInformation("Controller: UserSearchPartial requested.");
            // Pass current user ID to the view if needed (e.g., to exclude self from initial display/search)
            ViewData["CurrentUserId"] = await GetInternalUserId();
            return PartialView("_UserSearchAndSelectionPartial");
        }

        /// <summary>
        /// GET: /Messages/SearchUsers
        /// Searches for internal users based on a provided search term.
        /// </summary>
        /// <param name="searchTerm">The term to search for (e.g., name, rank, department).</param>
        /// <returns>A partial view displaying the search results.</returns>
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string searchTerm)
        {
            _logger.LogInformation("Controller: Searching users with term: {SearchTerm}", searchTerm);
            var currentUserId = await GetInternalUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("Controller: SearchUsers called without valid user ID.");
                return PartialView("~/Views/Shared/_ErrorPartial", "User session invalid. Cannot search users.");
            }

            IEnumerable<AdminUserListItemDto> users;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                users = await _messagingService.SearchInternalUsersAsync(searchTerm, currentUserId.Value);
            }
            else
            {
                users = Enumerable.Empty<AdminUserListItemDto>(); // Return empty if no search term
            }

            return PartialView("_UserSearchResultsPartial", users);
        }

        /// <summary>
        /// POST: /Messages/StartConversation
        /// Creates a new conversation or retrieves an existing one with the selected participants.
        /// </summary>
        /// <param name="selectedUserIds">An array of user IDs to include in the conversation (excluding the current user).</param>
        /// <param name="groupName">Optional name for a group chat.</param>
        /// <returns>Triggers HTMX to load the new conversation's message panel and refresh the conversation list.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartConversation([FromForm] int[] selectedUserIds, [FromForm] string? groupName)
        {
            _logger.LogInformation("Controller: Attempting to start conversation with user IDs: {UserIds}", string.Join(", ", selectedUserIds));

            var currentUserId = await GetInternalUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogError("Controller: StartConversation - Could not identify current user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot start conversation.\"}");
                return Ok();
            }

            if (selectedUserIds == null || !selectedUserIds.Any())
            {
                _logger.LogWarning("Controller: StartConversation - No participants selected.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Please select at least one person to chat with.\"}");
                return Ok();
            }

            try
            {
                var conversation = await _messagingService.CreateOrGetConversationAsync(currentUserId.Value, selectedUserIds, groupName);

                if (conversation != null)
                {
                    _logger.LogInformation("Controller: Conversation started/retrieved. ID: {ConversationId}", conversation.ConversationId);
                    // HTMX triggers to load the message panel and refresh the conversation list
                    Response.Headers.Append("HX-Trigger", $"{{ \"showToastSuccess\": \"Conversation started!\", \"refreshMessagePanel\": {conversation.ConversationId}, \"refreshConversationList\": {conversation.ConversationId}, \"setActiveConversation\": {conversation.ConversationId} }}");
                }
                else
                {
                    _logger.LogError("Controller: Failed to create or retrieve conversation.");
                    Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to start conversation. Please try again.\"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Controller: Error starting conversation.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"An unexpected error occurred while starting conversation.\"}");
            }

            return Ok();
        }

        // Helper to get the internal user ID from claims (existing)
        private async Task<int?> GetInternalUserId()
        {
            var userIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                var logtoSub = User.FindFirstValue("sub");
                if (!string.IsNullOrEmpty(logtoSub))
                {
                    return await _userRepository.GetUserIdByLogtoSubAsync(logtoSub);
                }
                return null;
            }
            return userId;
        }
    }
}