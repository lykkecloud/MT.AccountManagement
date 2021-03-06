// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.NegativeProtection.Commands;
using MarginTrading.Backend.Contracts.Workflow.Liquidation.Events;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Workflow.NegativeProtection
{
    internal class NegativeProtectionSaga
    {
        private readonly CqrsContextNamesSettings _contextNames;
        private readonly AccountManagementSettings _settings;
        private readonly INegativeProtectionService _negativeProtectionService;
        private readonly IAccountsRepository _accountsRepository;
        private readonly ISystemClock _systemClock;

        public NegativeProtectionSaga(
            CqrsContextNamesSettings contextNames,
            AccountManagementSettings settings,
            INegativeProtectionService negativeProtectionService,
            IAccountsRepository accountsRepository,
            ISystemClock systemClock)
        {
            _contextNames = contextNames;
            _settings = settings;
            _negativeProtectionService = negativeProtectionService;
            _accountsRepository = accountsRepository;
            _systemClock = systemClock;
        }

        [UsedImplicitly]
        private async Task Handle(LiquidationFinishedEvent evt, ICommandSender sender)
        {
            await HandleEvents(evt.OperationId, evt.AccountId,
                evt.OpenPositionsRemainingOnAccount, evt.CurrentTotalCapital, sender);
        }

        [UsedImplicitly]
        private async Task Handle(LiquidationFailedEvent evt, ICommandSender sender)
        {
            await HandleEvents(evt.OperationId, evt.AccountId,
                evt.OpenPositionsRemainingOnAccount, evt.CurrentTotalCapital, sender);
        }
        
        private async Task HandleEvents(string operationId, string accountId, 
            int openPositionsOnAccount, decimal currentTotalCapital, ICommandSender sender)
        {
            await Task.Delay(_settings.NegativeProtectionTimeoutMs);
                
            var account = await _accountsRepository.GetAsync(accountId);
            var amount = await _negativeProtectionService.CheckAsync(operationId, account);

            if (account == null || amount == null)
            {
                return;
            }

            sender.SendCommand(new NotifyNegativeProtectionInternalCommand(
                    Guid.NewGuid().ToString("N"),
                    operationId,
                    operationId,
                    _systemClock.UtcNow.UtcDateTime,
                    account.ClientId,
                    account.Id,
                    amount.Value,
                    openPositionsOnAccount,
                    currentTotalCapital
                ),
                _contextNames.AccountsManagement);  
        }
    }
}