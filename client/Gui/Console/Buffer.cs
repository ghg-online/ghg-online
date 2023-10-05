namespace client.Gui.Console
{
    public class Buffer : IBuffer
    {
        readonly Queue<byte> queue = new();

        public int Read(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (queue.Count == 0)
                    return i;
                buffer[offset + i] = queue.Dequeue();
            }
            return count;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                queue.Enqueue(buffer[offset + i]);
        }
    }
}
