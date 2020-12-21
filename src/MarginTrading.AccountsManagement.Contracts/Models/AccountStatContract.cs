// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.AccountsManagement.Contracts.Models
{
    /// <summary>
    /// Account statistics for the current trading day
    /// </summary>
    [MessagePackObject]
    public class AccountStatContract
    {
        /// <summary>
        /// Account id
        /// </summary>
        [NotNull]
        [Key(0)]
        public string AccountId { get; }
        
        /// <summary>
        /// Creation timestamp
        /// </summary>
        [Key(1)]
        public DateTime Created { get; }
        
        /// <summary>
        /// Realised pnl for the day
        /// </summary>
        [Key(2)]
        public decimal RealisedPnl { get; }
        
        /// <summary>
        /// Deposit amount for the day
        /// </summary>
        [Key(3)]
        public decimal DepositAmount { get; }
        
        /// <summary>
        /// Withdrawal amount for the day
        /// </summary>
        [Key(4)]
        public decimal WithdrawalAmount { get; }
        
        /// <summary>
        /// Commission amount for the day
        /// </summary>
        [Key(5)]
        public decimal CommissionAmount { get; }
        
        /// <summary>
        /// Other account balance changed for the day
        /// </summary>
        [Key(6)]
        public decimal OtherAmount { get; }
        
        /// <summary>
        /// Current account balance
        /// </summary>
        [Key(7)]
        public decimal AccountBalance { get; }
        
        /// <summary>
        /// Account balance at the moment of previous EOD
        /// </summary>
        [Key(8)]
        public decimal PrevEodAccountBalance { get; }
        
        /// <summary>
        /// The available balance for account
        /// </summary>
        [Key(9)]
        public decimal DisposableCapital { get; }
        
        /// <summary>
        /// UnRealised pnl for the day
        /// </summary>
        [Key(10)]
        public decimal UnRealisedPnl { get; }

        /// <summary>
        /// Account name
        /// </summary>
        [Key(11)]
        public string AccountName { get; }
        
        /// <summary>
        /// Detailed account capital info
        /// </summary>
        [Key(12)]
        public AccountCapitalContract AccountCapitalDetails { get; }

        public AccountStatContract([NotNull] string accountId, DateTime created, decimal realisedPnl,
            decimal depositAmount, decimal withdrawalAmount, decimal commissionAmount, decimal otherAmount,
            decimal accountBalance, decimal prevEodAccountBalance, decimal disposableCapital,
            decimal unRealisedPnl, string accountName, AccountCapitalContract accountCapitalDetails)
        {
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            Created = created;
            RealisedPnl = realisedPnl;
            DepositAmount = depositAmount;
            WithdrawalAmount = withdrawalAmount;
            CommissionAmount = commissionAmount;
            OtherAmount = otherAmount;
            AccountBalance = accountBalance;
            PrevEodAccountBalance = prevEodAccountBalance;
            DisposableCapital = disposableCapital;
            UnRealisedPnl = unRealisedPnl;
            AccountName = accountName;
            AccountCapitalDetails = accountCapitalDetails;
        }
    }
}