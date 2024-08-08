using BenchmarkDotNet.Running;
using ParkEase.Messages;
using ParkEase.PerformanceTest.Benchmarks;
namespace ParkEase.PerformanceTest
{

    public class Program
    {
        public static void Main(string[] args)
        {
            bool analysisPage = false;
            bool privateSearchPage = false;
            bool SignUpPage = false;
            bool LogInPage = false;
            bool MapPage = false;
            bool PrivateMapPage = false;
            bool createMapPage = false;
            bool UserMapPage = true;

            if (analysisPage) BenchmarkRunner.Run<AnalysisViewModelBenchmarks>();
            if (privateSearchPage) BenchmarkRunner.Run<PrivateSearchPageViewModelBenchmarks>();
            if (SignUpPage) BenchmarkRunner.Run<SignUpViewModelBenchmarks>();
            if (LogInPage) BenchmarkRunner.Run<LogInViewModelBenchmarks>();
            if (MapPage) BenchmarkRunner.Run<MapViewModelBenchmarks>();
            if (PrivateMapPage) BenchmarkRunner.Run<PrivateMapViewModelBenchmarks>();
            if (createMapPage) BenchmarkRunner.Run<CreateMapViewModelBenchmarks>();
            if (UserMapPage) BenchmarkRunner.Run<UserMapViewModelBenchmarks>();
        }
    }
}
