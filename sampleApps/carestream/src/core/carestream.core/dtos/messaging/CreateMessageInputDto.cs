using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.messaging
{
    /// <summary>
    /// DTO for creating a new message.
    /// </summary>
    public class CreateMessageInputDto
    {
        [Required]
        public int ConversationId { get; set; }

        [Required]
        public int SenderUserId { get; set; }

        [Required]
        [StringLength(4000)] // Assuming reasonable max length for message content
        public string Content { get; set; } = string.Empty;
    }
}