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
| Send_MediatR_0Behaviors   |  2.885 ms |    NA |    3560 B |
| Send_Plaxion_0Behaviors   |  3.966 ms |    NA |    1928 B |
| Send_MediatR_1Behavior    |  3.967 ms |    NA |    3656 B |
| Send_MediatR_5Behaviors   |  5.842 ms |    NA |    5392 B |
| Send_Plaxion_1Behavior    |  6.083 ms |    NA |    2816 B |
| Send_MediatR_10Behaviors  |  7.851 ms |    NA |    7336 B |
| Send_Plaxion_5Behaviors   |  8.817 ms |    NA |    3776 B |
| Send_Plaxion_10Behaviors  | 10.208 ms |    NA |    4976 B |
| Send_Mediator_0Behaviors  | 10.538 ms |    NA |     784 B |
| Send_MediatR_20Behaviors  | 11.630 ms |    NA |    9584 B |
| Send_Mediator_1Behavior   | 11.698 ms |    NA |     912 B |
| Send_Mediator_5Behaviors  | 14.361 ms |    NA |    1424 B |
| Send_Plaxion_20Behaviors  | 14.525 ms |    NA |    6504 B |
| Send_Mediator_10Behaviors | 17.741 ms |    NA |    2064 B |
| Send_Mediator_20Behaviors | 25.184 ms |    NA |    2624 B |
