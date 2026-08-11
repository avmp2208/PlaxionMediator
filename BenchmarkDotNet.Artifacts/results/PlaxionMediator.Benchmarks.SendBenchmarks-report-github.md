```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]   : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Gen0   | Allocated |
|------------------- |----------:|---------:|---------:|-------:|----------:|
| Send_NoPipeline    |  40.59 ns | 36.57 ns | 2.005 ns | 0.0079 |     104 B |
| Send_OneBehavior   |  68.78 ns | 21.44 ns | 1.175 ns | 0.0079 |     104 B |
| Send_FiveBehaviors | 105.89 ns | 20.93 ns | 1.147 ns | 0.0079 |     104 B |
