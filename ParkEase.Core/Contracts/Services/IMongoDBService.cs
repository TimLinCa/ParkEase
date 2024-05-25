using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Contracts.Services
{
    public interface IMongoDBService
    {
        Task InsertManyData<T>(string collectionName, IEnumerable<T> data) where T : class;

        Task InsertData<T>(string collectionName, T data) where T : class;

        Task<List<T>> GetData<T>(string collectionName) where T : class;

        Task<DeleteResult> DeleteData<T>(string collectionName, FilterDefinition<T> filter) where T : class;

    }
}
