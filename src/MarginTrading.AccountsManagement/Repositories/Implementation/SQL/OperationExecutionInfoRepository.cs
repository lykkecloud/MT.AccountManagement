﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using Lykke.Logs.MsSql.Extensions;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    internal class OperationExecutionInfoRepository : IOperationExecutionInfoRepository
    {
        private const string TableName = "OperationExecutionInfo";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[Oid] [bigint] NOT NULL IDENTITY(1,1) PRIMARY KEY, " +
                                                 "[Id] [nvarchar] (128) NOT NULL, " +
                                                 "[LastModified] [datetime] NOT NULL, " +
                                                 "[OperationName] [nvarchar] (64) NULL, " +
                                                 "[Version] [nvarchar] (64) NULL, " +
                                                 "[Data] [nvarchar] (MAX) NOT NULL, " +
                                                 "CONSTRAINT [UX_OpExInfo_Id_Op] UNIQUE NONCLUSTERED ([Id], [OperationName])" +
                                                 ");";
        
        private static Type DataType => typeof(IOperationExecutionInfo<object>);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));
        private static readonly string GetUpdateClause = string.Join(",", 
            DataType.GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly AccountManagementSettings _settings;
        private readonly ILog _log;
        private readonly ISystemClock _systemClock;

        public OperationExecutionInfoRepository(AccountManagementSettings settings, 
            ILog log, ISystemClock systemClock)
        {
            _log = log;
            _settings = settings;
            _systemClock = systemClock;
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(OperationExecutionInfoRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }
        
        public async Task<IOperationExecutionInfo<TData>> GetOrAddAsync<TData>(
            string operationName, string operationId, Func<IOperationExecutionInfo<TData>> factory) where TData : class
        {
            try
            {
                using (var conn = new SqlConnection(_settings.Db.ConnectionString))
                {
                    var operationInfo = await conn.QueryFirstOrDefaultAsync<OperationExecutionInfoEntity>(
                        $"SELECT * FROM {TableName} WHERE Id=@operationId and OperationName=@operationName",
                        new {operationId, operationName});

                    if (operationInfo == null)
                    {
                        var entity = Convert(factory(), _systemClock.UtcNow.UtcDateTime);

                        await conn.ExecuteAsync(
                            $"insert into {TableName} ({GetColumns}) values ({GetFields})", entity);

                        return Convert<TData>(entity);
                    }

                    return Convert<TData>(operationInfo);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OperationExecutionInfoRepository), nameof(GetOrAddAsync), ex);
                throw;
            }
        }

        public async Task<IOperationExecutionInfo<TData>> GetAsync<TData>(string operationName, string id) where TData : class
        {
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                var operationInfo = await conn.QuerySingleOrDefaultAsync<OperationExecutionInfoEntity>(
                    $"SELECT * FROM {TableName} WHERE Id = @id and OperationName=@operationName",
                    new {id, operationName});

                return operationInfo == null ? null : Convert<TData>(operationInfo);
            }
        }

        public async Task SaveAsync<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class
        {
            var entity = Convert(executionInfo, _systemClock.UtcNow.UtcDateTime);
            var affectedRows = 0;
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                try
                {
                    affectedRows = await conn.ExecuteAsync(
                        $"update {TableName} set {GetUpdateClause} where Id=@Id " +
                        "and OperationName=@OperationName " +
                        "and LastModified=@PrevLastModified",
                        entity);
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(OperationExecutionInfoRepository), nameof(GetOrAddAsync), ex);
                    throw;
                }
            }

            if (affectedRows == 0)
            {
                var existingExecutionInfo = await GetAsync<TData>(executionInfo.OperationName, executionInfo.Id);

                if (existingExecutionInfo == null)
                {
                    throw new InvalidOperationException(
                        $"Execution info {executionInfo.OperationName}:{executionInfo.Id} does not exist");
                }

                throw new InvalidOperationException(
                    $"Optimistic Concurrency Violation Encountered. " +
                    $"Existing info: [{existingExecutionInfo.ToJson()}] " +
                    $"New info: [{executionInfo.ToJson()}]");
            }
        }

        public async Task DeleteAsync<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class
        {
            var entity = Convert(executionInfo, _systemClock.UtcNow.UtcDateTime);
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                try
                {
                    await conn.ExecuteAsync(
                        $"DELETE {TableName} where Id=@Id " +
                        "and OperationName=@OperationName ",
                        entity);
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(OperationExecutionInfoRepository), nameof(GetOrAddAsync), ex);
                    throw;
                }
            }
        }

        private static OperationExecutionInfo<TData> Convert<TData>(OperationExecutionInfoEntity entity)
            where TData : class
        {
            return new OperationExecutionInfo<TData>(
                entity.OperationName,
                entity.Id,
                lastModified: entity.LastModified,
                data: entity.Data is string dataStr
                    ? JsonConvert.DeserializeObject<TData>(dataStr)
                    : ((JToken) entity.Data).ToObject<TData>());
        }

        private static OperationExecutionInfoEntity Convert<TData>(IOperationExecutionInfo<TData> model, DateTime now)
            where TData : class
        {
            return new OperationExecutionInfoEntity
            {
                Id = model.Id,
                OperationName = model.OperationName,
                Data = model.Data.ToJson(),
                PrevLastModified = model.LastModified,
                LastModified = now
            };
        }
    }
}