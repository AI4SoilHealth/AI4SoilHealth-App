namespace PublicAPI.Common.Models {
    public class MailMsg {
        public string SenderFrom { get; set; }
        public string SenderName { get; set; }
        public string Smtp { get; set; }
        public int Port { get; set; }
        public int EmailSslId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }

        public string InternalName { get; set; }
    }

    public class MailData {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Gender { get; set; }
        public string Title { get; set; }

        public string Url { get; set; }
        public int UserId { get; set; }

        public int UniqueId { get; set; }

    }
}
