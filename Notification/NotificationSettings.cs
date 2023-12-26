namespace WpfStatus.Notification
{
    public class NotificationSettings
    {
        public int TelegramApiId { get; set; } = 0;

        public string TelegramApiHash { get; set; } = string.Empty;

        public string TelegramPhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// @alias or user name
        /// </summary>
        public string TelegramSendToUserName { get; set; } = string.Empty;

        public int DalaySec { get; set; } = 300;

        public bool Enabled { get; set; } = false;
    }
}
