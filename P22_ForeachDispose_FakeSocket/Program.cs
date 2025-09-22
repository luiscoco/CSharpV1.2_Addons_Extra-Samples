using System;
using System.Collections;

class FakeSocketMessages : IEnumerable
{
    public IEnumerator GetEnumerator() { return new SocketEnumerator(); }

    private class SocketEnumerator : IEnumerator, IDisposable
    {
        private string[] _msgs = new string[] { "hello", "world", "!" };
        private int _i = -1;
        private bool _connected = false;
        private bool _disposed = false;

        public SocketEnumerator()
        {
            _connected = true;
            Console.WriteLine("Socket: connect");
        }

        public object Current { get { return _msgs[_i]; } }

        public bool MoveNext()
        {
            if (_disposed) throw new ObjectDisposedException("SocketEnumerator");
            _i++; return _i < _msgs.Length;
        }

        public void Reset()
        {
            if (_disposed) throw new ObjectDisposedException("SocketEnumerator");
            _i = -1;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_connected) { Console.WriteLine("Socket: close"); _connected = false; }
            Console.WriteLine("SocketEnumerator.Dispose() called by foreach");
        }
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("Iterating socket messages with foreach:");
        foreach (string m in new FakeSocketMessages())
        {
            Console.WriteLine("  " + m);
        }
    }
}
