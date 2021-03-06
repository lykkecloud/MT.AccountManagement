﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using AzureStorage;
using Common.Log;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AccountsManagement.Repositories.AzureServices
{
    public interface IAzureTableStorageFactoryService
    {
        INoSQLTableStorage<TEntity> Create<TEntity>(IReloadingManager<string> connectionStringManager,
            string tableName, ILog log) where TEntity : class, ITableEntity, new();
    }
}