﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LVMS.Zipato.Model;
using PortableRest;

namespace LVMS.Zipato
{
    public partial class ZipatoClient
    {
        Model.Attribute[] _cachedAttributesList;
        Dictionary<Guid, Model.Attribute> _cachedAttributes;
        public async Task<Model.Attribute[]> GetAttributesAsync(bool allowCache = true)
        {
            CheckInitialized();

            if (allowCache && _cachedAttributesList != null)
                return _cachedAttributesList;

            var request = new RestRequest("attributes", HttpMethod.Get);
            
            PrepareRequest(request);
            var result = await _httpClient.ExecuteAsync<Model.Attribute[]>(request);

            if (allowCache)
            {
                _cachedAttributesList = result;
            }
            return result;
        }

        public async Task<Model.Attribute> GetAttributeAsync(Guid uuid, bool allowCache = true)
        {
            CheckInitialized();

            if (allowCache && _cachedAttributes != null && _cachedAttributes.ContainsKey(uuid))
                return _cachedAttributes[uuid];

            var request = new RestRequest("attributes/" + uuid, HttpMethod.Get);

            PrepareRequest(request);
            var result = await _httpClient.ExecuteAsync<Model.Attribute>(request);

            if (allowCache)
            {
                if (_cachedAttributes == null)
                    _cachedAttributes = new Dictionary<Guid, Model.Attribute>();

                _cachedAttributes.Add(uuid, result);
            }
            return result;
        }

        /// <summary>
        /// Send a command to change the state of an endpoint. This method will seach for an endpoint
        /// with given name. If the endpoint was found, an attribute named state is searched and finally
        /// a command will be send to Zipato to change the state for that attribute UUID.
        /// </summary>
        /// <param name="endpointName">Endpoint name</param>
        /// <param name="newState">The new state</param>
        /// <returns>True when the request was executed succesful, otherwise False</returns>
        public async Task<bool> SetOnOffState(string endpointName, bool newState)
        {
            Endpoint endpoint = await GetEndpointAsync(endpointName);
            return await SendOnOffStateAsync(endpoint, newState);
        }

        /// <summary>
        /// Finds an endpoint by name. Will call GetEndpointsAsync if no endpoints are loaded yet.
        /// Will use cached data whenever possible.
        /// </summary>
        /// <param name="endpointName">Endpoint name</param>
        /// <returns>An Endpoint instance</returns>
        public async Task<Endpoint> GetEndpointAsync(string endpointName)
        {
            var endpoints = await GetEndpointsAsync(true);
            var endpoint = endpoints.First(e => e.Name == endpointName);
            return endpoint;
        }

        /// <summary>
        /// Send a command to change the state of an endpoint. This method will search for an attribute
        /// named state in the endpoint's attributes list and will use that attribute UUID.
        /// </summary>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="newState">The new state</param>
        /// <returns>True when the request was executed succesful, otherwise False</returns>
        public async Task<bool> SendOnOffStateAsync(Endpoint endpoint, bool newState)
        {
            CheckInitialized();           

            Model.Attribute stateAttribute = await GetAttributeAsync(endpoint, Enums.CommonAttributeNames.STATE);
            return await SendOnOffStateAsync(stateAttribute.Uuid, newState);
        }

        /// <summary>
        /// Finds an attribute with a given name on an endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint instance</param>
        /// <param name="attributeName">Attribute name</param>
        /// <returns>Attribute instance</returns>
        private async Task<Model.Attribute> GetAttributeAsync(Endpoint endpoint, Enums.CommonAttributeNames attribute)
        {
            string attributeName = Enum.GetName(typeof(Enums.CommonAttributeNames), attribute);
            return await GetAttributeAsync(endpoint, attributeName);
        }

        /// <summary>
        /// Finds an attribute with a given name on an endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint instance</param>
        /// <param name="attributeName">Attribute name</param>
        /// <returns>Attribute instance</returns>
        private async Task<Model.Attribute> GetAttributeAsync(Endpoint endpoint, string attributeName)
        {
            if (endpoint.Attributes == null)
                endpoint = await GetEndpointAsync(endpoint.Uuid);
            if (endpoint.Attributes == null)
                throw new Exceptions.CannotChangeStateException("Couldn't retrieve attribute list for endpoint: " + endpoint.Uuid);

            var stateAttribute = endpoint.Attributes.FirstOrDefault(a => (a.Name != null && a.Name.ToLowerInvariant() == attributeName.ToLowerInvariant()) ||
                            (a.AttributeName != null && a.AttributeName.ToLowerInvariant() == attributeName.ToLowerInvariant()));
            if (stateAttribute == null)
                throw new Exceptions.CannotChangeStateException("Couldn't find an attribute with name '" +attributeName + "' on endpoint: " + endpoint.Uuid);
            return stateAttribute;
        }

        /// <summary>
        /// Send a command to change the state of an endpoint.
        /// </summary>
        /// <param name="attributeUuid">Attribute UUID</param>
        /// <param name="newState">The new state</param>
        /// <returns>True when the request was executed succesful, otherwise False</returns>
        public async Task<bool> SendOnOffStateAsync(Guid attributeUuid, bool newState)
        {
            CheckInitialized();

            // Send a PUT request with 'true' or 'false' as plain-text in the body.
            // Had to modify PortableRest package to support this.
            var request = new RestRequest("attributes/" + attributeUuid + "/value", HttpMethod.Put);
            request.ContentType = ContentTypes.PlainText;
            PrepareRequest(request);            
            request.AddParameter(newState.ToString().ToLowerInvariant());
            var returnValue = await _httpClient.ExecuteAsync<object>(request);
            return true;
        }

        /// <summary>
        /// Get the ON/OFF state of an endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint. The endpoint must have an attribute named state</param>
        /// <returns>True when the endpoint if on / enabled, otherwise False</returns>
        public async Task<bool> GetOnOffStateAsync(string endpointName)
        {
            Endpoint endpoint = await GetEndpointAsync(endpointName);
            return await GetOnOffStateAsync(endpoint);
        }


        /// <summary>
        /// Get the ON/OFF state of an endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint which must have an attribute named state</param>
        /// <returns>True when the endpoint if on / enabled, otherwise False</returns>
        public async Task<bool> GetOnOffStateAsync(Endpoint endpoint)
        {
            CheckInitialized();

            Model.Attribute stateAttribute = await GetAttributeAsync(endpoint, Enums.CommonAttributeNames.STATE);
            return await GetOnOffStateAsync(stateAttribute.Uuid);
        }

        /// <summary>
        /// Get the ON/OFF state of an endpoint
        /// </summary>
        /// <param name="attributeUuid">Attribute UUID</param>
        /// <returns>True when the endpoint if on / enabled, otherwise False</returns>
        public async Task<bool> GetOnOffStateAsync(Guid attributeUuid)
        {
            CheckInitialized();

            var request = new RestRequest("attributes/" + attributeUuid + "/value", HttpMethod.Get);

            PrepareRequest(request);
            var result = await _httpClient.ExecuteAsync<Model.AttributeValue>(request);
            return bool.Parse(result.Value);
        }

        /// <summary>
        /// Get the value of an attribute
        /// </summary>
        /// <param name="attributeUuid">Attribute UUID</param>
        /// <returns>The value of the attribute, converted to T</returns>
        public async Task<T> GetAttributeValueAsync<T>(Model.Attribute attribute)
        {
            CheckInitialized();

            return await GetAttributeValueAsync<T>(attribute.Uuid);
        }

        /// <summary>
        /// Get the value of an attribute
        /// </summary>
        /// <param name="attributeUuid">Attribute UUID</param>
        /// <returns>The value of the attribute, converted to T</returns>
        public async Task<T> GetAttributeValueAsync<T>(Guid attributeUuid)
        {
            CheckInitialized();

            var request = new RestRequest("attributes/" + attributeUuid + "/value", HttpMethod.Get);

            PrepareRequest(request);
            var result = await _httpClient.ExecuteAsync<Model.AttributeValue>(request);
            return Utils.ChangeType<T>(result.Value);
        }

        /// <summary>
        /// Get the value of an attribute
        /// </summary>
        /// <param name="endpoint">The endpoint to retrieve the attribute from</param>
        /// <param name="attribute">The attribute to read</param>
        /// <returns>The value of the attribute, converted to T</returns>
        public async Task<T> GetAttributeValueAsync<T>(Endpoint endpoint, Enums.CommonAttributeNames attribute)
        {
            CheckInitialized();

            string attributeName = Enum.GetName(typeof(Enums.CommonAttributeNames), attribute);
            return await GetAttributeValueAsync<T>(endpoint, attributeName);
        }

        /// <summary>
        /// Get the value of an attribute
        /// </summary>
        /// <param name="endpoint">The endpoint to retrieve the attribute from</param>
        /// <param name="attribute">The attribute to read</param>
        /// <returns>The value of the attribute, converted to T</returns>
        public async Task<T> GetAttributeValueAsync<T>(Endpoint endpoint, string attributeName)
        {
            CheckInitialized();
            var attribute = await GetAttributeAsync(endpoint, attributeName);
            return await GetAttributeValueAsync<T>(attribute);
        }
    }
}