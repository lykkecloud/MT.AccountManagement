﻿// Copyright (c) 2019 Lykke Corp.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AccountsManagement.Repositories.AzureServices.Implementations
{
    internal class InMemoryTableStorageFactory : IAzureTableStorageFactoryService
    {
        private readonly ConcurrentDictionary<Type, object> _tables = new ConcurrentDictionary<Type, object>();

        public INoSQLTableStorage<TEntity> Get<TEntity>() where TEntity : class, ITableEntity, new()
        {
            return (INoSQLTableStorage<TEntity>) _tables.GetValueOrDefault(typeof(TEntity));
        }

        public INoSQLTableStorage<TEntity> Create<TEntity>(IReloadingManager<string> connectionStringManager,
            string tableName, ILog log) where TEntity : class, ITableEntity, new()
        {
            return (INoSQLTableStorage<TEntity>) _tables.GetOrAdd(typeof(TEntity),
                t => new NoSqlTableInMemory<TEntity>());
        }
    }
}