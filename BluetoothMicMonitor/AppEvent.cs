namespace BluetoothMicMonitor
{
    public class AppEvent
    {
        public string Type { get; set; }
        public string ProcessName { get; set; }

        public AppEvent()
        {
            Type = "";
            ProcessName = "";
        }
    }
}
