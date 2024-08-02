public interface IClientEventStreamSender
{
    void TrackEvent(EventData eventData);
}