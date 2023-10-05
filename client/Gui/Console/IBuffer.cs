namespace client.Gui.Console
{
    public interface IBuffer
    {
        void Write(byte[] buffer, int offset, int count);
        int Read(byte[] buffer, int offset, int count);
    }
}
