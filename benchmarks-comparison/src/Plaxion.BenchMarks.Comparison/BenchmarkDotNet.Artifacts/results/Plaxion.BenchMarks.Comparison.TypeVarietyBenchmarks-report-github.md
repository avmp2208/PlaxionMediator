```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host] : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Dry    : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                    | Mean      | Error | Allocated |
|-------------------------- |----------:|------:|----------:|
| Dispatch_MediatR_50Types  |  6.853 ms |    NA |  95.78 KB |
| Dispatch_Plaxion_50Types  | 12.058 ms |    NA |   8.17 KB |
| Dispatch_Mediator_50Types | 12.339 ms |    NA |   1.05 KB |
