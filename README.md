# C# 1.2 – Extra foreach/Dispose Demos

In C# 1.2, the language added an important guarantee:  if the enumerator returned by `GetEnumerator()` implements `IDisposable`, the compiler automatically wraps the `foreach` in a hidden `try/finally` and calls `Dispose()` at the end of iteration.

That means resources like database readers, sockets, and streams are cleaned up automatically when a `foreach` finishes—whether it ends normally, via `break`/`return`, or because of an exception.

**Compiler-equivalent desugaring looks like this:**

```csharp
var e = source.GetEnumerator();
try
{
    while (e.MoveNext())
    {
        var item = e.Current;
        // loop body
    }
}
finally
{
    (e as IDisposable)?.Dispose();
}
```

The repo contains five small projects that each stress-test this feature with real-world scenarios.

## Projects
- **P21_ForeachDispose_FakeDb** — Simulated DB row reader; shows connection open/close and auto-dispose at end of foreach.
- **P22_ForeachDispose_FakeSocket** — Simulated network socket; auto-close on foreach completion.
- **P23_ForeachDispose_NestedEnumerators** — Nested foreach loops; each enumerator gets its own Dispose call.
- **P24_ForeachDispose_ExceptionMidLoop** — Enumerator throws mid-iteration; Dispose still runs via compiler-generated finally.
- **P25_ForeachDispose_WrapperEnumerator** — Enumerator that wraps another enumerator; wrapper forwards Dispose to inner.

## Build
```bash
dotnet restore
dotnet build
dotnet run --project P21_ForeachDispose_FakeDb
```
All projects target `.NET 10.0` and force **C# 1.0 syntax** via `Directory.Build.props`. The 1.2-specific behavior here is demonstrated by the compiler calling `Dispose()` after each foreach, including in exceptional and nested cases.

## P21_ForeachDispose_FakeDb — simulated DB reader

**Idea:** Simulate a `SqlDataReader`-style enumerator that opens and closes a database connection.

```csharp
public sealed class FakeDbRows : IEnumerable<string>
{
    public IEnumerator<string> GetEnumerator() => new FakeDbEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class FakeDbEnumerator : IEnumerator<string>, IDisposable
    {
        private int _i = -1;
        private readonly string[] _rows = { "row1", "row2", "row3" };
        private bool _opened;

        public FakeDbEnumerator()
        {
            _opened = true;
            Console.WriteLine("DB: open connection/reader");
        }

        public string Current => _rows[_i];
        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _i++;
            return _i < _rows.Length;
        }

        public void Reset() => _i = -1;

        public void Dispose()
        {
            if (_opened)
            {
                Console.WriteLine("DB: dispose/close reader+connection");
                _opened = false;
            }
        }
    }
}

static void Main()
{
    foreach (var row in new FakeDbRows())
    {
        Console.WriteLine($"Processing {row}");
        if (row == "row2") break; // still triggers Dispose()
    }
}
```

✅ **Takeaway:** Even with an early `break`, the connection is still disposed. No extra `using` needed around the enumerator itself.

---

## P22_ForeachDispose_FakeSocket — simulated network packets

**Idea:** Model an enumerator that represents receiving data packets from a socket.

```csharp
public sealed class FakeSocketPackets : IEnumerable<byte[]>
{
    public IEnumerator<byte[]> GetEnumerator() => new SocketEnum();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class SocketEnum : IEnumerator<byte[]>, IDisposable
    {
        private int _i = -1;
        private readonly byte[][] _packets = {
            new byte[]{1,2}, new byte[]{3,4}, new byte[]{5,6}
        };
        private bool _connected;

        public SocketEnum()
        {
            _connected = true;
            Console.WriteLine("Socket: CONNECT");
        }

        public byte[] Current => _packets[_i];
        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext() => ++_i < _packets.Length;
        public void Reset() => _i = -1;

        public void Dispose()
        {
            if (_connected)
            {
                Console.WriteLine("Socket: CLOSE");
                _connected = false;
            }
        }
    }
}

static void Main()
{
    foreach (var packet in new FakeSocketPackets())
        Console.WriteLine($"Got {packet.Length} bytes");
}
```

✅ **Takeaway:** The socket connection is always closed at the end of iteration—automatic cleanup with `foreach`.

---

## P23_ForeachDispose_NestedEnumerators — nested loops

**Idea:** Test two `foreach` loops (outer and inner), each with disposable enumerators.

```csharp
static IEnumerable<int> Outer()
{
    Console.WriteLine("Outer: open");
    try { yield return 1; yield return 2; }
    finally { Console.WriteLine("Outer: dispose"); }
}

static IEnumerable<char> Inner(int n)
{
    Console.WriteLine($"Inner({n}): open");
    try { yield return 'A'; yield return 'B'; }
    finally { Console.WriteLine($"Inner({n}): dispose"); }
}

static void Main()
{
    foreach (var n in Outer())
    {
        foreach (var c in Inner(n))
        {
            Console.WriteLine($"{n}:{c}");
        }
    }
}
```

✅ **Takeaway:** Both enumerators are disposed independently:
- The inner enumerator is disposed at the end of each inner loop.
- The outer enumerator is disposed at the end of the outer loop.

---

## P24_ForeachDispose_ExceptionMidLoop — exception handling

**Idea:** Ensure cleanup still runs if the loop is interrupted by an exception.

```csharp
public sealed class ThrowsHalfway : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => new E();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class E : IEnumerator<int>, IDisposable
    {
        private int _i = -1;
        public int Current => _i;
        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _i++;
            if (_i == 2) throw new InvalidOperationException("boom");
            return _i < 5;
        }

        public void Reset() => _i = -1;
        public void Dispose() => Console.WriteLine("Enumerator: disposed");
    }
}

static void Main()
{
    try
    {
        foreach (var x in new ThrowsHalfway())
            Console.WriteLine(x);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}
```

✅ **Takeaway:** Even though the enumerator throws an exception at element 2, `Dispose()` is still called. Resources don’t leak.

---

## P25_ForeachDispose_WrapperEnumerator — wrapper pattern

**Idea:** An enumerator wraps another enumerator and forwards calls, including `Dispose()`.

```csharp
public sealed class Wrapper<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> _inner;
    public Wrapper(IEnumerable<T> inner) => _inner = inner;

    public IEnumerator<T> GetEnumerator() => new WrapEnum(_inner.GetEnumerator());
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class WrapEnum : IEnumerator<T>, IDisposable
    {
        private readonly IEnumerator<T> _inner;
        public WrapEnum(IEnumerator<T> inner)
        {
            _inner = inner;
            Console.WriteLine("Wrapper: open");
        }

        public T Current => _inner.Current;
        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext() => _inner.MoveNext();
        public void Reset() => _inner.Reset();

        public void Dispose()
        {
            Console.WriteLine("Wrapper: dispose");
            (_inner as IDisposable)?.Dispose(); // forward to inner
        }
    }
}

static void Main()
{
    foreach (var n in new Wrapper<int>(Enumerable.Range(1, 3)))
        Console.WriteLine(n);
}
```

✅ **Takeaway:** Both wrapper and inner enumerator are disposed properly. Useful when building decorators around enumerations.

---

## Why this matters in practice

- `foreach` always cleans up disposable enumerators—on normal completion, `break`, or exceptions.
- If you implement a custom enumerator that manages scarce resources (files, sockets, DB handles), implement `IDisposable` and put teardown logic in `Dispose()`. The compiler ensures it gets called.
- Works correctly with nested loops and wrapper enumerators.
- Arrays and simple collections don’t require this, but resource-backed iterators do.

👉 This repo is essentially a teaching collection that proves the compiler’s guarantee across multiple realistic scenarios.


