using BenchmarkDotNet.Running;
using ParkEase.Messages;
using ParkEase.PerformanceTest.Benchmarks;
namespace ParkEase.PerformanceTest
{

    public class Program
    {
        public static void Main(string[] args)
        {
            bool analysispage = true;
            if (analysispage) BenchmarkRunner.Run<AnalysisViewModelBenchmarks>();
            bool privatePage = true;
            if(privatePage) BenchmarkRunner.Run<PrivateSearchPageViewModelBenchmarks>();
        }
    }



}
