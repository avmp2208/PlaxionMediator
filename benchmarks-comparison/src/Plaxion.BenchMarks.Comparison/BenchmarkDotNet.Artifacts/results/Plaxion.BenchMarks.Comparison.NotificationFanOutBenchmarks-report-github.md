```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host] : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Dry    : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                       | Mean      | Error | Allocated |
|----------------------------- |----------:|------:|----------:|
| Publish_Plaxion_1Handler     |  3.668 ms |    NA |   1.95 KB |
| Publish_MediatR_1Handler     |  3.809 ms |    NA |   2.15 KB |
| Publish_Plaxion_10Handlers   |  3.999 ms |    NA |   3.08 KB |
| Publish_MediatR_10Handlers   |  4.750 ms |    NA |    4.6 KB |
| Publish_Plaxion_50Handlers   |  4.954 ms |    NA |   8.98 KB |
| Publish_MediatR_50Handlers   |  5.184 ms |    NA |  14.77 KB |
| Publish_MediatR_100Handlers  |  5.901 ms |    NA |   26.2 KB |
| Publish_Plaxion_100Handlers  |  6.156 ms |    NA |  14.61 KB |
| Publish_Mediator_1Handler    | 10.150 ms |    NA |   1.49 KB |
| Publish_Mediator_10Handlers  | 11.035 ms |    NA |   2.83 KB |
| Publish_Mediator_50Handlers  | 13.714 ms |    NA |   7.23 KB |
| Publish_Mediator_100Handlers | 14.049 ms |    NA |  13.09 KB |
