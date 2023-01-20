## Description
This library provides a set of classes and functionality for working with collections and enumerables. A featured capability is working with "Frozen" collections that do not allow mutation of the collection itself.

## Benchmarks
``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1105)
12th Gen Intel Core i7-12700H, 1 CPU, 20 logical and 14 physical cores
.NET SDK=7.0.101
  [Host]   : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2  [AttachedDebugger]
  ShortRun : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
|                                                   Method |    Size |            Mean |            Error |         StdDev |   Gen0 | Allocated |
|--------------------------------------------------------- |-------- |----------------:|-----------------:|---------------:|-------:|----------:|
|                                     **ReadOnlySpan_Foreach** |     **100** |        **31.46 ns** |        **14.210 ns** |       **0.779 ns** |      **-** |         **-** |
|                                            Array_Foreach |     100 |        29.35 ns |         1.129 ns |       0.062 ns |      - |         - |
|                                             List_Foreach |     100 |        45.11 ns |         5.264 ns |       0.289 ns |      - |         - |
|                          ArrayFrozenAsCollection_Foreach |     100 |       463.30 ns |       145.652 ns |       7.984 ns | 0.0038 |      48 B |
|                 ArrayFrozenAsSpannableCollection_Foreach |     100 |       463.94 ns |        72.599 ns |       3.979 ns | 0.0038 |      48 B |
|                                ArrayFrozenAsList_Foreach |     100 |       466.45 ns |       104.058 ns |       5.704 ns | 0.0038 |      48 B |
|                       ArrayFrozenAsSpannableList_Foreach |     100 |       476.35 ns |       348.744 ns |      19.116 ns | 0.0038 |      48 B |
|                             ArrayFrozenAsHashSet_Foreach |     100 |       613.20 ns |        53.400 ns |       2.927 ns | 0.0029 |      40 B |
|                           ListFrozenAsCollection_Foreach |     100 |       461.15 ns |        60.991 ns |       3.343 ns | 0.0038 |      48 B |
|                  ListFrozenAsSpannableCollection_Foreach |     100 |       456.89 ns |        61.193 ns |       3.354 ns | 0.0038 |      48 B |
|                                 ListFrozenAsList_Foreach |     100 |       456.91 ns |        59.426 ns |       3.257 ns | 0.0038 |      48 B |
|                        ListFrozenAsSpannableList_Foreach |     100 |       465.82 ns |       118.061 ns |       6.471 ns | 0.0038 |      48 B |
|                              ListFrozenAsHashSet_Foreach |     100 |       606.68 ns |       119.121 ns |       6.529 ns | 0.0029 |      40 B |
| ArrayFrozenAsSpannableCollection_ForeachOverReadOnlySpan |     100 |        31.87 ns |         5.723 ns |       0.314 ns |      - |         - |
|       ArrayFrozenAsSpannableList_ForeachOverReadOnlySpan |     100 |        32.24 ns |         4.653 ns |       0.255 ns |      - |         - |
|  ListFrozenAsSpannableCollection_ForeachOverReadOnlySpan |     100 |        31.97 ns |         2.398 ns |       0.131 ns |      - |         - |
|        ListFrozenAsSpannableList_ForeachOverReadOnlySpan |     100 |        31.80 ns |         2.826 ns |       0.155 ns |      - |         - |
|                                     **ReadOnlySpan_Foreach** |    **1000** |       **232.55 ns** |        **26.290 ns** |       **1.441 ns** |      **-** |         **-** |
|                                            Array_Foreach |    1000 |       230.89 ns |        29.373 ns |       1.610 ns |      - |         - |
|                                             List_Foreach |    1000 |       356.60 ns |       158.793 ns |       8.704 ns |      - |         - |
|                          ArrayFrozenAsCollection_Foreach |    1000 |     4,759.59 ns |       695.561 ns |      38.126 ns |      - |      48 B |
|                 ArrayFrozenAsSpannableCollection_Foreach |    1000 |     4,774.49 ns |       115.760 ns |       6.345 ns |      - |      48 B |
|                                ArrayFrozenAsList_Foreach |    1000 |     4,842.57 ns |     2,450.183 ns |     134.303 ns |      - |      48 B |
|                       ArrayFrozenAsSpannableList_Foreach |    1000 |     4,731.12 ns |       570.072 ns |      31.248 ns |      - |      48 B |
|                             ArrayFrozenAsHashSet_Foreach |    1000 |     5,876.02 ns |       954.459 ns |      52.317 ns |      - |      40 B |
|                           ListFrozenAsCollection_Foreach |    1000 |     4,749.21 ns |       365.020 ns |      20.008 ns |      - |      48 B |
|                  ListFrozenAsSpannableCollection_Foreach |    1000 |     4,821.92 ns |       162.775 ns |       8.922 ns |      - |      48 B |
|                                 ListFrozenAsList_Foreach |    1000 |     4,709.20 ns |       701.666 ns |      38.461 ns |      - |      48 B |
|                        ListFrozenAsSpannableList_Foreach |    1000 |     4,748.78 ns |     1,025.154 ns |      56.192 ns |      - |      48 B |
|                              ListFrozenAsHashSet_Foreach |    1000 |     6,156.79 ns |     4,202.042 ns |     230.328 ns |      - |      40 B |
| ArrayFrozenAsSpannableCollection_ForeachOverReadOnlySpan |    1000 |       234.60 ns |         8.786 ns |       0.482 ns |      - |         - |
|       ArrayFrozenAsSpannableList_ForeachOverReadOnlySpan |    1000 |       238.40 ns |       131.383 ns |       7.202 ns |      - |         - |
|  ListFrozenAsSpannableCollection_ForeachOverReadOnlySpan |    1000 |       233.63 ns |         9.954 ns |       0.546 ns |      - |         - |
|        ListFrozenAsSpannableList_ForeachOverReadOnlySpan |    1000 |       238.22 ns |        96.315 ns |       5.279 ns |      - |         - |
|                                     **ReadOnlySpan_Foreach** |   **10000** |     **2,262.07 ns** |       **182.779 ns** |      **10.019 ns** |      **-** |         **-** |
|                                            Array_Foreach |   10000 |     2,268.93 ns |       384.150 ns |      21.057 ns |      - |         - |
|                                             List_Foreach |   10000 |     3,408.17 ns |       490.031 ns |      26.860 ns |      - |         - |
|                          ArrayFrozenAsCollection_Foreach |   10000 |    45,367.74 ns |     7,982.021 ns |     437.522 ns |      - |      49 B |
|                 ArrayFrozenAsSpannableCollection_Foreach |   10000 |    44,809.85 ns |     5,638.012 ns |     309.038 ns |      - |      48 B |
|                                ArrayFrozenAsList_Foreach |   10000 |    45,518.18 ns |    18,841.159 ns |   1,032.747 ns |      - |      48 B |
|                       ArrayFrozenAsSpannableList_Foreach |   10000 |    45,325.48 ns |    11,847.941 ns |     649.426 ns |      - |      48 B |
|                             ArrayFrozenAsHashSet_Foreach |   10000 |    57,863.81 ns |     3,264.237 ns |     178.924 ns |      - |      40 B |
|                           ListFrozenAsCollection_Foreach |   10000 |    44,644.80 ns |     4,811.292 ns |     263.723 ns |      - |      48 B |
|                  ListFrozenAsSpannableCollection_Foreach |   10000 |    44,813.54 ns |     6,579.982 ns |     360.671 ns |      - |      49 B |
|                                 ListFrozenAsList_Foreach |   10000 |    44,705.70 ns |     3,350.868 ns |     183.672 ns |      - |      48 B |
|                        ListFrozenAsSpannableList_Foreach |   10000 |    44,922.93 ns |     2,902.090 ns |     159.073 ns |      - |      48 B |
|                              ListFrozenAsHashSet_Foreach |   10000 |    58,642.55 ns |     5,098.248 ns |     279.452 ns |      - |      40 B |
| ArrayFrozenAsSpannableCollection_ForeachOverReadOnlySpan |   10000 |     2,340.55 ns |     1,152.984 ns |      63.199 ns |      - |         - |
|       ArrayFrozenAsSpannableList_ForeachOverReadOnlySpan |   10000 |     2,281.76 ns |       357.069 ns |      19.572 ns |      - |         - |
|  ListFrozenAsSpannableCollection_ForeachOverReadOnlySpan |   10000 |     2,285.40 ns |       307.553 ns |      16.858 ns |      - |         - |
|        ListFrozenAsSpannableList_ForeachOverReadOnlySpan |   10000 |     2,283.98 ns |       371.033 ns |      20.338 ns |      - |         - |
|                                     **ReadOnlySpan_Foreach** |  **100000** |    **22,971.98 ns** |     **2,972.103 ns** |     **162.911 ns** |      **-** |         **-** |
|                                            Array_Foreach |  100000 |    22,766.80 ns |     2,373.370 ns |     130.092 ns |      - |         - |
|                                             List_Foreach |  100000 |    34,246.70 ns |     3,330.700 ns |     182.567 ns |      - |         - |
|                          ArrayFrozenAsCollection_Foreach |  100000 |   446,288.83 ns |    67,341.169 ns |   3,691.197 ns |      - |      48 B |
|                 ArrayFrozenAsSpannableCollection_Foreach |  100000 |   438,721.68 ns |    25,573.207 ns |   1,401.754 ns |      - |      48 B |
|                                ArrayFrozenAsList_Foreach |  100000 |   447,893.54 ns |   193,181.752 ns |  10,588.944 ns |      - |      48 B |
|                       ArrayFrozenAsSpannableList_Foreach |  100000 |   430,979.48 ns |    36,073.075 ns |   1,977.287 ns |      - |      48 B |
|                             ArrayFrozenAsHashSet_Foreach |  100000 |   588,005.58 ns |   126,371.022 ns |   6,926.822 ns |      - |      40 B |
|                           ListFrozenAsCollection_Foreach |  100000 |   426,551.77 ns |     9,833.100 ns |     538.985 ns |      - |      48 B |
|                  ListFrozenAsSpannableCollection_Foreach |  100000 |   422,953.55 ns |    41,210.996 ns |   2,258.914 ns |      - |      48 B |
|                                 ListFrozenAsList_Foreach |  100000 |   426,376.09 ns |    34,900.836 ns |   1,913.033 ns |      - |      48 B |
|                        ListFrozenAsSpannableList_Foreach |  100000 |   430,787.21 ns |    74,413.538 ns |   4,078.857 ns |      - |      48 B |
|                              ListFrozenAsHashSet_Foreach |  100000 |   590,044.04 ns |   104,844.083 ns |   5,746.858 ns |      - |      40 B |
| ArrayFrozenAsSpannableCollection_ForeachOverReadOnlySpan |  100000 |    23,147.72 ns |    11,190.842 ns |     613.408 ns |      - |         - |
|       ArrayFrozenAsSpannableList_ForeachOverReadOnlySpan |  100000 |    22,978.07 ns |     4,329.308 ns |     237.304 ns |      - |         - |
|  ListFrozenAsSpannableCollection_ForeachOverReadOnlySpan |  100000 |    22,844.34 ns |     4,903.786 ns |     268.793 ns |      - |         - |
|        ListFrozenAsSpannableList_ForeachOverReadOnlySpan |  100000 |    22,849.71 ns |     6,260.658 ns |     343.168 ns |      - |         - |
|                                     **ReadOnlySpan_Foreach** | **1000000** |   **228,238.20 ns** |    **32,349.077 ns** |   **1,773.162 ns** |      **-** |         **-** |
|                                            Array_Foreach | 1000000 |   226,284.88 ns |    18,073.009 ns |     990.643 ns |      - |         - |
|                                             List_Foreach | 1000000 |   350,164.26 ns |    46,197.449 ns |   2,532.238 ns |      - |         - |
|                          ArrayFrozenAsCollection_Foreach | 1000000 | 4,353,089.32 ns |   369,433.232 ns |  20,249.882 ns |      - |      51 B |
|                 ArrayFrozenAsSpannableCollection_Foreach | 1000000 | 4,399,369.53 ns |   579,540.979 ns |  31,766.597 ns |      - |      51 B |
|                                ArrayFrozenAsList_Foreach | 1000000 | 4,401,088.28 ns | 1,546,454.007 ns |  84,766.362 ns |      - |      49 B |
|                       ArrayFrozenAsSpannableList_Foreach | 1000000 | 4,290,886.46 ns |   884,762.731 ns |  48,496.831 ns |      - |      51 B |
|                             ArrayFrozenAsHashSet_Foreach | 1000000 | 5,895,946.09 ns |   813,695.624 ns |  44,601.403 ns |      - |      43 B |
|                           ListFrozenAsCollection_Foreach | 1000000 | 4,366,629.43 ns | 2,441,247.960 ns | 133,813.038 ns |      - |      51 B |
|                  ListFrozenAsSpannableCollection_Foreach | 1000000 | 4,284,680.21 ns |   819,927.723 ns |  44,943.005 ns |      - |      51 B |
|                                 ListFrozenAsList_Foreach | 1000000 | 4,349,446.48 ns |   798,886.624 ns |  43,789.672 ns |      - |      51 B |
|                        ListFrozenAsSpannableList_Foreach | 1000000 | 4,273,119.66 ns |   607,751.316 ns |  33,312.900 ns |      - |      51 B |
|                              ListFrozenAsHashSet_Foreach | 1000000 | 6,009,651.56 ns | 1,784,928.428 ns |  97,837.950 ns |      - |      43 B |
| ArrayFrozenAsSpannableCollection_ForeachOverReadOnlySpan | 1000000 |   226,916.72 ns |    26,681.977 ns |   1,462.529 ns |      - |         - |
|       ArrayFrozenAsSpannableList_ForeachOverReadOnlySpan | 1000000 |   231,471.66 ns |   101,213.127 ns |   5,547.833 ns |      - |         - |
|  ListFrozenAsSpannableCollection_ForeachOverReadOnlySpan | 1000000 |   227,388.02 ns |    26,556.435 ns |   1,455.648 ns |      - |         - |
|        ListFrozenAsSpannableList_ForeachOverReadOnlySpan | 1000000 |   235,453.17 ns |    91,472.139 ns |   5,013.897 ns |      - |         - |
