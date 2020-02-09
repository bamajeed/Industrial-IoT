// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Implementation of supervisor module api.
    /// </summary>
    public sealed class TwinModuleClient : ITwinModuleApi {

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="serializer"></param>
        public TwinModuleClient(IMethodClient methodClient, string deviceId, string moduleId,
            IJsonSerializer serializer = null) {
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _methodClient = methodClient ?? throw new ArgumentNullException(nameof(methodClient));
            _moduleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public TwinModuleClient(IMethodClient methodClient, ITwinModuleConfig config,
            IJsonSerializer serializer) :
            this(methodClient, config?.DeviceId, config?.ModuleId, serializer) {
        }

        /// <inheritdoc/>
        public async Task<BrowseResponseApiModel> NodeBrowseFirstAsync(EndpointApiModel endpoint,
            BrowseRequestApiModel request, CancellationToken ct) {
            if (endpoint is null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request is null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "Browse_V2", _serializer.Serialize(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<BrowseResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseApiModel> NodeBrowseNextAsync(EndpointApiModel endpoint,
            BrowseNextRequestApiModel request, CancellationToken ct) {
            if (endpoint is null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request is null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ContinuationToken is null) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "BrowseNext_V2", _serializer.Serialize(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<BrowseNextResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseApiModel> NodeBrowsePathAsync(EndpointApiModel endpoint,
            BrowsePathRequestApiModel request, CancellationToken ct) {
            if (endpoint is null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request is null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.BrowsePaths is null || request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p is null || p.Length == 0)) {
                throw new ArgumentNullException(nameof(request.BrowsePaths));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "BrowsePath_V2", _serializer.Serialize(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<BrowsePathResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseApiModel> NodeReadAsync(EndpointApiModel endpoint,
            ReadRequestApiModel request, CancellationToken ct) {
            if (endpoint is null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request is null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes is null || request.Attributes.Count == 0) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "NodeRead_V2", _serializer.Serialize(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<ReadResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseApiModel> NodeWriteAsync(EndpointApiModel endpoint,
            WriteRequestApiModel request, CancellationToken ct) {
            if (endpoint is null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request is null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes is null || request.Attributes.Count == 0) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "NodeWrite_V2", _serializer.Serialize(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<WriteResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseApiModel> NodeValueReadAsync(EndpointApiModel endpoint,
            ValueReadRequestApiModel request, CancellationToken ct) {
            if (endpoint is null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request is null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "ValueRead_V2", _serializer.Serialize(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<ValueReadResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseApiModel> NodeValueWriteAsync(EndpointApiModel endpoint,
            ValueWriteRequestApiModel request, CancellationToken ct) {
            if (endpoint is null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request is null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Value is null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "ValueWrite_V2", _serializer.Serialize(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<ValueWriteResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            EndpointApiModel endpoint, MethodMetadataRequestApiModel request, CancellationToken ct) {
            if (endpoint is null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request is null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "MethodMetadata_V2", _serializer.Serialize(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<MethodMetadataResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            EndpointApiModel endpoint, MethodCallRequestApiModel request, CancellationToken ct) {
            if (endpoint is null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request is null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "MethodCall_V2", _serializer.Serialize(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<MethodCallResponseApiModel>(response);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _moduleId;
        private readonly string _deviceId;
    }
}
