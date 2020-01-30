// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.ComponentModel;

    /// <summary>
    /// Node value read request webservice api model
    /// </summary>
    [DataContract]
    public class ValueReadRequestApiModel {

        /// <summary>
        /// Node to read from (mandatory)
        /// </summary>
        [DataMember(Name = "nodeId",
            EmitDefaultValue = false)]
        [DefaultValue(null)]
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// the actual node.
        /// </summary>
        [DataMember(Name = "browsePath",
            EmitDefaultValue = false)]
        [DefaultValue(null)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Index range to read, e.g. 1:2,0:1 for 2 slices
        /// out of a matrix or 0:1 for the first item in
        /// an array, string or bytestring.
        /// See 7.22 of part 4: NumericRange.
        /// </summary>
        [DataMember(Name = "indexRange",
            EmitDefaultValue = false)]
        [DefaultValue(null)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header",
            EmitDefaultValue = false)]
        [DefaultValue(null)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
