using carestream.core.dtos.messaging;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;
using carestream.core.dtos.admin; // Import for AdminUserListItemDto
using System.Linq; // For .Distinct(), .Except()

namespace carestream.core.services
{
    /// <summary>
    /// Implements the business logic for internal messaging operations.
    /// </summary>
    public class MessagingService : IMessagingService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository; // Inject IUserRepository
        private readonly ILogger<MessagingService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingService"/> class.
        /// </summary>
        /// <param name="conversationRepository">The conversation data repository.</param>
        /// <param name="messageRepository">The message data repository.</param>
        /// <param name="userRepository">The user data repository.</param> // Add to constructor
        /// <param name="logger">The logger instance.</param>
        public MessagingService(
            IConversationRepository conversationRepository,
            IMessageRepository messageRepository,
            IUserRepository userRepository, // Add to constructor
            ILogger<MessagingService> logger)
        {
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository)); // Initialize
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a list of conversations for a specific user, including unread message counts.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>An enumerable of <see cref="ConversationDto"/> representing the user's conversations.</returns>
        public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(int userId)
        {
            _logger.LogInformation("Service: Getting conversations for UserId: {UserId}", userId);

            if (userId <= 0)
            {
                _logger.LogWarning("Service: GetUserConversationsAsync called with invalid UserId: {UserId}", userId);
                return Enumerable.Empty<ConversationDto>();
            }

            var conversations = (await _conversationRepository.GetUserConversationsAsync(userId))?.ToList();

            if (conversations == null || !conversations.Any())
            {
                _logger.LogInformation("Service: No conversations found for UserId: {UserId}", userId);
                return Enumerable.Empty<ConversationDto>();
            }

            // Populate unread counts for each conversation
            foreach (var conv in conversations)
            {
                conv.UnreadCount = await _messageRepository.GetUnreadMessagesCountInConversationAsync(conv.ConversationId, userId);
            }

            return conversations;
        }

        /// <summary>
        /// Retrieves a paginated list of messages within a specific conversation.
        /// Also updates the user's 'last viewed at' timestamp for that conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to retrieve messages from.</param>
        /// <param name="userId">The ID of the user viewing the conversation.</param>
        /// <param name="limit">The maximum number of messages to retrieve.</param>
        /// <param name="offset">The offset from which to start retrieving messages.</param>
        /// <returns>An enumerable of <see cref="MessageDto"/> representing the messages in the conversation.</returns>
        public async Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int userId, int limit = 100, int offset = 0)
        {
            _logger.LogInformation("Service: Getting messages for ConversationId: {ConversationId} by UserId: {UserId}. Limit: {Limit}, Offset: {Offset}", conversationId, userId, limit, offset);

            if (conversationId <= 0 || userId <= 0)
            {
                _logger.LogWarning("Service: GetConversationMessagesAsync called with invalid IDs. ConversationId: {ConversationId}, UserId: {UserId}", conversationId, userId);
                return Enumerable.Empty<MessageDto>();
            }

            var messages = await _messageRepository.GetConversationMessagesAsync(conversationId, limit, offset);

            // Important: Update the user's last viewed timestamp for this conversation
            // This is now handled by the MarkConversationAsReadAsync method when the panel is opened.
            // If this method is called outside of opening the panel, this line might still be desired.
            // For the current HTMX flow, it's better to explicitly call MarkConversationAsReadAsync
            // when the user navigates to the conversation.
            // await _messageRepository.UpdateParticipantLastViewedAtAsync(conversationId, userId); // Moved to MarkConversationAsReadAsync

            return messages;
        }

        /// <summary>
        /// Sends a new message in a conversation.
        /// </summary>
        /// <param name="messageData">The DTO containing the message content and sender information.</param>
        /// <returns>A <see cref="MessageDto"/> representing the newly sent message, or null if sending failed.</returns>
        public async Task<MessageDto?> SendMessageAsync(CreateMessageInputDto messageData)
        {
            _logger.LogInformation("Service: Sending new message in ConversationId: {ConversationId} from SenderUserId: {SenderUserId}", messageData.ConversationId, messageData.SenderUserId);

            if (messageData == null || messageData.ConversationId <= 0 || messageData.SenderUserId <= 0 || string.IsNullOrWhiteSpace(messageData.Content))
            {
                _logger.LogWarning("Service: SendMessageAsync called with invalid input: Missing required fields or invalid IDs.");
                return null;
            }

            try
            {
                int newMessageId = await _messageRepository.CreateMessageAsync(messageData);

                if (newMessageId > 0)
                {
                    // Update conversation's last message timestamp for UI sorting/freshness
                    await _conversationRepository.UpdateConversationLastMessageTimestampAsync(messageData.ConversationId);

                    // To return a full MessageDto, fetch the sender's details and include them.
                    var senderUser = await _userRepository.GetUserForAdminByIdAsync(messageData.SenderUserId);

                    _logger.LogInformation("Service: Successfully sent message with ID: {MessageId} in ConversationId: {ConversationId}", newMessageId, messageData.ConversationId);
                    return new MessageDto
                    {
                        MessageId = newMessageId,
                        ConversationId = messageData.ConversationId,
                        SenderUserId = messageData.SenderUserId,
                        SenderUserName = senderUser?.FullName, // Populate from fetched user details
                        SenderUserRank = senderUser?.Rank,     // Populate from fetched user details
                        Content = messageData.Content,
                        SentAt = DateTimeOffset.UtcNow, // Use UTC for consistency
                        IsRead = false // Newly sent messages are unread by others
                    };
                }
                else
                {
                    _logger.LogError("Service: Failed to create new message in ConversationId: {ConversationId}. Repository returned no ID.", messageData.ConversationId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while sending message in ConversationId: {ConversationId}.", messageData.ConversationId);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the total count of unread messages for a specific user across all their conversations.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The total number of unread messages.</returns>
        public async Task<int> GetUserUnreadMessagesCountAsync(int userId)
        {
            _logger.LogInformation("Service: Getting total unread messages count for UserId: {UserId}", userId);

            if (userId <= 0)
            {
                _logger.LogWarning("Service: GetUserUnreadMessagesCountAsync called with invalid UserId: {UserId}", userId);
                return 0;
            }

            // The repository method already aggregates across conversations based on user's last_viewed_at
            // This method in IMessageRepository (GetUnreadMessagesCountInConversationAsync) is designed for a single conversation.
            // For a total count across all conversations, we need to iterate or create a new repository method.
            // Let's assume for now GetUnreadMessagesCountInConversationAsync can be called with 0 for conversationId
            // OR iterate through all conversations and sum. Iterating is safer/more explicit for now.

            var conversations = await GetUserConversationsAsync(userId); // Re-use method to get conversations with counts
            return conversations.Sum(c => c.UnreadCount);
        }

        /// <summary>
        /// Searches for internal users based on a search term, excluding the current user.
        /// </summary>
        /// <param name="searchTerm">The term to search for (e.g., name, rank, department).</param>
        /// <param name="currentUserId">The ID of the current user, to exclude them from results.</param>
        /// <returns>An enumerable of AdminUserListItemDto representing the found users.</returns>
        public async Task<IEnumerable<AdminUserListItemDto>> SearchInternalUsersAsync(string searchTerm, int currentUserId)
        {
            _logger.LogInformation("Service: Searching users with term '{SearchTerm}' for UserId: {CurrentUserId}", searchTerm, currentUserId);

            if (currentUserId <= 0)
            {
                _logger.LogWarning("Service: SearchInternalUsersAsync called with invalid CurrentUserId: {CurrentUserId}", currentUserId);
                return Enumerable.Empty<AdminUserListItemDto>();
            }

            // Leverage existing IUserRepository.GetAllUsersForAdminAsync
            // This method might return more results than strictly needed (pageSize/pageNumber are set by default),
            // but it's okay for typical search scenarios.
            var allUsers = await _userRepository.GetAllUsersForAdminAsync(searchTerm);

            // Filter out the current user and ensure distinct results
            return allUsers.Where(u => u.UserId != currentUserId && u.IsActive).DistinctBy(u => u.UserId).ToList();
        }

        /// <summary>
        /// Creates a new conversation or retrieves an existing direct conversation.
        /// </summary>
        /// <param name="currentUserId">The ID of the user initiating the conversation.</param>
        /// <param name="participantIds">The IDs of other participants (excluding current user). Must include at least one other user.</param>
        /// <param name="conversationName">Optional name for group chats.</param>
        /// <returns>The ConversationDto of the newly created or retrieved conversation.</returns>
        public async Task<ConversationDto?> CreateOrGetConversationAsync(int currentUserId, IEnumerable<int> participantIds, string? conversationName = null)
        {
            _logger.LogInformation("Service: Attempting to create or get conversation for UserId: {CurrentUserId} with Participants: {ParticipantIds}", currentUserId, string.Join(", ", participantIds));

            var distinctParticipantIds = participantIds.Distinct().ToList();

            if (!distinctParticipantIds.Any())
            {
                _logger.LogWarning("Service: CreateOrGetConversationAsync called with no other participants selected.");
                return null;
            }

            var allParticipantIds = new List<int> { currentUserId };
            allParticipantIds.AddRange(distinctParticipantIds);
            allParticipantIds = allParticipantIds.Distinct().ToList(); // Ensure current user is not duplicated if already in list

            if (allParticipantIds.Count < 2)
            {
                _logger.LogWarning("Service: CreateOrGetConversationAsync requires at least two distinct participants (current user + one other). Current count: {Count}", allParticipantIds.Count);
                return null;
            }

            bool isGroupChat = allParticipantIds.Count > 2; // More than 2 participants means it's a group chat

            // --- Handle 1:1 chats first ---
            if (!isGroupChat)
            {
                // Find the other participant's ID
                var otherUserId = allParticipantIds.Except(new[] { currentUserId }).FirstOrDefault();

                if (otherUserId == default(int))
                {
                    _logger.LogError("Service: Logic error in CreateOrGetConversationAsync - failed to identify other participant for 1:1 chat.");
                    return null;
                }

                // Check if a direct conversation already exists between these two specific users.
                // This requires a new method in IConversationRepository.
                // For simplicity now, let's just create a new one if it's 1:1 and no existing one.
                // A more robust solution would check `app.conversation_participants`
                // to see if a non-group conversation exists with exactly these two participants.
                // For now, I'll add a helper method to IConversationRepository.
                var existingConversation = await _conversationRepository.GetDirectConversationByParticipantsAsync(currentUserId, otherUserId);
                if (existingConversation != null)
                {
                    _logger.LogInformation("Service: Found existing direct conversation {ConversationId} for users {User1} and {User2}.", existingConversation.ConversationId, currentUserId, otherUserId);
                    return existingConversation;
                }
            }

            // --- Create a new conversation (either new 1:1 or new group) ---
            string? name = conversationName;
            if (!isGroupChat && string.IsNullOrWhiteSpace(name))
            {
                // For a new 1:1 chat, default name to the other user's full name
                var otherUserId = allParticipantIds.Except(new[] { currentUserId }).First();
                var otherUser = await _userRepository.GetUserForAdminByIdAsync(otherUserId);
                name = otherUser?.FullName ?? "Direct Chat";
                _logger.LogDebug("Service: Defaulting 1:1 chat name to '{Name}'", name);
            }
            else if (isGroupChat && string.IsNullOrWhiteSpace(name))
            {
                // For a new group chat, default name if not provided
                name = "New Group Chat";
                _logger.LogDebug("Service: Defaulting group chat name to '{Name}'", name);
            }

            try
            {
                // Use the CreateConversationAsync from IConversationRepository that takes initialParticipantIds
                int newConversationId = await _conversationRepository.CreateConversationAsync(name, isGroupChat, currentUserId, allParticipantIds);

                if (newConversationId > 0)
                {
                    _logger.LogInformation("Service: Successfully created new conversation {ConversationId} with {ParticipantCount} participants.", newConversationId, allParticipantIds.Count);
                    // Fetch the newly created conversation details to return a complete DTO
                    return await _conversationRepository.GetConversationByIdAsync(newConversationId);
                }
                else
                {
                    _logger.LogError("Service: Failed to create conversation in repository. Conversation ID returned was 0.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while creating a new conversation.");
                return null;
            }
        }

        /// <summary>
        /// Marks all messages in a conversation as read for a specific user.
        /// This typically updates the 'last viewed at' timestamp for the user in the conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="userId">The ID of the user whose messages should be marked as read.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public async Task<bool> MarkConversationAsReadAsync(int conversationId, int userId)
        {
            _logger.LogInformation("Service: Marking ConversationId: {ConversationId} as read for UserId: {UserId}", conversationId, userId);

            if (conversationId <= 0 || userId <= 0)
            {
                _logger.LogWarning("Service: MarkConversationAsReadAsync called with invalid IDs. ConversationId: {ConversationId}, UserId: {UserId}", conversationId, userId);
                return false;
            }

            try
            {
                // This is the core action: update the participant's last_viewed_at timestamp
                bool success = await _messageRepository.UpdateParticipantLastViewedAtAsync(conversationId, userId);
                if (!success)
                {
                    _logger.LogWarning("Service: Failed to update last_viewed_at for ConversationId: {ConversationId}, UserId: {UserId}", conversationId, userId);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while marking conversation {ConversationId} as read for User {UserId}.", conversationId, userId);
                return false;
            }
        }
    }
}