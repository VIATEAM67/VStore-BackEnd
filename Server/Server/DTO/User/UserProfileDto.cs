namespace Server.DTO.User
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int LvlAcc { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ProfilePictureUrl { get; set; } = string.Empty;
    }
}
