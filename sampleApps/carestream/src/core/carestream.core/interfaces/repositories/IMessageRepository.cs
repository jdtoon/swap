using carestream.core.dtos.messaging; // For MessageDto, CreateMessageInputDto
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic; // For IEnumerable

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for individual messages within conversations.
    /// </summary>
    public interface IMessageRepository
    {
        /// <summary>
        /// Retrieves messages for a specific conversation, ordered by send time.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="limit">Optional limit for number of messages.</param>
        /// <param name="offset">Optional offset for pagination.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="MessageDto"/>.</returns>
        Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int limit = 100, int offset = 0, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new message in a conversation.
        /// </summary>
        /// <param name="messageData">The DTO containing the message content and sender.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created message, or 0 if creation failed.</returns>
        Task<int> CreateMessageAsync(CreateMessageInputDto messageData, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Marks a specific message as read.
        /// </summary>
        /// <param name="messageId">The ID of the message to mark as read.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the message was marked as read, false otherwise.</returns>
        Task<bool> MarkMessageAsReadAsync(int messageId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates the last_viewed_at timestamp for a specific user in a conversation.
        /// Used to track which messages a user has seen.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the timestamp was updated, false otherwise.</returns>
        Task<bool> UpdateParticipantLastViewedAtAsync(int conversationId, int userId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves the count of unread messages for a specific user in a given conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The number of unread messages.</returns>
        Task<int> GetUnreadMessagesCountInConversationAsync(int conversationId, int userId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}