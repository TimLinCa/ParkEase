using Amazon;
using Amazon.Runtime;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.Extensions.Configuration;
using ParkEase.Core.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Services
{
    public class AWSService : IAWSService
    {
        //https://stackoverflow.com/questions/69700124/reading-the-values-from-parameter-store-using-amazonsimplesystemsmanagementclien
        private readonly AmazonSimpleSystemsManagementClient client;
        public AWSService(IConfiguration configuration)
        {
            var credentials = new BasicAWSCredentials(configuration["AWSAccessKey"], configuration["AWSSecretKey"]);
            client = new AmazonSimpleSystemsManagementClient(credentials, RegionEndpoint.USEast2);
        }
        public async Task<string> GetParamenter(string name)
        {
            var request = new GetParameterRequest { Name = name };
            var value = await client.GetParameterAsync(request);
            return value.Parameter.Value;
        }
    }
}
