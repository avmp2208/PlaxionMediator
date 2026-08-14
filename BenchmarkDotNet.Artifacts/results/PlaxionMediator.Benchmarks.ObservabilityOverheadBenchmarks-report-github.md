```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2


```
| Method                | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| Publish_OpenTelemetry | 173.9 ns | 3.30 ns | 3.53 ns |  0.94 |    0.02 | 0.0098 |     128 B |        0.64 |
| Publish_NoObserver    | 174.9 ns | 3.39 ns | 4.52 ns |  0.95 |    0.03 | 0.0098 |     128 B |        0.64 |
| Send_NoObserver       | 184.5 ns | 3.56 ns | 3.16 ns |  1.00 |    0.02 | 0.0153 |     200 B |        1.00 |
| Send_OpenTelemetry    | 185.5 ns | 3.45 ns | 3.06 ns |  1.01 |    0.02 | 0.0153 |     200 B |        1.00 |
