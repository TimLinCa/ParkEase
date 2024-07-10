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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ParkEase.Core.Services
{
    public class MongoDBService : IMongoDBService
    {
        //https://www.mongodb.com/developer/languages/csharp/create-restful-api-dotnet-core-mongodb/

        private string apiBase = string.Empty;
        private string dataSourceName = string.Empty;
        private string databaseName = string.Empty;
        private string apiKey = string.Empty;
        private IAWSService awsService;
        private IMongoDatabase db;
        private bool isTesting;
        private DevicePlatform platform;
        public MongoDBService(IAWSService awsService, DevicePlatform devicePlatform, bool isTesting = false)
        {
            this.isTesting = isTesting;
            this.awsService = awsService;
            this.platform = devicePlatform;
        }

        public async Task<T> InsertData<T>(string collectionName, T data) where T : class
        {
            await CheckAPIKey();

            if (platform == DevicePlatform.WinUI)
            {
                var collection = db.GetCollection<T>(collectionName);
                await collection.InsertOneAsync(data);
                return data;
            }

            var client = new RestClient($"{apiBase}/insertOne");
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");
            request.AddHeader("api-key", apiKey);
            var body = @"{" +
            $@" ""collection"":""{collectionName}""," +
            $@" ""database"":""{databaseName}""," +
            $@" ""dataSource"":""{dataSourceName}""," +
            $@" ""document"": {Newtonsoft.Json.JsonConvert.SerializeObject(data)}" +
            @"}";
            request.AddStringBody(body, DataFormat.Json);
            await client.PostAsync(request);
            return data;
        }

        public async Task<List<T>> GetData<T>(string collectionName) where T : class
        {
            await CheckAPIKey();


            if (platform == DevicePlatform.WinUI)
            {
                var collection = db.GetCollection<T>(collectionName);
                var result = await collection.FindAsync(FilterDefinition<T>.Empty);
                return result.ToList();
            }

            var client = new RestClient($"{apiBase}/find");
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");
            request.AddHeader("api-key", apiKey);
            var body = @"{" +
            $@" ""collection"":""{collectionName}""," +
            $@" ""database"":""{databaseName}""," +
            $@" ""dataSource"":""{dataSourceName}""" +
            @"}";
            request.AddStringBody(body, DataFormat.Json);
            RestResponse response = await client.PostAsync(request);
            Console.WriteLine(response.Content);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<T>>>(response.Content);
            if (data == null) return new List<T> { };
            return data["documents"];

        }

        public async Task<List<T>> GetStatusData<T>(string collectionName) where T : class
        {
            await CheckAPIKey();


            /*if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                var collection = db.GetCollection<T>(collectionName);
                var result = await collection.FindAsync(FilterDefinition<T>.Empty);
                return result.ToList();
            }*/

            var client = new RestClient($"{apiBase}/find");
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");
            request.AddHeader("api-key", apiKey);
            var body = @"{" +
            $@" ""collection"":""{collectionName}""," +
            $@" ""database"":""{databaseName}""," +
            $@" ""dataSource"":""{dataSourceName}""" +
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

            if (platform == DevicePlatform.WinUI)
            {
                var collection = db.GetCollection<T>(collectionName);
                var result = await collection.FindAsync(filter);
                return result.ToList();
            }

            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            var renderedFilter = filter.Render(documentSerializer, serializerRegistry);
            var filterString = renderedFilter.ToString();
            var client = new RestClient($"{apiBase}/find");
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");
            request.AddHeader("api-key", apiKey);
            var body = @"{" +
            $@" ""collection"":""{collectionName}""," +
            $@" ""database"":""{databaseName}""," +
            $@" ""dataSource"":""{dataSourceName}""," +
            $@" ""filter"": {filterString}" +
            @"}";
            request.AddStringBody(body, DataFormat.Json);
            RestResponse response = await client.PostAsync(request);
            Console.WriteLine(response.Content);
            Dictionary<string, object> responseContent = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
            string docuemts = responseContent["documents"].ToString();
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(docuemts);
            if (data == null) return new List<T> { };
            return data;
        }

        public async Task<DeleteDataResult> DeleteData<T>(string collectionName, FilterDefinition<T> filter) where T : class
        {
            await CheckAPIKey();

            if (platform == DevicePlatform.WinUI)
            {
                var collection = db.GetCollection<T>(collectionName);
                var result = await collection.DeleteOneAsync(filter);
                DeleteDataResult deleteResult_win = new DeleteDataResult();
                deleteResult_win.Success = true;
                deleteResult_win.DeleteCount = (int)result.DeletedCount;
                return deleteResult_win;
            }

            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            var renderedFilter = filter.Render(documentSerializer, serializerRegistry);
            var filterString = renderedFilter.ToString();
            var client = new RestClient($"{apiBase}/deleteMany");
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");
            request.AddHeader("api-key", apiKey);
            var body = @"{" +
            $@" ""collection"":""{collectionName}""," +
            $@" ""database"":""{databaseName}""," +
            $@" ""dataSource"":""{dataSourceName}""," +
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

            if(platform == DevicePlatform.WinUI)
            {
                var collection = db.GetCollection<T>(collectionName);
                await collection.UpdateOneAsync(filter, update);
            }
            else
            {
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
                request.AddHeader("api-key", apiKey);
                var body = @"{" +
                $@" ""collection"":""{collectionName}""," +
                $@" ""database"":""{databaseName}""," +
                $@" ""dataSource"":""{dataSourceName}""," +
                $@" ""filter"": {filterString}," +
                $@" ""update"": {updateString}" +
                @"}";
                request.AddStringBody(body, DataFormat.Json);
                RestResponse response = await client.PostAsync(request);

                UpdateDataResult updateResult = new UpdateDataResult();
                updateResult.Success = response.IsSuccessStatusCode;
                if (response.IsSuccessStatusCode)
                {
                    Dictionary<string, object> responseContent = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                    //updateResult.Message = responseContent["message"].ToString();
                    updateResult.MatchedCount = int.Parse(responseContent["matchedCount"].ToString());
                    updateResult.ModifiedCount = int.Parse(responseContent["modifiedCount"].ToString());
                    //System.Diagnostics.Debug.WriteLine($"MongoDB message: {updateResult.Message}");
                    System.Diagnostics.Debug.WriteLine($"MongoDB matchCount: {updateResult.MatchedCount}");
                    System.Diagnostics.Debug.WriteLine($"MongoDB ModifiedCount: {updateResult.ModifiedCount}");
                }
                else
                {
                    updateResult.Message = $"Update failed: {response.StatusDescription}";
                    System.Diagnostics.Debug.WriteLine($"MongoDB fail: {updateResult.Message}");
                }
            }
        }

        

        private async Task CheckAPIKey()
        {
            if (platform == DevicePlatform.WinUI && db == null)
            {
                string connectString = await awsService.GetParamenter("/ParkEase/Configs/ConnectionString");
                MongoClient mongoClient = new MongoClient(connectString);
                databaseName = isTesting ? await awsService.GetParamenter("/ParkEase/Configs/DatabaseTestName") : await awsService.GetParamenter("/ParkEase/Configs/DatabaseName");
                db = mongoClient.GetDatabase(databaseName);
            }
            else
            {
                if (apiKey == string.Empty)
                {
                    apiBase = await awsService.GetParamenter("/ParkEase/Configs/DatabaseAPI");
                    apiKey = await awsService.GetParamenter("/ParkEase/APIKeys/mongoDb");
                    databaseName = isTesting ? await awsService.GetParamenter("/ParkEase/Configs/DatabaseTestName") : await awsService.GetParamenter("/ParkEase/Configs/DatabaseName");
                    dataSourceName = await awsService.GetParamenter("/ParkEase/Configs/DatabaseSource");
                }
            }
        }

        public async Task DropCollection(string collectionName)
        {
            if (isTesting)
            {
                await db.DropCollectionAsync(collectionName);
            }
        }
    }

    public class CollectionName
    {
        public static string Users = "Users";
        public static string ParkingData = "ParkingData";
        public static string PrivateParking = "PrivateParking";
        public static string PrivateStatus = "PrivateStatus";
        public static string PublicStatus = "PublicStatus";
        public static string PublicLogs = "PublicLogs";
        public static string PrivateLogs = "PrivateLogs";
    }

    public class UpdateDataResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int MatchedCount { get; set; }
        public int ModifiedCount { get; set; }
    }

}
