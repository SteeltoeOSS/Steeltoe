``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.19041
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.101
  [Host]     : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT
  DefaultJob : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT


```
|                                                 Method |     Mean |   Error |  StdDev |       Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------------------- |---------:|--------:|--------:|------------:|------:|------:|----------:|
|       TaskSchedulerSubscribableChannel_Send_10_000_000 | 489.2 ms | 7.76 ms | 7.26 ms | 229000.0000 |     - |     - | 686.65 MB |
| TaskSchedulerSubscribableChannel_WriteAsync_10_000_000 | 558.3 ms | 4.45 ms | 4.16 ms | 229000.0000 |     - |     - | 686.65 MB |
