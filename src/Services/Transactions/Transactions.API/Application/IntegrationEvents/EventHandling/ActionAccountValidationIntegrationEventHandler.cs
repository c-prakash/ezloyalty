﻿using eRewards.Services.Transactions.API.Application.Commands;
using eRewards.Services.Transactions.API.Application.IntegrationEvents.Events;
using MediatR;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Extensions;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eRewards.Services.Transactions.API.Application.IntegrationEvents.EventHandling
{
    /// <summary>
    /// 
    /// </summary>
    public class ActionAccountValidationIntegrationEventHandler : IIntegrationEventHandler<ActionAccountValidationIntegrationEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ActionAccountValidationIntegrationEventHandler> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mediator"></param>
        /// <param name="logger"></param>
        public ActionAccountValidationIntegrationEventHandler(
           IMediator mediator,
           ILogger<ActionAccountValidationIntegrationEventHandler> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public async Task Handle(ActionAccountValidationIntegrationEvent @event)
        {
            using (LogContext.PushProperty("IntegrationEventContext", $"{@event.Id}-{Program.AppName}"))
            {
                _logger.LogInformation("----- Handling integration event: {IntegrationEventId} at {AppName} - ({@IntegrationEvent})", @event.Id, Program.AppName, @event);

                var command = new SetAccountValidationActionStatusCommand(@event.AccountId, @event.ActionId, @event.Status == AccountValidationStatus.Found);

                _logger.LogInformation(
                    "----- Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
                    command.GetGenericTypeName(),
                    nameof(command.ActionId),
                    command.ActionId,
                    command);

                await _mediator.Send(command);
            }
        }
    }

    
}