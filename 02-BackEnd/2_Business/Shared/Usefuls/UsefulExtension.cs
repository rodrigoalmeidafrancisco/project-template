using Flunt.Notifications;

namespace Shared.Usefuls
{
    public static class UsefulExtension
    {
        public static List<string> SelectFluntNotifications(this IReadOnlyCollection<Notification> listNotification)
        {
            var listReturn = new List<string>();

            if (listNotification != null && listNotification.Count != 0)
            {
                foreach (Notification notification in listNotification)
                {
                    if (listReturn.Any(x => x.Equals(notification.Message, StringComparison.OrdinalIgnoreCase)) == false)
                    {
                        listReturn.Add(notification.Message);
                    }
                }
            }

            return [.. listReturn.Distinct()];
        }

    }
}
