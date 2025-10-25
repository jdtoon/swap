// carestream.core.interfaces.repositories/IConversationRepository.cs
using carestream.core.dtos.messaging;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for messaging conversations.
    /// </summary>
    public interface IConversationRepository
    {
        /// <summary>
        /// Retrieves a conversation by its unique ID.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="ConversationDto"/> if found; otherwise, null.</returns>
        Task<ConversationDto?> GetConversationByIdAsync(int conversationId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a list of conversations a specific user is a participant in.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="ConversationDto"/> for the user.</returns>
        Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(int userId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new conversation.
        /// </summary>
        /// <param name="name">Optional name for the conversation (for group chats).</param>
        /// <param name="isGroupChat">Indicates if it's a group chat.</param>
        /// <param name="createdByUserId">The ID of the user creating the conversation.</param>
        /// <param name="initialParticipantIds">A list of user IDs to include as initial participants.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created conversation, or 0 if creation failed.</returns>
        Task<int> CreateConversationAsync(string? name, bool isGroupChat, int createdByUserId, IEnumerable<int> initialParticipantIds, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Adds a user as a participant to an existing conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="userId">The ID of the user to add.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the participant was added, false otherwise (e.g., already a participant).</returns>
        Task<bool> AddParticipantToConversationAsync(int conversationId, int userId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Removes a user from a conversation (e.g., leaving a group chat).
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="userId">The ID of the user to remove.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the participant was removed, false otherwise.</returns>
        Task<bool> RemoveParticipantFromConversationAsync(int conversationId, int userId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates the last_message_at timestamp for a conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the timestamp was updated, false otherwise.</returns>
        Task<bool> UpdateConversationLastMessageTimestampAsync(int conversationId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Attempts to find an existing direct (1:1) conversation between two specific users.
        /// </summary>
        /// <param name="userId1">The ID of the first user.</param>
        /// <param name="userId2">The ID of the second user.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="ConversationDto"/> if a unique 1:1 conversation exists; otherwise, null.</returns>
        Task<ConversationDto?> GetDirectConversationByParticipantsAsync(int userId1, int userId2, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}