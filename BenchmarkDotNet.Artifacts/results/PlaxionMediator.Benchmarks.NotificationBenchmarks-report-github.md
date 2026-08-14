```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2


```
| Method               | Mean      | Error    | StdDev   | Gen0   | Allocated |
|--------------------- |----------:|---------:|---------:|-------:|----------:|
| Publish_OneHandler   |  54.59 ns | 1.106 ns | 1.817 ns | 0.0024 |      32 B |
| Publish_FiveHandlers |  74.12 ns | 1.364 ns | 1.209 ns | 0.0049 |      64 B |
| Publish_TenHandlers  | 101.38 ns | 1.637 ns | 1.451 ns | 0.0079 |     104 B |
