// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Text;

    /// <summary>
    /// Module host implementation
    /// </summary>
    public sealed class ModuleHost : IModuleHost, ITwinProperties, IEventEmitter,
        IBlobUpload, IJsonMethodClient, IClientAccessor {

        /// <inheritdoc/>
        public int MaxMethodPayloadCharacterCount => 120 * 1024;

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, VariantValue> Reported => _reported;

        /// <inheritdoc/>
        public string DeviceId { get; private set; }

        /// <inheritdoc/>
        public string ModuleId { get; private set; }

        /// <inheritdoc/>
        public string SiteId { get; private set; }

        /// <inheritdoc/>
        public IClient Client { get; private set; }

        /// <summary>
        /// Create module host
        /// </summary>
        /// <param name="router"></param>
        /// <param name="settings"></param>
        /// <param name="factory"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public ModuleHost(IMethodRouter router, ISettingsRouter settings,
            IClientFactory factory, IJsonSerializer serializer, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            if (Client != null) {
                try {
                    await _lock.WaitAsync();
                    if (Client != null) {
                        _logger.Information("Stopping Module Host...");
                        try {
                            var twinSettings = new TwinCollection {
                                [TwinProperty.Connected] = false
                            };
                            await Client.UpdateReportedPropertiesAsync(twinSettings);
                            await Client.CloseAsync();
                        }
                        catch (OperationCanceledException) { }
                        catch (IotHubCommunicationException) { }
                        catch (DeviceNotFoundException) { }
                        catch (UnauthorizedException) { }
                        catch (Exception se) {
                            _logger.Error(se, "Module Host not cleanly disconnected.");
                        }
                        Client.Dispose();
                    }
                    _logger.Information("Module Host stopped.");
                }
                catch (Exception ce) {
                    _logger.Error(ce, "Module Host stopping caused exception.");
                }
                finally {
                    Client = null;
                    _reported?.Clear();
                    DeviceId = null;
                    ModuleId = null;
                    SiteId = null;
                    _lock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(string type, string siteId, string serviceInfo,
            IProcessControl reset) {
            if (Client is null) {
                try {
                    await _lock.WaitAsync();
                    if (Client is null) {
                        // Create client
                        _logger.Debug("Starting Module Host...");
                        Client = await _factory.CreateAsync(serviceInfo, reset);
                        DeviceId = _factory.DeviceId;
                        ModuleId = _factory.ModuleId;

                        // Register callback to be called when a method request is received
                        await Client.SetMethodDefaultHandlerAsync((request, _) =>
                            _router.InvokeMethodAsync(request), null);

                        await InitializeTwinAsync();

                        // Register callback to be called when settings change ...
                        await Client.SetDesiredPropertyUpdateCallbackAsync(
                            (settings, _) => ProcessSettingsAsync(settings), null);

                        // Report type of service, chosen site, and connection state
                        var twinSettings = new TwinCollection {
                            [TwinProperty.Type] = type,
                            [TwinProperty.Connected] = true
                        };

                        // Set site if provided
                        if (string.IsNullOrEmpty(SiteId)) {
                            SiteId = siteId;
                            twinSettings[TwinProperty.SiteId] = SiteId;
                        }
                        await Client.UpdateReportedPropertiesAsync(twinSettings);

                        // Done...
                        _logger.Information("Module Host started.");
                        return;
                    }
                }
                catch (Exception ex) {
                    Client?.Dispose();
                    Client = null;
                    _reported?.Clear();
                    DeviceId = null;
                    ModuleId = null;
                    SiteId = null;
                    throw ex;
                }
                finally {
                    _lock.Release();
                }
            }
            throw new InvalidOperationException("Already started");
        }

        /// <inheritdoc/>
        public async Task RefreshAsync() {
            try {
                await _lock.WaitAsync();
                if (Client != null) {
                    _twin = await Client.GetTwinAsync();
                    _reported.Clear();
                    foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Reported) {
                        _reported.AddOrUpdate(property.Key,
                            (VariantValue)_serializer.FromObject(property.Value));

                    }
                    var changes = await _settings.GetSettingsChangesAsync();
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SendEventAsync(IEnumerable<byte[]> batch, string contentType,
            string eventSchema, string contentEncoding) {
            try {
                await _lock.WaitAsync();
                if (Client != null) {
                    var messages = batch
                        .Select(ev =>
                             CreateMessage(ev, contentEncoding, contentType, eventSchema,
                                DeviceId, ModuleId))
                        .ToList();
                    try {
                        await Client.SendEventBatchAsync(messages);
                    }
                    finally {
                        messages.ForEach(m => m?.Dispose());
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SendEventAsync(byte[] data, string contentType, string eventSchema,
            string contentEncoding) {
            try {
                await _lock.WaitAsync();
                if (Client != null) {
                    using (var msg = CreateMessage(data, contentEncoding, contentType,
                        eventSchema, DeviceId, ModuleId)) {
                        await Client.SendEventAsync(msg);
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task ReportAsync(IEnumerable<KeyValuePair<string, VariantValue>> properties) {
            try {
                await _lock.WaitAsync();
                if (Client != null) {
                    var collection = new TwinCollection();
                    foreach (var property in properties) {
                        collection[property.Key] = property.Value?.ToObject<object>();
                    }
                    await Client.UpdateReportedPropertiesAsync(collection);
                    foreach (var property in properties) {
                        _reported.Remove(property.Key);
                        _reported.Add(property.Key, property.Value);
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task ReportAsync(string propertyId, VariantValue value) {
            try {
                await _lock.WaitAsync();
                if (Client != null) {
                    var collection = new TwinCollection {
                        [propertyId] = value?.ToObject<object>()
                    };
                    await Client.UpdateReportedPropertiesAsync(collection);
                    _reported.Remove(propertyId);
                    _reported.Add(propertyId, value);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SendFileAsync(string fileName, Stream stream, string contentType) {
            await Client.UploadToBlobAsync(
                $"{contentType.UrlEncode()}/{fileName.UrlEncode().TrimEnd('/')}",
                    stream);
        }

        /// <inheritdoc/>
        public async Task<string> CallMethodAsync(string deviceId, string moduleId,
            string method, string payload, TimeSpan? timeout, CancellationToken ct) {
            var request = new MethodRequest(method, Encoding.UTF8.GetBytes(payload),
                timeout, null);
            MethodResponse response;
            if (string.IsNullOrEmpty(moduleId)) {
                response = await Client.InvokeMethodAsync(deviceId, request, ct);
            }
            else {
                response = await Client.InvokeMethodAsync(deviceId, moduleId, request, ct);
            }
            if (response.Status != 200) {
                throw new MethodCallStatusException(
                    response.ResultAsJson, response.Status);
            }
            return response.ResultAsJson;
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (Client != null) {
                StopAsync().Wait();
            }
            _lock.Dispose();
        }

        /// <summary>
        /// Create message
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="contentType"></param>
        /// <param name="eventSchema"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        private static Message CreateMessage(byte[] data, string contentEncoding,
            string contentType, string eventSchema, string deviceId, string moduleId) {
            var msg = new Message(data) {
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                CreationTimeUtc = DateTime.UtcNow
            };
            if (!string.IsNullOrEmpty(contentEncoding)) {
                msg.Properties.Add(CommonProperties.ContentEncoding, contentEncoding);
            }
            if (!string.IsNullOrEmpty(eventSchema)) {
                msg.Properties.Add(CommonProperties.EventSchemaType, eventSchema);
            }
            if (!string.IsNullOrEmpty(deviceId)) {
                msg.Properties.Add(CommonProperties.DeviceId, deviceId);
            }
            if (!string.IsNullOrEmpty(moduleId)) {
                msg.Properties.Add(CommonProperties.ModuleId, moduleId);
            }
            msg.Properties.Add(CommonProperties.CreationTimeUtc,
                msg.CreationTimeUtc.ToString());
            return msg;
        }

        /// <summary>
        /// Reads the twin including desired and reported settings and applies them to the
        /// settings controllers.  updates the twin for any changes resulting from the
        /// update.  Reported values are cached until user calls Refresh.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeTwinAsync() {

            System.Diagnostics.Debug.Assert(_lock.CurrentCount == 0);

            // Process initial setting snapshot from twin
            _twin = await Client.GetTwinAsync();

            if (!string.IsNullOrEmpty(_twin.DeviceId)) {
                DeviceId = _twin.DeviceId;
            }
            if (!string.IsNullOrEmpty(_twin.ModuleId)) {
                ModuleId = _twin.ModuleId;
            }
            _logger.Information("Initialize device twin for {deviceId} - {moduleId}",
                DeviceId, ModuleId ?? "standalone");

            var desired = new Dictionary<string, VariantValue>();
            var reported = new Dictionary<string, VariantValue>();

            // Start with reported values which we desire to be re-applied
            _reported.Clear();
            foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Reported) {
                var value = (VariantValue)_serializer.FromObject(property.Value);
                if (value.Type == VariantValueType.Object &&
                    value.TryGetValue("status", out var val) &&
                    value.Keys.Count() == 1) {
                    // Clear status properties from twin
                    _reported.AddOrUpdate(property.Key, null);
                    continue;
                }
                if (!ProcessEdgeHostSettings(property.Key, value)) {
                    desired[property.Key] = value;
                    _reported.AddOrUpdate(property.Key, value);
                }
            }
            // Apply desired values on top.
            foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Desired) {
                var value = (VariantValue)_serializer.FromObject(property.Value);
                if (!ProcessEdgeHostSettings(property.Key, value, reported)) {
                    desired[property.Key] = value;
                }
            }

            // Process settings on controllers
            _logger.Debug("Applying initial state.");
            var processed = await _settings.ProcessSettingsAsync(desired);

            // If there are changes, update what should be reported back.
            foreach (var property in processed) {
                var exists = _twin.Properties.Reported.Contains(property.Key);
                if (property.Value == null) {
                    if (exists) {
                        // If exists as reported, remove
                        reported.AddOrUpdate(property.Key, null);
                        _reported.Remove(property.Key);
                    }
                }
                else {
                    if (exists) {
                        // If exists and same as property value, continue
                        var r = (VariantValue)_serializer.FromObject(
                            _twin.Properties.Reported[property.Key]);
                        if (r == property.Value) {
                            continue;
                        }
                    }
                    else if (property.Value == null) {
                        continue;
                    }

                    // Otherwise, add to reported properties
                    reported[property.Key] = property.Value;
                    _reported.AddOrUpdate(property.Key, property.Value);
                }
            }
            if (reported.Count > 0) {
                _logger.Debug("Reporting initial state.");
                var collection = new TwinCollection();
                foreach (var item in reported) {
                    collection[item.Key] = item.Value?.ToObject<object>();
                }
                await Client.UpdateReportedPropertiesAsync(collection);
            }
        }

        /// <summary>
        /// Update device client settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private async Task ProcessSettingsAsync(TwinCollection settings) {
            if (settings.Count > 0) {
                try {
                    await _lock.WaitAsync();

                    // Patch existing reported properties
                    var desired = new Dictionary<string, VariantValue>();
                    var reporting = new Dictionary<string, VariantValue>();

                    foreach (KeyValuePair<string, dynamic> property in settings) {
                        var value = (VariantValue)_serializer.FromObject(property.Value);
                        if (!ProcessEdgeHostSettings(property.Key, value, reporting)) {
                            desired.AddOrUpdate(property.Key, value);
                        }
                    }

                    if (reporting != null && reporting.Count != 0) {
                        var collection = new TwinCollection();
                        foreach (var item in reporting) {
                            collection[item.Key] = item.Value?.ToObject<object>();
                        }
                        await Client.UpdateReportedPropertiesAsync(collection);
                        _logger.Debug("Internal state updated...", reporting);
                    }

                    // Any controller properties left?
                    if (desired.Count == 0) {
                        return;
                    }

                    _logger.Debug("Processing new settings...");
                    var reported = await _settings.ProcessSettingsAsync(desired);

                    if (reported != null && reported.Count != 0) {
                        _logger.Debug("Reporting setting results...");
                        var collection = new TwinCollection();
                        foreach (var item in reported) {
                            collection[item.Key] = item.Value?.ToObject<object>();
                        }
                        await Client.UpdateReportedPropertiesAsync(collection);
                        foreach (var item in reported) {
                            _reported.AddOrUpdate(item.Key, item.Value);
                        }
                    }
                    _logger.Information("New settings processed.");
                }
                finally {
                    _lock.Release();
                }
            }
        }

        /// <summary>
        /// Process default settings
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="processed"></param>
        /// <returns></returns>
        private bool ProcessEdgeHostSettings(string key, VariantValue value,
            IDictionary<string, VariantValue> processed = null) {
            switch (key.ToLowerInvariant()) {
                case TwinProperty.Connected:
                case TwinProperty.Type:
                    break;
                case TwinProperty.SiteId:
                    SiteId = (string)value;
                    break;
                default:
                    return false;
            }
            if (processed != null) {
                processed[key] = value;
            }
            return true;
        }

        private Twin _twin;

        private readonly IMethodRouter _router;
        private readonly ISettingsRouter _settings;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly IClientFactory _factory;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, VariantValue> _reported =
            new Dictionary<string, VariantValue>();
    }
}
