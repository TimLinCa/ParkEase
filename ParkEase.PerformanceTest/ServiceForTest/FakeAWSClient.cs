using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Services;
using ParkEase.ViewModel;
using ParkEase.Services;
using MongoDB.Driver;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Configuration;
using Moq;
using Microsoft.Extensions.Primitives;
using Syncfusion.Maui.Core.Carousel;
using ParkEase.Core.Data;
using System;
using Microsoft.Maui.Platform;

namespace ParkEase.PerformanceTest.ServiceForTest
{
    public class FakeAWSClient : IAWSService
    {

        private Dictionary<string, string> data;

        public FakeAWSClient()
        {
            data = new Dictionary<string, string>
            {
                {"/ParkEase/Configs/DatabaseTestName", "ParkEaseTest"}
            };
        }

        public Task<string> GetParamenter(string name)
        {
            string result = data.ContainsKey(name) ? data[name] : "";
            return Task.FromResult(result);
        }
    }
}
