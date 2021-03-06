﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.AzureStorage.Tables;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.AzureStorage
{
    public class OperationExecutionInfoEntity : AzureTableEntity, IOperationExecutionInfo<object>
    {
        public string OperationName
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }
        
        public string Id
        {
            get => RowKey;
            set => RowKey = value;
        }

        object IOperationExecutionInfo<object>.Data => JsonConvert.DeserializeObject<object>(Data);
        public string Data { get; set; }
        public DateTime LastModified { get; set; }

        public static string GeneratePartitionKey(string operationName)
        {
            return operationName;
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }
    }
}