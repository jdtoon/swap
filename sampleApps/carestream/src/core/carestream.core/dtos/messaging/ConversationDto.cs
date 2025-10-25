using System;
using System.Collections.Generic; // For List
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.messaging
{
    /// <summary>
    /// DTO for displaying conversation information.
    /// </summary>
    public class ConversationDto
    {
        public int ConversationId { get; set; }

        [StringLength(255)]
        public string? Name { get; set; } // Null for direct 1:1 chats, or set for group chats

        public bool IsGroupChat { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; } // Populated by join

        public DateTimeOffset LastMessageAt { get; set; }

        public DateTimeOffset? LastViewedAt { get; set; } // From conversation_participants, for unread status
        public int UnreadCount { get; set; } // Calculated, not from DB directly
    }
}