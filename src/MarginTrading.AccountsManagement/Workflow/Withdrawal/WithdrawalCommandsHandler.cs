﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Workflow.Withdrawal.Commands;
using MarginTrading.AccountsManagement.Workflow.Withdrawal.Events;
using Microsoft.Extensions.Internal;
using MarginTrading.SettingsService.Contracts;

namespace MarginTrading.AccountsManagement.Workflow.Withdrawal
{
    internal class WithdrawalCommandsHandler
    {
        private readonly ISystemClock _systemClock;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IOperationExecutionInfoRepository _executionInfoRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly IScheduleSettingsApi _scheduleSettingsApi;
        private readonly IAccountManagementService _accountManagementService;

        private const string OperationName = "Withdraw";

        public WithdrawalCommandsHandler(
            ISystemClock systemClock,
            IAccountsRepository accountsRepository,
            IOperationExecutionInfoRepository executionInfoRepository,
            IChaosKitty chaosKitty,
            IScheduleSettingsApi scheduleSettingsApi, 
            IAccountManagementService accountManagementService)
        {
            _systemClock = systemClock;
            _executionInfoRepository = executionInfoRepository;
            _accountsRepository = accountsRepository;
            _chaosKitty = chaosKitty;
            _scheduleSettingsApi = scheduleSettingsApi;
            _accountManagementService = accountManagementService;
        }

        /// <summary>
        /// Handles the command to begin the withdrawal
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(WithdrawCommand command, IEventPublisher publisher)
        {
            await _executionInfoRepository.GetOrAddAsync(
                OperationName,
                command.OperationId,
                () => new OperationExecutionInfo<WithdrawalDepositData>(
                    OperationName,
                    command.OperationId,
                    new WithdrawalDepositData
                    {
                        AccountId = command.AccountId,
                        Amount = command.Amount,
                        AuditLog = command.AuditLog,
                        State = WithdrawalState.Created,
                        Comment = command.Comment
                    },
                    _systemClock.UtcNow.UtcDateTime));

            var account = await _accountsRepository.GetAsync(command.AccountId);

            var accountCapital = await _accountManagementService.GetAccountCapitalAsync(account);
            
            if (account == null || accountCapital.Disposable < command.Amount)
            {
                publisher.PublishEvent(new WithdrawalStartFailedInternalEvent(command.OperationId,
                    _systemClock.UtcNow.UtcDateTime, account == null
                        ? $"Account {command.AccountId} not found."
                        : $"Account {account.Id} balance {accountCapital.Balance}{accountCapital.AssetId} is not enough to withdraw {command.Amount}{accountCapital.AssetId}."
                          + (accountCapital.Compensations + accountCapital.Temporary + accountCapital.TotalPnl != 0
                              ? $" Taking into account the sum of the total daily PnL and compensation payments {accountCapital.TotalPnl + accountCapital.Compensations}{accountCapital.AssetId}, and temporary capital {accountCapital.Temporary}{accountCapital.AssetId}."
                              : "")));
                return;
            }

            var platformInfo = await _scheduleSettingsApi.GetPlatformInfo();

            if (!platformInfo.IsTradingEnabled)
            {
                publisher.PublishEvent(new WithdrawalStartFailedInternalEvent(command.OperationId,
                    _systemClock.UtcNow.UtcDateTime,
                    $"Platform is our of trading hours. Last trading day: {platformInfo.LastTradingDay}, next will start: {platformInfo.NextTradingDayStart}"));
            }

            if (account.IsWithdrawalDisabled)
            {
                publisher.PublishEvent(new WithdrawalStartFailedInternalEvent(command.OperationId,
                    _systemClock.UtcNow.UtcDateTime, "Withdrawal is disabled"));
                
                return;
            }
            
            _chaosKitty.Meow(command.OperationId);
          
            publisher.PublishEvent(new WithdrawalStartedInternalEvent(command.OperationId, 
                _systemClock.UtcNow.UtcDateTime));
        }

        /// <summary>
        /// Handles the command to fail the withdrawal
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(FailWithdrawalInternalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(
                OperationName,
                command.OperationId
            );

            if (executionInfo == null)
                return;
            
            var account = await _accountsRepository.GetAsync(executionInfo.Data.AccountId);

            publisher.PublishEvent(new WithdrawalFailedEvent(
                command.OperationId,
                _systemClock.UtcNow.UtcDateTime, 
                command.Reason,
                executionInfo.Data.AccountId,
                account?.ClientId,
                executionInfo.Data.Amount));
        }

        /// <summary>
        /// Handles the command to complete the withdrawal
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(CompleteWithdrawalInternalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(
                OperationName,
                command.OperationId
            );

            if (executionInfo == null)
                return;

            var account = await _accountsRepository.GetAsync(executionInfo.Data.AccountId);

            publisher.PublishEvent(new WithdrawalSucceededEvent(command.OperationId, _systemClock.UtcNow.UtcDateTime,
                account?.ClientId, executionInfo.Data.AccountId, executionInfo.Data.Amount));
        }
    }
}