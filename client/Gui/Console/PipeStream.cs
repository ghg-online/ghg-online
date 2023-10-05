namespace client.Gui.Console
{
    public class PipeStream : Stream
    {
        readonly object _lock = new();
        readonly AutoResetEvent _event = new(false);
        PipeStream[] peers;
        readonly IBuffer buffer;

        public PipeStream()
        {
            peers = Array.Empty<PipeStream>();
            buffer = new Buffer();
        }

        public Stream[] Peers
        {
            get { return peers; }
            set
            {
                lock (_lock)
                {
                    Array.Resize(ref peers, value.Length);
                    value.CopyTo(peers, 0);
                }
            }
        }

        public static void EnsureConnection(PipeStream a, PipeStream b)
        {
            lock (a._lock)
            {
                lock (b._lock)
                {
                    if (false == a.peers.Contains(b))
                        a.peers = a.peers.Append(b).ToArray();
                    if (false == b.peers.Contains(a))
                        b.peers = b.peers.Append(a).ToArray();
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read;
            while ((read = this.buffer.Read(buffer, offset, count)) == 0)
            {
                Thread.Sleep(1);
            }
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                int length = peers.Length;
                for (int i = 0; i < length; i++)
                {
                    peers[i].buffer.Write(buffer, offset, count);
                    peers[i]._event.Set();
                }
            }
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override void Flush() { }
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotImplementedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
