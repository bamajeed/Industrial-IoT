﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Core.Messaging.EventHub {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Default Event Hub message handler implementation
    /// </summary>
    public sealed class EventHubDeviceEventHandler : IEventHandler {
        /// <summary>
        /// Create processor factory
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="unknown"></param>
        public EventHubDeviceEventHandler(IEnumerable<IDeviceTelemetryHandler> handlers,
            IUnknownEventHandler unknown = null) {
            if (handlers == null) {
                throw new ArgumentNullException(nameof(handlers));
            }
            _handlers = handlers.ToDictionary(h => h.MessageSchema, h => h);
            _unknown = unknown;
        }

        /// <inheritdoc/>
        public async Task HandleAsync(byte[] eventData, IDictionary<string, string> properties,
            Func<Task> checkpoint) {

            // try to get event's properties 
            var handled = false;
            if (properties.TryGetValue(CommonProperties.EventSchemaType, out var schemaType)) {

                properties.TryGetValue(CommonProperties.DeviceId, out var deviceId);
                properties.TryGetValue(CommonProperties.ModuleId, out var moduleId);

                if (_handlers.TryGetValue(schemaType, out var handler)) {
                    _used.Add(handler);
                    await handler.HandleAsync(deviceId, moduleId, eventData, properties, checkpoint);
                    handled = true;
                }
            }

            if (!handled && _unknown != null) {
                // From a device, but does not have any event schema or message schema
                await _unknown.HandleAsync(eventData, properties);
            }
        }

        /// <inheritdoc/>
        public async Task OnBatchCompleteAsync() {
            try {
                foreach (var handler in _used.ToList()) {
                    await handler.OnBatchCompleteAsync();
                }
            }
            catch {
                throw;
            }
            finally { 
                _used.Clear();
            }
        }

        private readonly HashSet<IDeviceTelemetryHandler> _used = new HashSet<IDeviceTelemetryHandler>();
        private readonly Dictionary<string, IDeviceTelemetryHandler> _handlers;
        private readonly IUnknownEventHandler _unknown;
    }
}
