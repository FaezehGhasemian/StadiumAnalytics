
namespace StadiumAnalytics.Application.Abstractions.Eventing
{
    public interface IEventBus
    {
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : class;
    }
}
