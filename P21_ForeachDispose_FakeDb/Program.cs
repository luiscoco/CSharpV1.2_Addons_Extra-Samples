using System;
using System.Collections;

class FakeDbRows : IEnumerable
{
    public IEnumerator GetEnumerator() { return new RowEnumerator(); }

    private class RowEnumerator : IEnumerator, IDisposable
    {
        private string[] _rows = new string[] { "row1", "row2", "row3" };
        private int _i = -1;
        private bool _opened = false;
        private bool _disposed = false;

        public RowEnumerator()
        {
            OpenConnection();
        }

        private void OpenConnection()
        {
            _opened = true;
            Console.WriteLine("DB: open connection");
        }

        private void CloseConnection()
        {
            if (_opened)
            {
                Console.WriteLine("DB: close connection");
                _opened = false;
            }
        }

        public object Current { get { return _rows[_i]; } }

        public bool MoveNext()
        {
            if (_disposed) throw new ObjectDisposedException("RowEnumerator");
            _i++; return _i < _rows.Length;
        }

        public void Reset()
        {
            if (_disposed) throw new ObjectDisposedException("RowEnumerator");
            _i = -1;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CloseConnection();
            Console.WriteLine("RowEnumerator.Dispose() called by foreach");
        }
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("Iterating DB rows with foreach (C# 1.2 calls Dispose at end):");
        foreach (string row in new FakeDbRows())
        {
            Console.WriteLine("  " + row);
        }
    }
}
