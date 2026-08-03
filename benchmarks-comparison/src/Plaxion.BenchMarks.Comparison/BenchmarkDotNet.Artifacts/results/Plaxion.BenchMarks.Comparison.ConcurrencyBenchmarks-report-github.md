```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host] : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Dry    : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                  | Mean      | Error | Completed Work Items | Lock Contentions | Allocated |
|------------------------ |----------:|------:|---------------------:|-----------------:|----------:|
| Concurrent_MediatR_1    |  3.396 ms |    NA |                    - |                - |   3.58 KB |
| Concurrent_MediatR_128  |  3.637 ms |    NA |                    - |                - |  35.09 KB |
| Concurrent_MediatR_8    |  3.644 ms |    NA |                    - |                - |   3.22 KB |
| Concurrent_Plaxion_32   |  4.535 ms |    NA |                    - |                - |   3.31 KB |
| Concurrent_Plaxion_1    |  4.576 ms |    NA |                    - |                - |   1.65 KB |
| Concurrent_MediatR_32   |  4.676 ms |    NA |                    - |                - |   9.59 KB |
| Concurrent_Plaxion_8    |  5.032 ms |    NA |                    - |                - |   1.44 KB |
| Concurrent_Plaxion_128  |  5.107 ms |    NA |                    - |                - |  10.81 KB |
| Concurrent_Mediator_1   | 11.119 ms |    NA |                    - |                - |   1.22 KB |
| Concurrent_Mediator_32  | 11.247 ms |    NA |                    - |                - |   3.36 KB |
| Concurrent_Mediator_128 | 12.066 ms |    NA |                    - |                - |  10.86 KB |
| Concurrent_Mediator_8   | 13.678 ms |    NA |                    - |                - |   1.77 KB |
