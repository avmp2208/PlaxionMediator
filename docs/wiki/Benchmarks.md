# Benchmarks

## Benchmark Summary

These benchmarks measure the performance characteristics of **PlaxionMediator** (dispatch, notifications, streaming, and pipeline overhead). All results are generated using [BenchmarkDotNet](https://benchmarkdotnet.org/) to ensure accuracy and reproducibility. This page will be updated with every framework release to track performance evolution over time.

PlaxionMediator is designed for high performance and zero reflection. Below are the benchmark results for various dispatch paths.

## Environment

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
```

## Send Benchmarks

Measures the overhead of dispatching a single request through the pipeline.

| Method             | Mean      | Error    | StdDev   | Gen0   | Allocated |
|------------------- |----------:|---------:|---------:|-------:|----------:|
| Send_NoPipeline    |  53.60 ns | 0.567 ns | 0.531 ns | 0.0042 |      56 B |
| Send_OneBehavior   | 119.57 ns | 2.339 ns | 3.972 ns | 0.0274 |     360 B |
| Send_FiveBehaviors | 242.83 ns | 4.703 ns | 5.416 ns | 0.0615 |     808 B |

## Notification Benchmarks (Publish)

Measures the overhead of publishing a notification to multiple handlers.

| Method               | Mean     | Error    | StdDev   | Gen0   | Allocated |
|--------------------- |---------:|---------:|---------:|-------:|----------:|
| Publish_OneHandler   | 42.85 ns | 0.446 ns | 0.417 ns | 0.0024 |      32 B |
| Publish_FiveHandlers | 55.65 ns | 0.895 ns | 0.837 ns | 0.0049 |      64 B |
| Publish_TenHandlers  | 77.42 ns | 0.659 ns | 0.550 ns | 0.0079 |     104 B |

## Stream Benchmarks

Measures the overhead of streaming 1000 items through an async enumerable.

| Method           | Mean     | Error    | StdDev   | Allocated |
|----------------- |---------:|---------:|---------:|----------:|
| Stream_1000Items | 410.7 μs | 6.54 μs  | 5.80 μs  |     968 B |
