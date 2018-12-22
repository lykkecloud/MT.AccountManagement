﻿using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Services
{
    public interface IAccountManagementService
    {
        
        #region Create 
        
        Task<IAccount> CreateAsync(string clientId, string accountId, string tradingConditionId, string baseAssetId);
        
        /// <summary>
        /// Creates default accounts for client by trading condition id.
        /// </summary>
        Task<IReadOnlyList<IAccount>> CreateDefaultAccountsAsync(string clientId, string tradingConditionId);
        
        /// <summary>
        /// Create accounts with requested base asset for all users 
        /// that already have accounts with requested trading condition
        /// </summary>
        Task<IReadOnlyList<IAccount>> CreateAccountsForNewBaseAssetAsync(string tradingConditionId, string baseAssetId);
        
        #endregion
        
        
        #region Get
        
        Task<IReadOnlyList<IAccount>> ListAsync(string search);

        Task<PaginatedResponse<IAccount>> ListByPagesAsync(string search, int? skip = null, int? take = null);
        
        Task<IReadOnlyList<IAccount>> GetByClientAsync(string clientId);
        
        [ItemCanBeNull]
        Task<IAccount> GetByIdAsync(string accountId);

        Task<AccountStat> GetStat(string accountId);

        Task ValidateAccountId(string accountId);
        
        #endregion
        
        
        #region Modify
        
        Task<IAccount> UpdateAccountAsync(string accountId,
            string tradingConditionId, bool? isDisabled, bool? isWithdrawalDisabled);
        
        Task ResetAccountAsync(string accountId);
        
        Task<string> StartGiveTemporaryCapital(string eventSourceId, string accountId, decimal amount, string reason,
            string comment, string additionalInfo);

        Task<string> StartRevokeTemporaryCapital(string eventSourceId, string accountId, string revokeEventSourceId,
            string comment,
            string additionalInfo);
        
        #endregion
    }
}