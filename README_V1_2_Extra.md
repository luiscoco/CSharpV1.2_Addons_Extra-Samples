# C# 1.2 – Extra foreach/Dispose Demos

C# 1.2 changed the compiler so that **foreach calls `Dispose()` on the enumerator** when it implements `IDisposable`. These extra samples broaden real-world coverage beyond the basics.

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
