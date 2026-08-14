```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2


```
| Method             | Mean      | Error    | StdDev   | Gen0   | Allocated |
|------------------- |----------:|---------:|---------:|-------:|----------:|
| Send_NoPipeline    |  32.63 ns | 0.558 ns | 0.495 ns | 0.0042 |      56 B |
| Send_OneBehavior   |  85.57 ns | 1.735 ns | 3.038 ns | 0.0079 |     104 B |
| Send_FiveBehaviors | 136.22 ns | 2.741 ns | 3.047 ns | 0.0079 |     104 B |
