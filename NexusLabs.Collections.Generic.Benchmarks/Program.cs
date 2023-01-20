using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

using System.Reflection;

var config = ManualConfig
    .Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);
var summary = BenchmarkRunner.Run(
    Assembly.GetExecutingAssembly(),
    config);
