﻿using MongoDB.Driver;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Contracts.Services
{
    public interface IMongoDBService
    {
        Task<RestResponse> InsertData<T>(string collectionName, T data) where T : class;

        Task<List<T>> GetData<T>(string collectionName) where T : class;

        Task<List<T>> GetDataFilter<T>(string collectionName, FilterDefinition<T> filter) where T : class;

        Task<DeleteDataResult> DeleteData<T>(string collectionName, FilterDefinition<T> filter) where T : class;

        Task UpdateData<T>(string collectionName, FilterDefinition<T> filter, UpdateDefinition<T> update);

    }

    public class DeleteDataResult
    {
        public bool Success { get; set; }
        public int DeleteCount { get; set; }
    }
}
