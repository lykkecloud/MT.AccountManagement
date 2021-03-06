// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Events;
using MessagePack;

namespace MarginTrading.AccountsManagement.Workflow.GiveTemporaryCapital.Commands
{
    [MessagePackObject]
    public class FinishGiveTemporaryCapitalInternalCommand : BaseEvent
    {
        public FinishGiveTemporaryCapitalInternalCommand([NotNull] string operationId, DateTime eventTimestamp,
            bool isSuccess, string failReason)
            : base(operationId, eventTimestamp)
        {
            IsSuccess = isSuccess;
            FailReason = failReason;
        }
        
        [Key(2)]
        public bool IsSuccess { get; set; }
        
        [Key(3)]
        public string FailReason { get; set; }
    }
}