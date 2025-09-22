using System;
using System.Collections;

class Outer : IEnumerable
{
    public IEnumerator GetEnumerator() { return new OuterEnum(); }

    private class OuterEnum : IEnumerator, IDisposable
    {
        private int _i = -1;
        private IDisposable _inner; // track inner to show dispose order
        public object Current { get { return null; } }

        public bool MoveNext()
        {
            _i++;
            if (_i >= 2) return false;
            return true;
        }

        public void Reset() { _i = -1; }

        public void Dispose()
        {
            Console.WriteLine("OuterEnum.Dispose() called");
            if (_inner != null) _inner.Dispose();
        }

        public IEnumerator Inner()
        {
            InnerEnum e = new InnerEnum();
            _inner = e;
            return e;
        }

        private class InnerEnum : IEnumerator, IDisposable
        {
            private int _j = 0;
            public object Current { get { return _j; } }
            public bool MoveNext() { _j++; return _j <= 2; }
            public void Reset() { _j = 0; }
            public void Dispose() { Console.WriteLine("  InnerEnum.Dispose() called"); }
        }
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("Nested foreach: each enumerator gets its own Dispose:");
        foreach (object _ in new Outer())
        {
            // start a nested foreach over the inner
            Outer o = new Outer();
            // Access Inner via reflection of pattern: for demo simplicity,
            // we re-create an inner enumerator here.
            foreach (int x in new InnerEnumerable())
            {
                Console.WriteLine("  value " + x);
            }
        }
    }
}

class InnerEnumerable : IEnumerable
{
    public IEnumerator GetEnumerator() { return new InnerEnum(); }
    private class InnerEnum : IEnumerator, IDisposable
    {
        private int _j = 0;
        public object Current { get { return _j; } }
        public bool MoveNext() { _j++; return _j <= 2; }
        public void Reset() { _j = 0; }
        public void Dispose() { Console.WriteLine("  InnerEnumerable.InnerEnum.Dispose() called"); }
    }
}
