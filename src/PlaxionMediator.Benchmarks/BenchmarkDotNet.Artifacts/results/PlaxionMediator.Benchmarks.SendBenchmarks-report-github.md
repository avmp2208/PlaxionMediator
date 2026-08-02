```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2


```
| Method             | Mean      | Error    | StdDev   | Gen0   | Allocated |
|------------------- |----------:|---------:|---------:|-------:|----------:|
| Send_NoPipeline    |  53.60 ns | 0.567 ns | 0.531 ns | 0.0042 |      56 B |
| Send_OneBehavior   | 119.57 ns | 2.339 ns | 3.972 ns | 0.0274 |     360 B |
| Send_FiveBehaviors | 242.83 ns | 4.703 ns | 5.416 ns | 0.0615 |     808 B |
