using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.messaging
{
    /// <summary>
    /// DTO for displaying a single message.
    /// </summary>
    public class MessageDto
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }

        public int SenderUserId { get; set; }
        public string? SenderUserName { get; set; } // Populated by join
        public string? SenderUserRank { get; set; } // Populated by join

        [Required]
        public string Content { get; set; } = string.Empty; // TEXT column

        public DateTimeOffset SentAt { get; set; }
        public bool IsRead { get; set; } // This is the 'is_read' flag on the message itself (less common for individual read status)
    }
}