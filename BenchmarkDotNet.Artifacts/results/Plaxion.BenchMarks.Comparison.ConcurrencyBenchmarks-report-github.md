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
| Concurrent_MediatR_1    |  3.254 ms |    NA |                    - |                - |      4 KB |
| Concurrent_MediatR_128  |  3.497 ms |    NA |                    - |                - |  35.09 KB |
| Concurrent_MediatR_8    |  3.613 ms |    NA |                    - |                - |   3.22 KB |
| Concurrent_MediatR_32   |  3.856 ms |    NA |                    - |                - |   9.31 KB |
| Concurrent_Plaxion_1    |  3.896 ms |    NA |                    - |                - |   1.65 KB |
| Concurrent_Plaxion_8    |  4.269 ms |    NA |                    - |                - |   1.72 KB |
| Concurrent_Plaxion_128  |  4.675 ms |    NA |                    - |                - |  10.81 KB |
| Concurrent_Plaxion_32   |  4.745 ms |    NA |                    - |                - |   3.31 KB |
| Concurrent_Mediator_32  | 10.278 ms |    NA |                    - |                - |   3.64 KB |
| Concurrent_Mediator_128 | 10.476 ms |    NA |                    - |                - |  10.86 KB |
| Concurrent_Mediator_1   | 10.513 ms |    NA |                    - |                - |   1.22 KB |
| Concurrent_Mediator_8   | 10.619 ms |    NA |                    - |                - |   1.48 KB |
