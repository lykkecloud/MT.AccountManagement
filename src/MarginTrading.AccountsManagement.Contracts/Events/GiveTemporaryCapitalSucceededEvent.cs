// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.AccountsManagement.Contracts.Events
{
    /// <summary>
    /// Give Temporary Capital operation succeeded
    /// </summary>
    [MessagePackObject]
    public class GiveTemporaryCapitalSucceededEvent : BaseEvent
    {
        public GiveTemporaryCapitalSucceededEvent([NotNull] string operationId, DateTime eventTimestamp, 
            string eventSourceId, string accountId, decimal amount, string reason, string comment, string additionalInfo)
            : base(operationId, eventTimestamp)
        {
            EventSourceId = eventSourceId;
            AccountId = accountId;
            Amount = amount;
            Reason = reason;
            Comment = comment;
            AdditionalInfo = additionalInfo;
        }
        
        [Key(2)]
        public string EventSourceId { get; }
        
        [Key(3)]
        public string AccountId { get; }
        
        [Key(4)]
        public decimal Amount { get; }
        
        [Key(5)]
        public string Reason { get; }
        
        [Key(6)]
        public string Comment { get; }
        
        [Key(7)]
        public string AdditionalInfo { get; }
    }
}