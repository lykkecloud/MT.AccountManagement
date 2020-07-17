﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.AccountsManagement.Settings
{
    [UsedImplicitly]
    public class RabbitMqSettings
    {
        public SubscriberSettings MarketStateChangedEventSubscriber { get; set; }
        public SubscriberSettings TaxFileUploadedEventSubscriber { get; set; }
    }
}