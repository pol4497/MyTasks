namespace MyTasks.Dtos
{
    public class GuestSessionResponseDto
    {
        /// <summary>
        /// The raw guest token. Shown exactly once - only its hash is stored, so the
        /// caller must save it and send it back as the X-Guest-Token header on task
        /// requests. There's no way to recover it if lost; a new session must be created.
        /// </summary>
        public string GuestToken { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }
}
