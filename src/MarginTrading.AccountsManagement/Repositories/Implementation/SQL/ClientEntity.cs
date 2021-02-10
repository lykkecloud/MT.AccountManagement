﻿using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class ClientEntity : IClient
    {
        public string Id { get; set; }
        public string TradingConditionId { get; set; }

        public static ClientEntity From(IAccount account)
        {
            return new ClientEntity
            {
                Id = account.ClientId,
                TradingConditionId = account.TradingConditionId
            };
        }
    }
}
