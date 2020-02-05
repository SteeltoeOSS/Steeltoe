// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Messaging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Integration.Support
{
    /// <summary>
    /// A message builder that creates immutable GenericMessages.
    /// </summary>
    public interface IMessageBuilder
    {
        /// <summary>
        /// Adds an expiration date in the headers
        /// </summary>
        /// <param name="expirationDate">expiration date added to header</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetExpirationDate(long expirationDate);

        /// <summary>
        /// Adds an expiration date in the headers
        /// </summary>
        /// <param name="expirationDate">expiration date added to header</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetExpirationDate(DateTime? expirationDate);

        /// <summary>
        /// Adds the correlationId to the headers
        /// </summary>
        /// <param name="correlationId">the id to add</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetCorrelationId(object correlationId);

        /// <summary>
        /// Adds a sequence details header to the message
        /// </summary>
        /// <param name="correlationId">correlation id to use in sequence</param>
        /// <param name="sequenceNumber">the sequence number</param>
        /// <param name="sequenceSize">the size of the sequence number</param>
        /// <returns>the builder</returns>
        IMessageBuilder PushSequenceDetails(object correlationId, int sequenceNumber, int sequenceSize);

        /// <summary>
        /// Removes a sequence details header from the message
        /// </summary>
        /// <returns>the builder</returns>
        IMessageBuilder PopSequenceDetails();

        /// <summary>
        /// Adds a reply channel to the message
        /// </summary>
        /// <param name="replyChannel">the reply channel</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetReplyChannel(IMessageChannel replyChannel);

        /// <summary>
        /// Adds a reply channel name to the message
        /// </summary>
        /// <param name="replyChannelName">the reply channel name</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetReplyChannelName(string replyChannelName);

        /// <summary>
        /// Adds an error channel to the messsage
        /// </summary>
        /// <param name="errorChannel">the error channel</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetErrorChannel(IMessageChannel errorChannel);

        /// <summary>
        /// Adds an error channel name to the message
        /// </summary>
        /// <param name="errorChannelName">the name of the error channel</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetErrorChannelName(string errorChannelName);

        /// <summary>
        /// Adds sequence details header to the message
        /// </summary>
        /// <param name="sequenceNumber">the sequence number</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetSequenceNumber(int sequenceNumber);

        /// <summary>
        /// Sets the size of the sequence number
        /// </summary>
        /// <param name="sequenceSize">the size</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetSequenceSize(int sequenceSize);

        /// <summary>
        /// Adds a priority header to the message
        /// </summary>
        /// <param name="priority">the priority to add</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetPriority(int priority);

        /// <summary>
        /// Remove headers from the provided map matching to the provided pattens
        /// and only after that copy the result into the target message headers.
        /// </summary>
        /// <param name="headersToCopy">the set of headers to copy</param>
        /// <param name="headerPatternsToFilter">header patterns to filter before copy</param>
        /// <returns>the builder</returns>
        IMessageBuilder FilterAndCopyHeadersIfAbsent(IDictionary<string, object> headersToCopy, params string[] headerPatternsToFilter);

        /// <summary>
        /// Add a header and value to the message
        /// </summary>
        /// <param name="headerName">name of the header</param>
        /// <param name="headerValue">value of the header item</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetHeader(string headerName, object headerValue);

        /// <summary>
        /// Add a header and value to the message if not present
        /// </summary>
        /// <param name="headerName">name of the header</param>
        /// <param name="headerValue">value of the header item</param>
        /// <returns>the builder</returns>
        IMessageBuilder SetHeaderIfAbsent(string headerName, object headerValue);

        /// <summary>
        /// Remove the headers matched by the header patterns from the message
        /// </summary>
        /// <param name="headerPatterns">header patterns to match</param>
        /// <returns>the builder</returns>
        IMessageBuilder RemoveHeaders(params string[] headerPatterns);

        /// <summary>
        /// Remove the header if present
        /// </summary>
        /// <param name="headerName">the name of the header to remove</param>
        /// <returns>the builder</returns>
        IMessageBuilder RemoveHeader(string headerName);

        /// <summary>
        /// Adds the headers to the message overwriting any existing values.
        /// </summary>
        /// <param name="headersToCopy">the headers to add</param>
        /// <returns>the builder</returns>
        IMessageBuilder CopyHeaders(IDictionary<string, object> headersToCopy);

        /// <summary>
        /// Adds the headers to the message but will not overwrite any existing values.
        /// </summary>
        /// <param name="headersToCopy">the headers to add</param>
        /// <returns>the builder</returns>
        IMessageBuilder CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy);

        /// <summary>
        /// Gets the payload of the message
        /// </summary>
        object Payload { get; }

        /// <summary>
        /// Gets the current headers of the message
        /// </summary>
        IDictionary<string, object> Headers { get; }

        /// <summary>
        /// Build the message
        /// </summary>
        /// <returns>the message</returns>
        IMessage Build();
    }

    /// <summary>
    /// A typed MessageBuilder.
    /// </summary>
    /// <typeparam name="T">the type of the payload</typeparam>
    public interface IMessageBuilder<T> : IMessageBuilder
    {
        /// <summary>
        /// Adds an expiration date in the headers
        /// </summary>
        /// <param name="expirationDate">expiration date added to header</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetExpirationDate(long expirationDate);

        /// <summary>
        /// Adds an expiration date in the headers
        /// </summary>
        /// <param name="expirationDate">expiration date added to header</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetExpirationDate(DateTime? expirationDate);

        /// <summary>
        /// Adds the correlationId to the headers
        /// </summary>
        /// <param name="correlationId">the id to add</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetCorrelationId(object correlationId);

        /// <summary>
        /// Adds a sequence details header to the message
        /// </summary>
        /// <param name="correlationId">correlation id to use in sequence</param>
        /// <param name="sequenceNumber">the sequence number</param>
        /// <param name="sequenceSize">the size of the sequence number</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> PushSequenceDetails(object correlationId, int sequenceNumber, int sequenceSize);

        /// <summary>
        /// Removes a sequence details header from the message
        /// </summary>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> PopSequenceDetails();

        /// <summary>
        /// Adds a reply channel to the message
        /// </summary>
        /// <param name="replyChannel">the reply channel</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetReplyChannel(IMessageChannel replyChannel);

        /// <summary>
        /// Adds a reply channel name to the message
        /// </summary>
        /// <param name="replyChannelName">the reply channel name</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetReplyChannelName(string replyChannelName);

        /// <summary>
        /// Adds an error channel to the messsage
        /// </summary>
        /// <param name="errorChannel">the error channel</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetErrorChannel(IMessageChannel errorChannel);

        /// <summary>
        /// Adds an error channel name to the message
        /// </summary>
        /// <param name="errorChannelName">the name of the error channel</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetErrorChannelName(string errorChannelName);

        /// <summary>
        /// Adds sequence details header to the message
        /// </summary>
        /// <param name="sequenceNumber">the sequence number</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetSequenceNumber(int sequenceNumber);

        /// <summary>
        /// Sets the size of the sequence number
        /// </summary>
        /// <param name="sequenceSize">the size</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetSequenceSize(int sequenceSize);

        /// <summary>
        /// Adds a priority header to the message
        /// </summary>
        /// <param name="priority">the priority to add</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetPriority(int priority);

        /// <summary>
        /// Remove headers from the provided map matching to the provided pattens
        /// and only after that copy the result into the target message headers.
        /// </summary>
        /// <param name="headersToCopy">the set of headers to copy</param>
        /// <param name="headerPatternsToFilter">header patterns to filter before copy</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> FilterAndCopyHeadersIfAbsent(IDictionary<string, object> headersToCopy, params string[] headerPatternsToFilter);

        /// <summary>
        /// Gets the payload of the message
        /// </summary>
        new T Payload { get; }

        /// <summary>
        /// Add a header and value to the message
        /// </summary>
        /// <param name="headerName">name of the header</param>
        /// <param name="headerValue">value of the header item</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetHeader(string headerName, object headerValue);

        /// <summary>
        /// Add a header and value to the message if not present
        /// </summary>
        /// <param name="headerName">name of the header</param>
        /// <param name="headerValue">value of the header item</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> SetHeaderIfAbsent(string headerName, object headerValue);

        /// <summary>
        /// Remove the headers matched by the header patterns from the message
        /// </summary>
        /// <param name="headerPatterns">header patterns to match</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> RemoveHeaders(params string[] headerPatterns);

        /// <summary>
        /// Remove the header if present
        /// </summary>
        /// <param name="headerName">the name of the header to remove</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> RemoveHeader(string headerName);

        /// <summary>
        /// Adds the headers to the message overwriting any existing values.
        /// </summary>
        /// <param name="headersToCopy">the headers to add</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> CopyHeaders(IDictionary<string, object> headersToCopy);

        /// <summary>
        /// Adds the headers to the message but will not overwrite any existing values.
        /// </summary>
        /// <param name="headersToCopy">the headers to add</param>
        /// <returns>the builder</returns>
        new IMessageBuilder<T> CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy);

        /// <summary>
        /// Build the message
        /// </summary>
        /// <returns>the message</returns>
        new IMessage<T> Build();
    }
}
