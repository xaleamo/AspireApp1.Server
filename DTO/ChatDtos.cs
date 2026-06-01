namespace AspireApp1.Server.DTO
{
    public class ChatMessageDto
    {
        public string Id { get; set; } = "";
        public ChatSenderDto Sender { get; set; } = new();
        public string Text { get; set; } = "";
        public DateTime SentAt { get; set; }
    }

    public class ChatSenderDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string Surname { get; set; } = "";
        public string Role { get; set; } = "";
    }

    public class SendMessageDto
    {
        public ChatSenderDto Sender { get; set; } = new();
        public string Text { get; set; } = "";
    }
}
