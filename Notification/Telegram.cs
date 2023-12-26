namespace WpfStatus.Notification
{
    public class Telegram : IDisposable
    {
        readonly Lazy<WTelegram.Client> client;

        readonly NotificationSettings settings;

        public Telegram(NotificationSettings settings)
        {
            this.settings = settings;

            string? Config(string what)
            {
                switch (what)
                {
                    case "api_id": return settings.TelegramApiId.ToString();
                    case "api_hash": return settings.TelegramApiHash;
                    case "phone_number": return settings.TelegramPhoneNumber;
                    case "verification_code":
                        {
                            var dialog = new VerificationCodeDialog();
                            if (dialog.ShowDialog() == true)
                            {
                                return dialog.ResponseTextBox.Text;
                            }
                            else
                            {
                                return null;
                            }
                        }
                    default: return null;
                }
            }

            client = new(() => new WTelegram.Client(Config));
        }

        public async Task Send(string Message = "Test")
        {
            var myself = await client.Value.LoginUserIfNeeded();
            try
            {
                var dialogs = await client.Value.Messages_GetAllDialogs();
                var target = dialogs.users.FirstOrDefault(
                        u => u.Value.MainUsername == settings.TelegramSendToUserName.TrimStart('@') ||
                        u.Value.username == settings.TelegramSendToUserName);
                if (target.Value != null)
                {
                    await client.Value.SendMessageAsync(target.Value, Message);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void Dispose()
        {
            if (client.IsValueCreated)
            {
                client.Value.Dispose();
            }
        }
    }
}
