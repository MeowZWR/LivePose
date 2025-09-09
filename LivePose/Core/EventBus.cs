using LivePose.UI;

namespace LivePose.Core;

public class EventBus
{
    public static EventBus Instance { get; private set; } = null!;

    public EventBus()
    {
        Instance = this;
    }

    public void NotifyError(string message)
    {
        UIManager.Instance.NotifyError(message);
    }
}
