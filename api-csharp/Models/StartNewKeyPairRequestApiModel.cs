// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models
{
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class StartNewKeyPairRequestApiModel
    {
        /// <summary>
        /// Initializes a new instance of the StartNewKeyPairRequestApiModel
        /// class.
        /// </summary>
        public StartNewKeyPairRequestApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the StartNewKeyPairRequestApiModel
        /// class.
        /// </summary>
        public StartNewKeyPairRequestApiModel(string applicationId = default(string), string certificateGroupId = default(string), string certificateTypeId = default(string), string subjectName = default(string), IList<string> domainNames = default(IList<string>), string privateKeyFormat = default(string), string privateKeyPassword = default(string), string authorityId = default(string))
        {
            ApplicationId = applicationId;
            CertificateGroupId = certificateGroupId;
            CertificateTypeId = certificateTypeId;
            SubjectName = subjectName;
            DomainNames = domainNames;
            PrivateKeyFormat = privateKeyFormat;
            PrivateKeyPassword = privateKeyPassword;
            AuthorityId = authorityId;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ApplicationId")]
        public string ApplicationId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "CertificateGroupId")]
        public string CertificateGroupId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "CertificateTypeId")]
        public string CertificateTypeId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "SubjectName")]
        public string SubjectName { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "DomainNames")]
        public IList<string> DomainNames { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "PrivateKeyFormat")]
        public string PrivateKeyFormat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "PrivateKeyPassword")]
        public string PrivateKeyPassword { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "AuthorityId")]
        public string AuthorityId { get; set; }

    }
}
