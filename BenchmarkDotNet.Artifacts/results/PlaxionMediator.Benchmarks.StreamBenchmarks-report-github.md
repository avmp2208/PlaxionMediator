```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2


```
| Method           | Mean     | Error   | StdDev   | Allocated |
|----------------- |---------:|--------:|---------:|----------:|
| Stream_1000Items | 483.4 μs | 9.60 μs | 16.82 μs |     968 B |
