``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.19041
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.101
  [Host]     : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT
  DefaultJob : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT


```
|                                                Method |     Mean |   Error |  StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------------------ |---------:|--------:|--------:|------:|------:|------:|----------:|
|           DirectChannel_Send_10_000_000_SingleHandler | 229.1 ms | 2.47 ms | 2.31 ms |     - |     - |     - |   5.97 KB |
|      DirectChannel_SendAsync_10_000_000_SingleHandler | 233.5 ms | 1.50 ms | 1.33 ms |     - |     - |     - |   4.73 KB |
|             DirectChannel_Send_10_000_000_TwoHandlers | 472.9 ms | 5.28 ms | 4.41 ms |     - |     - |     - |    6.6 KB |
|            DirectChannel_Send_10_000_000_FourHandlers | 469.4 ms | 2.77 ms | 2.45 ms |     - |     - |     - |   5.29 KB |
| PublishSubscribeChannel_Send_10_000_000_SingleHandler | 387.0 ms | 2.89 ms | 2.56 ms |     - |     - |     - |   6.13 KB |
|   PublishSubscribeChannel_Send_10_000_000_TwoHandlers | 468.5 ms | 3.60 ms | 3.37 ms |     - |     - |     - |   6.32 KB |
