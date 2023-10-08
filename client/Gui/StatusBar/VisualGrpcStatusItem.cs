using Terminal.Gui;

namespace client.Gui.StatusBar
{
    public class VisualGrpcStatusItem : StatusItem
    {
        private int requestCount = 0;

        private VisualGrpcStatusItem() : base(Key.Null, "Idle", () => { }) { }

        public static VisualGrpcStatusItem Instance { get; } = new();

        public void SyncDisplay()
        {
            lock (this)
            {
                if (requestCount > 0)
                    Title = $"Waiting server: {requestCount}";
                else
                    Title = "Idle";
                Application.MainLoop.Invoke(() =>
                {
                    Application.Refresh();
                });
            }
        }

        public void Increase()
        {
            lock (this)
            {
                requestCount++;
                SyncDisplay();
            }
        }

        public void Decrease()
        {
            lock (this)
            {
                requestCount--;
                SyncDisplay();
            }
        }
    }
}
