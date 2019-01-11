using System;

namespace MarginTrading.AccountsManagement.InternalModels
{
    public class OperationDataBase<TState>
        where TState : struct, IConvertible
    {
        public TState State { get; set; }
    }
}