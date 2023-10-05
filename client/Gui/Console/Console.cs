using System.Text;
using client.Utils;
using Terminal.Gui;
using Terminal.ScreenLibrary;

namespace client.Gui.Console
{
    public class Console : ConsoleScreen, IDisposable
    {
        public Console(int width, int height, Color foreground, Color background)
            : base(width, height, background)
        {
            pipeStream = new PipeStream();
            textReader = new StreamReader(pipeStream);
            textWriter = new StreamWriter(pipeStream);
            cancellationTokenSource = new CancellationTokenSource();
            screen = new Screen(new ScreenDriver(this), foreground.ToSystemColor(), background.ToSystemColor());
            Listen(cancellationTokenSource.Token);
            Enabled = true;
            KeyPress += (args) => { if (OnKeyPress(args.KeyEvent)) args.Handled = true; };
        }

        public void Run(Action<PipeStream> program)
        {
            var pipe = new PipeStream();
            PipeStream.EnsureConnection(pipeStream, pipe);
            Task.Run(() => program(pipe));
        }

        public void Shutdown()
        {
            cancellationTokenSource.Cancel();
            pipeStream.Close();
            textReader.Close();
            textWriter.Close();
        }

        public new void Dispose()
        {
            Shutdown();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        readonly PipeStream pipeStream;
        readonly TextReader textReader;
        readonly TextWriter textWriter;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly IScreen screen;

        private async void Listen(CancellationToken cancellationToken)
        {
            while (true)
            {
                Memory<char> buffer = new char[32];
                int read = await textReader.ReadAsync(buffer, cancellationToken);
                string text = new(buffer.Span[..read]);
                screen.HandleString(text);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }



        private bool OnKeyPress(KeyEvent keyEvent)
        {
            try
            {
                string input = char.ConvertFromUtf32(keyEvent.KeyValue);
                pipeStream.Write(Encoding.UTF8.GetBytes(input), 0, input.Length);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
