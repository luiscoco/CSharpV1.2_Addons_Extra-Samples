using System;
using System.Collections;

class BoomSeq : IEnumerable
{
    public IEnumerator GetEnumerator() { return new BoomEnum(); }

    private class BoomEnum : IEnumerator, IDisposable
    {
        private int _i = 0;
        public object Current { get { return _i; } }
        public bool MoveNext()
        {
            _i++;
            if (_i == 3)
            {
                Console.WriteLine("Throwing at item 3...");
                throw new InvalidOperationException("simulated failure");
            }
            return _i <= 5;
        }
        public void Reset() { _i = 0; }
        public void Dispose() { Console.WriteLine("BoomEnum.Dispose() called despite exception"); }
    }
}

class Program
{
    static void Main()
    {
        try
        {
            foreach (int n in new BoomSeq())
            {
                Console.WriteLine("  " + n);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Caught: " + ex.GetType().Name + " - " + ex.Message);
        }
    }
}
