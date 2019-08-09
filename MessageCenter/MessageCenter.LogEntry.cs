namespace BepisPlugins
{
    public partial class MessageCenter
    {
        private sealed class LogEntry
        {
            public LogEntry(string text) => Text = text;

            public int Count { get; set; }

            public string Text { get; }
        }
    }
}
