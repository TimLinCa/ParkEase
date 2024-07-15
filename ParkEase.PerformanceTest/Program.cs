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
            bool privateSearchPage = true;
            bool SignUpPage = true;
            bool LogInPage = true;
            bool MapPage = true;
            bool PrivateMapPage = true;
            bool createMapPage = true;

            if (analysispage) BenchmarkRunner.Run<AnalysisViewModelBenchmarks>();
            if (privateSearchPage) BenchmarkRunner.Run<PrivateSearchPageViewModelBenchmarks>();
            if (SignUpPage) BenchmarkRunner.Run<SignUpViewModelBenchmarks>();
            if (LogInPage) BenchmarkRunner.Run<LogInViewModelBenchmarks>();
            if (MapPage) BenchmarkRunner.Run<MapViewModelBenchmarks>();
            if (PrivateMapPage) BenchmarkRunner.Run<PrivateMapViewModelBenchmarks>();
            if (createMapPage) BenchmarkRunner.Run<CreateMapViewModelBenchmarks>();
        }
    }
}
