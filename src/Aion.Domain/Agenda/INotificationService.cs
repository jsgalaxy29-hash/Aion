using System.Threading;
using System.Threading.Tasks;

namespace Aion.Domain.Agenda;

public interface INotificationService
{
    Task NotifyAsync(SNotification notification, CancellationToken ct = default);
    Task SendReminderAsync(SAgendaReminder reminder, CancellationToken ct = default);
}
