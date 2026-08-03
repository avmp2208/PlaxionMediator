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
| Send_MediatR_0Behaviors   |  2.432 ms |    NA |    3560 B |
| Send_MediatR_1Behavior    |  3.726 ms |    NA |    3656 B |
| Send_Plaxion_0Behaviors   |  4.414 ms |    NA |    1512 B |
| Send_MediatR_5Behaviors   |  5.841 ms |    NA |    5320 B |
| Send_Plaxion_1Behavior    |  6.369 ms |    NA |    2232 B |
| Send_MediatR_10Behaviors  |  7.649 ms |    NA |    6752 B |
| Send_Plaxion_5Behaviors   |  8.006 ms |    NA |    3776 B |
| Send_Plaxion_10Behaviors  | 10.364 ms |    NA |    4104 B |
| Send_Mediator_0Behaviors  | 11.892 ms |    NA |     784 B |
| Send_MediatR_20Behaviors  | 12.176 ms |    NA |    9584 B |
| Send_Mediator_1Behavior   | 12.547 ms |    NA |     912 B |
| Send_Plaxion_20Behaviors  | 15.043 ms |    NA |    6792 B |
| Send_Mediator_5Behaviors  | 15.685 ms |    NA |    1424 B |
| Send_Mediator_10Behaviors | 18.748 ms |    NA |    2064 B |
| Send_Mediator_20Behaviors | 25.021 ms |    NA |    3632 B |
