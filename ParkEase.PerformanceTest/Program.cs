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
            bool privatePage = true;
            bool SignUpPage = true;
            bool LogInPage = true;
            bool MapPage = true;

            if (analysispage) BenchmarkRunner.Run<AnalysisViewModelBenchmarks>();            
            if(privatePage) BenchmarkRunner.Run<PrivateSearchPageViewModelBenchmarks>();
            if (SignUpPage) BenchmarkRunner.Run<SignUpViewModelBenchmarks>();
            if (LogInPage) BenchmarkRunner.Run<LogInViewModelBenchmarks>();
            if (MapPage) BenchmarkRunner.Run<MapViewModelBenchmarks>();

        }
    }
}
