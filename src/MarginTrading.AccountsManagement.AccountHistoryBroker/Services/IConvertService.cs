﻿// Copyright (c) 2019 Lykke Corp.

using System;
using AutoMapper;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker.Services
{
    public interface IConvertService
    {
        TResult Convert<TSource, TResult>(TSource source, Action<IMappingOperationOptions<TSource, TResult>> opts);
        TResult Convert<TSource, TResult>(TSource source);
        TResult Convert<TResult>(object source);
    }
}