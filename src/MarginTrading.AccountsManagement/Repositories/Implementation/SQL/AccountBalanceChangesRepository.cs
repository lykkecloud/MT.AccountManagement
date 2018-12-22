﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using Lykke.Logs.MsSql.Extensions;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class AccountBalanceChangesRepository : IAccountBalanceChangesRepository
    {
        private const string TableName = "AccountHistory";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[Oid] [bigint] NOT NULL IDENTITY (1,1) PRIMARY KEY, " +
                                                 "[Id] [nvarchar] (64) NOT NULL UNIQUE, " +
                                                 "[AccountId] [nvarchar] (64) NOT NULL, " +
                                                 "[ChangeTimestamp] [datetime] NOT NULL, " +
                                                 "[ClientId] [nvarchar] (64) NOT NULL, " +
                                                 "[ChangeAmount] decimal (24, 12) NOT NULL, " +
                                                 "[Balance] decimal (24, 12) NOT NULL, " +
                                                 "[WithdrawTransferLimit] decimal (24, 12) NOT NULL, " +
                                                 "[Comment] [nvarchar] (MAX) NULL, " +
                                                 "[ReasonType] [nvarchar] (64) NULL, " +
                                                 "[EventSourceId] [nvarchar] (64) NULL, " +
                                                 "[LegalEntity] [nvarchar] (64) NULL, " +
                                                 "[AuditLog] [nvarchar] (MAX) NULL, " +
                                                 "[Instrument] [nvarchar] (64) NULL, " +
                                                 "[TradingDate] [datetime] NULL, " +
                                                 "INDEX IX_{0}_Base (Id, AccountId, ChangeTimestamp, EventSourceId, ReasonType)" +
                                                 ");";
        
        private static Type DataType => typeof(IAccountBalanceChange);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));
        private static readonly string GetUpdateClause = string.Join(",",
            DataType.GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly IConvertService _convertService;
        private readonly AccountManagementSettings _settings;
        private readonly ILog _log;
        private readonly ISystemClock _systemClock;
        
        public AccountBalanceChangesRepository(IConvertService convertService, 
            AccountManagementSettings settings, 
            ILog log,
            ISystemClock systemClock)
        {
            _convertService = convertService;
            _settings = settings;
            _log = log;
            _systemClock = systemClock;
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(AccountBalanceChangesRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }
        
        public async Task<IReadOnlyList<IAccountBalanceChange>> GetAsync(string accountId, DateTime? @from = null,
            DateTime? to = null, AccountBalanceChangeReasonType? reasonType = null)
        {
            var whereClause = "WHERE 1=1 " + (!string.IsNullOrWhiteSpace(accountId) ? " AND AccountId=@accountId" : "")
                                       + (from != null ? " AND ChangeTimestamp > @from" : "")
                                       + (to != null ? " AND ChangeTimestamp < @to" : "")
                                       + (reasonType != null ? " AND ReasonType = @reasonType" : "");
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                var data = await conn.QueryAsync<AccountBalanceChangeEntity>(
                    $"SELECT * FROM {TableName} {whereClause}", new
                    {
                        accountId, 
                        from, 
                        to, 
                        reasonType = reasonType.ToString(),
                    });

                return data.ToList();
            }
        }

        public async Task<IReadOnlyList<IAccountBalanceChange>> GetAsync(string accountId, string eventSourceId)
        {
            var whereClause = "WHERE AccountId=@accountId "
                + (string.IsNullOrWhiteSpace(eventSourceId) ? "" : "AND EventSourceId=@eventSourceId");
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                var data = await conn.QueryAsync<AccountBalanceChangeEntity>(
                    $"SELECT * FROM {TableName} {whereClause}", 
                    new { accountId, eventSourceId });

                return data.ToList();
            }
        }

        public async Task<decimal> GetRealizedDailyPnl(string accountId)
        {
            var whereClause = "WHERE AccountId=@accountId"
                              + " AND ChangeTimestamp > @from"
                              + " AND ReasonType = @reasonType";
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                return await conn.QuerySingleAsync<int>(
                    $"SELECT SUM(ChangeAmount) FROM {TableName} {whereClause}", new
                    {
                        accountId,
                        //TODO rethink the way trading day's start & end are selected 
                        from = _systemClock.UtcNow.UtcDateTime.Date,
                        reasonType = AccountBalanceChangeReasonType.RealizedPnL.ToString(),
                    });
            }
        }

        public async Task AddAsync(IAccountBalanceChange change)
        {
            var entity = _convertService.Convert<AccountBalanceChangeEntity>(change);
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                try
                {
                    try
                    {
                        await conn.ExecuteAsync(
                            $"insert into {TableName} ({GetColumns}) values ({GetFields})", entity);
                    }
                    catch (SqlException)
                    {
                        await conn.ExecuteAsync(
                            $"update {TableName} set {GetUpdateClause} where Id=@Id", entity); 
                    }
                }
                catch (Exception ex)
                {
                    var msg = $"Error {ex.Message} \n" +
                              "Entity <AccountBalanceChangeEntity>: \n" +
                              entity.ToJson();
                    await _log.WriteWarningAsync(nameof(AccountBalanceChangesRepository), nameof(AddAsync), null, msg);
                    throw new Exception(msg);
                }
            }
        }
    }
}