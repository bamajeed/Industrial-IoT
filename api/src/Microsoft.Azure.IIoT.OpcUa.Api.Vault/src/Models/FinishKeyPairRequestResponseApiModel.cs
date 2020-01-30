// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Finish request results
    /// </summary>
    [DataContract]
    public sealed class FinishNewKeyPairRequestResponseApiModel {

        /// <summary>
        /// Request
        /// </summary>
        [DataMember(Name = "request",
            EmitDefaultValue = false)]
        public CertificateRequestRecordApiModel Request { get; set; }

        /// <summary>
        /// Signed certificate
        /// </summary>
        [DataMember(Name = "certificate",
            EmitDefaultValue = false)]
        public X509CertificateApiModel Certificate { get; set; }

        /// <summary>
        /// Private key
        /// </summary>
        [DataMember(Name = "privateKey",
            EmitDefaultValue = false)]
        public PrivateKeyApiModel PrivateKey { get; set; }
    }
}
