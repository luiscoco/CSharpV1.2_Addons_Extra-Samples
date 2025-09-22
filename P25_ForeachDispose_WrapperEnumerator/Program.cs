using System;
using System.Collections;

class BaseSeq : IEnumerable
{
    public IEnumerator GetEnumerator() { return new BaseEnum(); }

    private class BaseEnum : IEnumerator, IDisposable
    {
        private int i = 0;
        public object Current { get { return i; } }
        public bool MoveNext() { i++; return i <= 2; }
        public void Reset() { i = 0; }
        public void Dispose() { Console.WriteLine("BaseEnum.Dispose() called"); }
    }
}

class WrapperSeq : IEnumerable
{
    public IEnumerator GetEnumerator() { return new WrapperEnum(new BaseSeq().GetEnumerator()); }

    private class WrapperEnum : IEnumerator, IDisposable
    {
        private IEnumerator _inner;
        public WrapperEnum(IEnumerator inner) { _inner = inner; }
        public object Current { get { return _inner.Current; } }
        public bool MoveNext() { return _inner.MoveNext(); }
        public void Reset() { _inner.Reset(); }
        public void Dispose()
        {
            Console.WriteLine("WrapperEnum.Dispose() called");
            IDisposable d = _inner as IDisposable;
            if (d != null) d.Dispose();
        }
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("Wrapper enumerator forwards Dispose to inner enumerator:");
        foreach (int v in new WrapperSeq())
        {
            Console.WriteLine("  " + v);
        }
    }
}
