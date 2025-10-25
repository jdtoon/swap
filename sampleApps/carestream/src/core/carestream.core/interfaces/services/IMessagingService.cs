using carestream.core.dtos.admin;
using carestream.core.dtos.messaging;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines the business logic for internal messaging operations.
    /// </summary>
    public interface IMessagingService
    {
        /// <summary>
        /// Retrieves a list of conversations for a specific user, including unread message counts.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>An enumerable of <see cref="ConversationDto"/> representing the user's conversations.</returns>
        Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(int userId);

        /// <summary>
        /// Retrieves a paginated list of messages within a specific conversation.
        /// Also updates the user's 'last viewed at' timestamp for that conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to retrieve messages from.</param>
        /// <param name="userId">The ID of the user viewing the conversation.</param>
        /// <param name="limit">The maximum number of messages to retrieve.</param>
        /// <param name="offset">The offset from which to start retrieving messages.</param>
        /// <returns>An enumerable of <see cref="MessageDto"/> representing the messages in the conversation.</returns>
        Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int userId, int limit = 100, int offset = 0);

        /// <summary>
        /// Sends a new message in a conversation.
        /// </summary>
        /// <param name="messageData">The DTO containing the message content and sender information.</param>
        /// <returns>A <see cref="MessageDto"/> representing the newly sent message, or null if sending failed.</returns>
        Task<MessageDto?> SendMessageAsync(CreateMessageInputDto messageData);

        /// <summary>
        /// Retrieves the total count of unread messages for a specific user across all their conversations.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The total number of unread messages.</returns>
        Task<int> GetUserUnreadMessagesCountAsync(int userId);

        /// <summary>
        /// Searches for internal users based on a search term, excluding the current user.
        /// </summary>
        /// <param name="searchTerm">The term to search for (e.g., name, rank, department).</param>
        /// <param name="currentUserId">The ID of the current user, to exclude them from results.</param>
        /// <returns>An enumerable of AdminUserListItemDto representing the found users.</returns>
        Task<IEnumerable<AdminUserListItemDto>> SearchInternalUsersAsync(string searchTerm, int currentUserId);

        /// <summary>
        /// Creates a new conversation or retrieves an existing direct conversation.
        /// </summary>
        /// <param name="currentUserId">The ID of the user initiating the conversation.</param>
        /// <param name="participantIds">The IDs of other participants (excluding current user). Must include at least one other user.</param>
        /// <param name="conversationName">Optional name for group chats.</param>
        /// <returns>The ConversationDto of the newly created or retrieved conversation.</returns>
        Task<ConversationDto?> CreateOrGetConversationAsync(int currentUserId, IEnumerable<int> participantIds, string? conversationName = null);

        /// <summary>
        /// Marks all messages in a conversation as read for a specific user.
        /// This typically updates the 'last viewed at' timestamp for the user in the conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="userId">The ID of the user whose messages should be marked as read.</param>
        /// <returns>True if successful, false otherwise.</returns>
        Task<bool> MarkConversationAsReadAsync(int conversationId, int userId);
    }
}