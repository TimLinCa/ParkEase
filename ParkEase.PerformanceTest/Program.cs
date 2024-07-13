using BenchmarkDotNet.Running;
using ParkEase.PerformanceTest.Benchmarks;
namespace ParkEase.PerformanceTest
{

    public class Program
    {
        public static void Main(string[] args)
        {
            bool analysispage = true;
            if(analysispage) BenchmarkRunner.Run<AnalysisViewModelBenchmarks>();
        }
    }



}
