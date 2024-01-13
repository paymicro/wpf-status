# Windows-only status window for spacemesh nodes
![image](https://github.com/paymicro/wpf-status/assets/27482193/af7f7fd0-fab5-4ed0-8be9-5d56c6af99fb)

## Notifications
### Telegram
Login as a regular user. This user should have a dialog with your primary user who will receive notifications.

To enable it, you need to fill in the settings.json file your `telegramApiHash` and  `telegramApiId` from the user who will send the notifications. That you obtain through [Telegram's API development tools](https://my.telegram.org/apps) page.

`telegramPhoneNumber` - in the international format - the user who sends notifications.

`telegramSendToUserName` - @alias of your telegram user who gets notifications. The dialog between users should already exist!

`delaySec` - minimum delay between notifications. To avoid spamming. If there is a new notification, but less time has passed since the previous one. That new notification will not be sent.

`enabled` - is enabled after start.
