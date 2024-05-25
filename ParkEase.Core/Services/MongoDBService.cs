using MongoDB.Driver;
using ParkEase.Core.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Services
{
    public class MongoDBService : IMongoDBService
    {
        private string connectionString = "mongodb://127.0.0.1:27017";
        private string databaseName = "ParkEase";

        private readonly IMongoDatabase db;

        public MongoDBService()
        {
            var client = new MongoClient(connectionString);
            db = client.GetDatabase(databaseName);
        }

        public Task InsertManyData<T>(string collectionName, IEnumerable<T> data) where T : class
        {
            var collection = db.GetCollection<T>(collectionName);
            return collection.InsertManyAsync(data);
        }

        public Task InsertData<T>(string collectionName, T data) where T : class
        {
            var collection = db.GetCollection<T>(collectionName);
            return collection.InsertOneAsync(data);
        }

        public async Task<List<T>> GetData<T>(string collectionName) where T : class
        {
            var collection = getCollection<T>(collectionName);
            var result = await collection.FindAsync(FilterDefinition<T>.Empty);
            return result.ToList();
        }

        public async Task<DeleteResult> DeleteData<T>(string collectionName, FilterDefinition<T> filter) where T : class
        {
            var collection = getCollection<T>(collectionName);
            return await collection.DeleteOneAsync(filter);
        }


        private IMongoCollection<T> getCollection<T>(string collectionName)
        {
            return db.GetCollection<T>(collectionName);
        }
    }

    public class CollectionName
    {
        public static string Users = "Users";
        public static string ParkingData = "ParkingData";
        public static string PrivateParking = "PrivateParking";

    }
}
