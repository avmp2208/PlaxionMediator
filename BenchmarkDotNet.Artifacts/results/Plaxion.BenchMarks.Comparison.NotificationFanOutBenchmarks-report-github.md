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
| Publish_Plaxion_1Handler     |  3.430 ms |    NA |    2.6 KB |
| Publish_MediatR_10Handlers   |  3.885 ms |    NA |   5.11 KB |
| Publish_MediatR_1Handler     |  3.887 ms |    NA |   2.43 KB |
| Publish_Plaxion_10Handlers   |  3.897 ms |    NA |   3.36 KB |
| Publish_Plaxion_50Handlers   |  4.618 ms |    NA |   8.93 KB |
| Publish_MediatR_50Handlers   |  5.103 ms |    NA |  14.48 KB |
| Publish_MediatR_100Handlers  |  5.882 ms |    NA |   26.2 KB |
| Publish_Plaxion_100Handlers  |  5.973 ms |    NA |  15.18 KB |
| Publish_Mediator_1Handler    |  9.752 ms |    NA |   1.49 KB |
| Publish_Mediator_10Handlers  | 10.896 ms |    NA |   2.55 KB |
| Publish_Mediator_50Handlers  | 11.519 ms |    NA |   7.23 KB |
| Publish_Mediator_100Handlers | 15.842 ms |    NA |  13.09 KB |
