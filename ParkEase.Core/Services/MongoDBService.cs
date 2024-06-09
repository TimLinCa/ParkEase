using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ParkEase.Core.Contracts.Services;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Services
{
    public class MongoDBService : IMongoDBService
    {
        private readonly string apiBase;
        private readonly string databaseName = "ParkEase";
        private string APIKey = string.Empty;
        private IAWSService awsService;
        public MongoDBService(IConfiguration configuration,IAWSService awsService)
        {
            apiBase = configuration["MongoDbRestAPI"];
            databaseName = configuration["DataBaseName"];
            this.awsService = awsService;
            //var client = new MongoClient(connectionUri);
            //try
            //{
            //    db = client.GetDatabase(databaseName);
            //    Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}
        }


        public async Task<RestResponse> InsertData<T>(string collectionName, T data) where T : class
        {
            await CheckAPIKey();
            var client = new RestClient($"{apiBase}/insertOne");
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");
            request.AddHeader("api-key", APIKey);
            var body = @"{" +
            $@" ""collection"":""{collectionName}""," +
            $@" ""database"":""{databaseName}""," +
            @" ""dataSource"":""ParkEase""," +
            $@" ""document"": {Newtonsoft.Json.JsonConvert.SerializeObject(data)}" +
            @"}";
            request.AddStringBody(body, DataFormat.Json);
            return await client.PostAsync(request);
        }

        public async Task<List<T>> GetData<T>(string collectionName) where T : class
        {
            await CheckAPIKey();
            var client = new RestClient($"{apiBase}/find");
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");
            request.AddHeader("api-key", APIKey);
            var body = @"{" +
            $@" ""collection"":""{collectionName}""," +
            $@" ""database"":""{databaseName}""," +
            @" ""dataSource"":""ParkEase""" +
            @"}";
            request.AddStringBody(body, DataFormat.Json);
            RestResponse response = await client.PostAsync(request);
            Console.WriteLine(response.Content);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<T>>>(response.Content);
            if (data == null) return new List<T> { };
            return data["documents"];

        }

        public async Task<List<T>> GetDataFilter<T>(string collectionName, FilterDefinition<T> filter) where T : class
        {
            await CheckAPIKey();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            var renderedFilter = filter.Render(documentSerializer, serializerRegistry);
            var filterString = renderedFilter.ToString();
            var client = new RestClient($"{apiBase}/find");
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");
            request.AddHeader("api-key", APIKey);
            var body = @"{" +
            $@" ""collection"":""{collectionName}""," +
            $@" ""database"":""{databaseName}""," +
            @" ""dataSource"":""ParkEase""," +
            $@" ""filter"": {filterString}" +
            @"}";
            request.AddStringBody(body, DataFormat.Json);
            RestResponse response = await client.PostAsync(request);
            Console.WriteLine(response.Content);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<T>>>(response.Content);
            if (data == null) return new List<T> { };
            return data["documents"];
        }

        public async Task<DeleteDataResult> DeleteData<T>(string collectionName, FilterDefinition<T> filter) where T : class
        {
            await CheckAPIKey();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            var renderedFilter = filter.Render(documentSerializer, serializerRegistry);
            var filterString = renderedFilter.ToString();
            var client = new RestClient($"{apiBase}/deleteMany");
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");
            request.AddHeader("api-key", APIKey);
            var body = @"{" +
            $@" ""collection"":""{collectionName}""," +
            $@" ""database"":""{databaseName}""," +
            @" ""dataSource"":""ParkEase""," +
            $@" ""filter"": {filterString}" +
            @"}";
            request.AddStringBody(body, DataFormat.Json);
            RestResponse response = await client.PostAsync(request);
            DeleteDataResult deleteResult = new DeleteDataResult();
            deleteResult.Success = response.IsSuccessStatusCode;

            if (response.IsSuccessStatusCode)
            {
                Dictionary<string, object> responseContent = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                deleteResult.DeleteCount = int.Parse(responseContent["deletedCount"].ToString());
            }
            else
            {
                deleteResult.DeleteCount = 0;
            }
            return deleteResult;
        }

        public async Task UpdateData<T>(string collectionName, FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            await CheckAPIKey();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            var renderedFilter = filter.Render(documentSerializer, serializerRegistry);
            var filterString = renderedFilter.ToString();

            var renderedUpdate = update.Render(documentSerializer, serializerRegistry);
            var updateString = renderedUpdate.ToString();

            var client = new RestClient($"{apiBase}/updateMany");
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");
            request.AddHeader("api-key", APIKey);
            var body = @"{" +
            $@" ""collection"":""{collectionName}""," +
            $@" ""database"":""{databaseName}""," +
            @" ""dataSource"":""ParkEase""," +
            $@" ""filter"": {filterString}," +
            $@" ""update"": {updateString}" +
            @"}";
            request.AddStringBody(body, DataFormat.Json);
            RestResponse response = await client.PostAsync(request);
        }

        private async Task CheckAPIKey()
        {
            if(APIKey == string.Empty)
            {
                APIKey = await awsService.GetParamenter("/ParkEase/APIKeys/mongoDb");
            }
        }
    }

    public class CollectionName
    {
        public static string Users = "Users";
        public static string ParkingData = "ParkingData";
        public static string PrivateParking = "PrivateParking";

    }

  
}
