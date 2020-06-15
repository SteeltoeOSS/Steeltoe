﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public interface ICredHubClient
    {
        /// <summary>
        /// Write a new credential to CredHub, or overwrite an existing credential with a new value.
        /// </summary>
        /// <param name="credentialRequest">The credential to write to CredHub; must not be <see langword="null" /></param>
        /// <typeparam name="T">Type of CredHub credential to be written</typeparam>
        /// <returns>Newly written credential</returns>
        Task<CredHubCredential<T>> WriteAsync<T>(CredentialSetRequest credentialRequest);

        /// <summary>
        /// Generate a new credential in CredHub, or overwrite an existing credential with a new generated value.
        /// </summary>
        /// <param name="requestParameters">Parameters for the new credential to be generated in CredHub</param>
        /// <typeparam name="T">Type of CredHub credential to be generated</typeparam>
        /// <returns>The details of the generated credential</returns>
        Task<CredHubCredential<T>> GenerateAsync<T>(CredHubGenerateRequest requestParameters);

        /// <summary>
        /// Regenerate a credential in CredHub. Only credentials that were previously generated can be re-generated.
        /// </summary>
        /// <param name="name">Name of the credential to regenerate</param>
        /// <typeparam name="T">Type of CredHub credential to be regenerated</typeparam>
        /// <returns>Regenerated credential details</returns>
        Task<CredHubCredential<T>> RegenerateAsync<T>(string name);

        /// <summary>
        /// Regenerate all certificates generated by a given certificate authority
        /// </summary>
        /// <param name="certificateAuthority">Name of certificate authority used to generate certificates that should be regenerated</param>
        /// <returns>List of regenerated certificates</returns>
        Task<RegeneratedCertificates> BulkRegenerateAsync(string certificateAuthority);

        /// <summary>
        /// Retrieve a credential using its ID, as returned in a write request.
        /// </summary>
        /// <param name="id">ID of the credential; must not be <see langword="null" /></param>
        /// <typeparam name="T">Type of CredHub credential to be retrieved</typeparam>
        /// <returns>The details of the retrieved credential</returns>
        Task<CredHubCredential<T>> GetByIdAsync<T>(Guid id);

        /// <summary>
        /// Retrieve a credential using its name, as passed to a write request. Only the current credential value will be returned.
        /// </summary>
        /// <param name="name">Name of the credential; must not be <see langword="null" /></param>
        /// <typeparam name="T">Type of CredHub credential to be retrieved</typeparam>
        /// <returns>The details of the retrieved credential</returns>
        Task<CredHubCredential<T>> GetByNameAsync<T>(string name);

        /// <summary>
        /// Retrieve a credential using its name, as passed to a write request. A collection of all stored values for the named credential will be returned,
        /// including historical values.
        /// </summary>
        /// <param name="name">Name of the credential; must not be <see langword="null" /></param>
        /// <param name="entries">Maximum number of entries to retrieve</param>
        /// <typeparam name="T">Type of CredHub credential to be retrieved</typeparam>
        /// <returns>The details of the retrieved credential</returns>
        Task<List<CredHubCredential<T>>> GetByNameWithHistoryAsync<T>(string name, int entries = 10);

        /// <summary>
        /// Search for credentials with a full or partial name.
        /// </summary>
        /// <param name="name">The name of the credential; must not be <see langword="null" /></param>
        /// <returns>A summary of the credential search results</returns>
        Task<List<FoundCredential>> FindByNameAsync(string name);

        /// <summary>
        /// Find a credential using a path.
        /// </summary>
        /// <param name="path">The path to the credential; must not be <see langword="null" /></param>
        /// <returns>A summary of the credential search results</returns>
        Task<List<FoundCredential>> FindByPathAsync(string path);

        /// <summary>
        /// Delete a credential by its full name.
        /// </summary>
        /// <param name="name">the name of the credential; must not be <see langword="null" /></param>
        /// <returns>Boolean indicating success/failure</returns>
        Task<bool> DeleteByNameAsync(string name);

        /// <summary>
        /// Get the permissions associated with a credential.
        /// </summary>
        /// <param name="credentialName">The name of the credential; must not be <see langword="null" /></param>
        /// <returns>List of permssions</returns>
        Task<List<CredentialPermission>> GetPermissionsAsync(string credentialName);

        /// <summary>
        /// Add permissions to an existing credential.
        /// </summary>
        /// <param name="credentialName">The name of the credential; must not be <see langword="null" /></param>
        /// <param name="permissions">A collection of permissions to add</param>
        /// <returns>List of permissions</returns>
        Task<List<CredentialPermission>> AddPermissionsAsync(string credentialName, List<CredentialPermission> permissions);

        /// <summary>
        /// Delete a permission associated with a credential.
        /// </summary>
        /// <param name="credentialName">the name of the credential; must not be <see langword="null" /></param>
        /// <param name="actor">the actor of the permission; must not be <see langword="null" /></param>
        /// <returns>Returns true if deleted, false if failed</returns>
        Task<bool> DeletePermissionAsync(string credentialName, string actor);

        /// <summary>
        /// Search the provided data structure of bound service credentials, looking for references to CredHub credentials.
        /// </summary>
        /// <param name="serviceData">a data structure of bound service credentials, as would be parsed from the {@literal VCAP_SERVICES} environment variable provided to applications running on Cloud Foundry</param>
        /// <returns>the serviceData structure with CredHub references replaced by stored credential values</returns>
        Task<string> InterpolateServiceDataAsync(string serviceData);
    }
}
