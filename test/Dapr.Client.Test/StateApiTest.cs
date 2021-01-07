﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Autogenerated = Dapr.Client.Autogen.Grpc.v1;
    using FluentAssertions;
    using Google.Protobuf;
    using Grpc.Core;
    using Grpc.Net.Client;
    using StateConsistency = Dapr.Client.Autogen.Grpc.v1.StateOptions.Types.StateConsistency;
    using StateConcurrency = Dapr.Client.Autogen.Grpc.v1.StateOptions.Types.StateConcurrency;
    using Xunit;
    using System.Threading;

    public class StateApiTest
    {
        [Fact]
        public async Task GetStateAsync_CanReadState()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.GetStateAsync<Widget>("testStore", "test");

            // Create Response & Respond
            var data = new Widget() { Size = "small", Color = "yellow", };
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            await SendResponseWithState(data, entry);

            // Get response and validate
            var state = await task;
            state.Size.Should().Be("small");
            state.Color.Should().Be("yellow");
        }

        [Fact]
        public async Task GetBulkStateAsync_CanReadState()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var key = "test";
            var task = daprClient.GetBulkStateAsync("testStore", new List<string>() { key }, null);

            // Create Response & Respond
            var data = "value";
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            await SendResponseWithBulkState(key, data, entry);

            // Get response and validate
            var state = await task;
            state.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetBulkStateAsync_ValidateRequest()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var metadata = new Dictionary<string, string>
            {
                { "partitionKey", "mypartition" }
            };

            var key = "test";
            var task = daprClient.GetBulkStateAsync("testStore", new List<string>() { key }, null, metadata : metadata);

            // Create Response & Validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.GetBulkStateRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.Metadata.Should().BeEquivalentTo(metadata);
        }

        [Fact]
        public async Task GetStateAndEtagAsync_CanReadState()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.GetStateAndETagAsync<Widget>("testStore", "test");

            // Create Response & Respond
            var data = new Widget() { Size = "small", Color = "yellow", };
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            await SendResponseWithState(data, entry, "Test_Etag");

            // Get response and validate
            var (state, etag) = await task;
            state.Size.Should().Be("small");
            state.Color.Should().Be("yellow");
            etag.Should().Be("Test_Etag");
        }

        [Fact]
        public async Task GetStateAsync_CanReadEmptyState_ReturnsDefault()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.GetStateAsync<Widget>("testStore", "test", ConsistencyMode.Eventual);

            // Create Response & Respond
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            await SendResponseWithState<Widget>(null, entry);

            // Get response and validate
            var state = await task;
            state.Should().BeNull();
        }

        [Theory]
        [InlineData(ConsistencyMode.Eventual, StateConsistency.ConsistencyEventual)]
        [InlineData(ConsistencyMode.Strong, StateConsistency.ConsistencyStrong)]
        public async Task GetStateAsync_ValidateRequest(ConsistencyMode consistencyMode, StateConsistency expectedConsistencyMode)
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.GetStateAsync<Widget>("testStore", "test", consistencyMode);

            // Get Request & Validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.GetStateRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.Key.Should().Be("test");
            request.Consistency.Should().Be(expectedConsistencyMode);

            // Create Response & Respond
            await SendResponseWithState<Widget>(null, entry);

            // Get response and validate
            var state = await task;
            state.Should().BeNull();
        }

        [Fact]
        public async Task GetStateAndEtagAsync_ValidateRequest()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();
            var metadata = new Dictionary<string, string>
            {
                { "partitionKey", "mypartition" }
            };

            var task = daprClient.GetStateAsync<Widget>("testStore", "test", metadata : metadata);

            // Get Request & Validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.GetStateRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.Key.Should().Be("test");
            request.Metadata.Should().BeEquivalentTo(metadata);

            // Create Response & Respond
            await SendResponseWithState<Widget>(null, entry);

            // Get response and validate
            var state = await task;
            state.Should().BeNull();
        }

        [Fact]
        public async Task GetStateAsync_ThrowsForNonSuccess()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            // Create Response & Respond
            var task = daprClient.GetStateAsync<Widget>("testStore", "test");
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var response = GrpcUtils.CreateResponse(HttpStatusCode.NotAcceptable);
            entry.Completion.SetResult(response);

            await FluentActions.Awaiting(async () => await task).Should().ThrowAsync<RpcException>();
        }

        [Fact]
        public async Task SaveStateAsync_CanSaveState()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var widget = new Widget() { Size = "small", Color = "yellow", };
            var task = daprClient.SaveStateAsync("testStore", "test", widget);

            // Get Request and validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.SaveStateRequest>(entry.Request);

            request.StoreName.Should().Be("testStore");
            request.States.Count.Should().Be(1);
            var state = request.States[0];
            state.Key.Should().Be("test");

            var stateJson = state.Value.ToStringUtf8();
            var stateFromRequest = JsonSerializer.Deserialize<Widget>(stateJson);
            stateFromRequest.Size.Should().Be(widget.Size);
            stateFromRequest.Color.Should().Be(widget.Color);
        }

        [Fact]
        public async Task GetStateAsync_WithCancelledToken()
        {
           // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient, ThrowOperationCanceledOnCancellation = true })
                .Build();

            var widget = new Widget() { Size = "small", Color = "yellow", };
            var task = daprClient.SaveStateAsync("testStore", "test", widget);

            var ctSource = new CancellationTokenSource();
            CancellationToken ct = ctSource.Token;
            ctSource.Cancel();

            await FluentActions.Awaiting(async () => await daprClient.GetStateAsync<Widget>("testStore", "test", cancellationToken: ct))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task SaveStateAsync_CanClearState()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.SaveStateAsync<object>("testStore", "test", null);

            // Get Request and validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.SaveStateRequest>(entry.Request);

            request.StoreName.Should().Be("testStore");
            request.States.Count.Should().Be(1);
            var state = request.States[0];
            state.Key.Should().Be("test");

            state.Value.Should().Equal(ByteString.Empty);
        }

        [Fact]
        public async Task SaveStateAsync_WithCancelledToken()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient, ThrowOperationCanceledOnCancellation = true })
                .Build();

            var ctSource = new CancellationTokenSource();
            CancellationToken ct = ctSource.Token;
            ctSource.Cancel();
            await FluentActions.Awaiting(async () => await daprClient.SaveStateAsync<object>("testStore", "test", null, cancellationToken: ct))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task SetStateAsync_ThrowsForNonSuccess()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var widget = new Widget() { Size = "small", Color = "yellow", };
            var task = daprClient.SaveStateAsync("testStore", "test", widget);

            // Create Response & Respond
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var response = GrpcUtils.CreateResponse(HttpStatusCode.NotAcceptable);
            entry.Completion.SetResult(response);

            await FluentActions.Awaiting(async () => await task).Should().ThrowAsync<RpcException>();
        }

        [Fact]
        public async Task ExecuteStateTransactionAsync_CanSaveState()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var stateValue1 = new Widget() { Size = "small", Color = "yellow", };
            var metadata1 = new Dictionary<string, string>()
            {
                {"a", "b" }
            };
            var options1 = new StateOptions
            {
                Concurrency = ConcurrencyMode.LastWrite
            };

            var state1 = new StateTransactionRequest("stateKey1", JsonSerializer.SerializeToUtf8Bytes(stateValue1), StateOperationType.Upsert, "testEtag", metadata1, options1);
            var stateValue2 = 100;
            var state2 = new StateTransactionRequest("stateKey2", JsonSerializer.SerializeToUtf8Bytes(stateValue2), StateOperationType.Delete);

            var stateValue3 = "teststring";
            var state3 = new StateTransactionRequest("stateKey3", JsonSerializer.SerializeToUtf8Bytes(stateValue3), StateOperationType.Upsert);

            var states = new List<StateTransactionRequest>
            {
                state1,
                state2,
                state3
            };

            var task = daprClient.ExecuteStateTransactionAsync("testStore", states);

            // Get Request and validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var transactionRequest = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.ExecuteStateTransactionRequest>(entry.Request);

            transactionRequest.StoreName.Should().Be("testStore");
            transactionRequest.Operations.Count.Should().Be(3);

            var req1 = transactionRequest.Operations[0];
            req1.Request.Key.Should().Be("stateKey1");
            req1.OperationType.Should().Be(StateOperationType.Upsert.ToString().ToLower());
            var valueJson1 = req1.Request.Value.ToStringUtf8();
            var value1 = JsonSerializer.Deserialize<Widget>(valueJson1);
            value1.Size.Should().Be(stateValue1.Size);
            value1.Color.Should().Be(stateValue1.Color);
            req1.Request.Etag.Should().Be("testEtag");
            req1.Request.Metadata.Count.Should().Be(1);
            req1.Request.Metadata["a"].Should().Be("b");
            req1.Request.Options.Concurrency.Should().Be(2);

            var req2 = transactionRequest.Operations[1];
            req2.Request.Key.Should().Be("stateKey2");
            req2.OperationType.Should().Be(StateOperationType.Delete.ToString().ToLower());
            var valueJson2 = req2.Request.Value.ToStringUtf8();
            var value2 = JsonSerializer.Deserialize<int>(valueJson2);
            value2.Should().Be(100);

            var req3 = transactionRequest.Operations[2];
            req3.Request.Key.Should().Be("stateKey3");
            req3.OperationType.Should().Be(StateOperationType.Upsert.ToString().ToLower());
            var valueJson3 = req3.Request.Value.ToStringUtf8();
            var value3 = JsonSerializer.Deserialize<string>(valueJson3);
            value3.Should().Be("teststring");
        }

        [Fact]
        public async Task ExecuteStateTransactionAsync_ThrowsForNonSuccess()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var widget1 = new Widget() { Size = "small", Color = "yellow", };
            var state1 = new StateTransactionRequest("stateKey1", JsonSerializer.SerializeToUtf8Bytes(widget1), StateOperationType.Upsert);
            var states = new List<StateTransactionRequest>
            {
                state1
            };
            var task = daprClient.ExecuteStateTransactionAsync("testStore", states);

            // Create Response & Respond
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var response = GrpcUtils.CreateResponse(HttpStatusCode.NotAcceptable);
            entry.Completion.SetResult(response);

            await FluentActions.Awaiting(async () => await task).Should().ThrowAsync<RpcException>();
        }

        [Fact]
        public async Task ExecuteStateTransactionAsync_WithCancelledToken()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient, ThrowOperationCanceledOnCancellation = true })
                .Build();

            var ctSource = new CancellationTokenSource();
            CancellationToken ct = ctSource.Token;
            ctSource.Cancel();

            var operation = new StateTransactionRequest("test", null, StateOperationType.Delete);
            var operations = new List<StateTransactionRequest>
            {
                operation
            };
            await FluentActions.Awaiting(async () => await daprClient.ExecuteStateTransactionAsync("testStore", operations, new Dictionary<string, string>(), cancellationToken: ct))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task DeleteStateAsync_CanDeleteState()
        {
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.DeleteStateAsync("testStore", "test");

            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.DeleteStateRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.Key.Should().Be("test");
        }

        [Fact]
        public async Task DeleteStateAsync_ThrowsForNonSuccess()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.DeleteStateAsync("testStore", "test");

            // Create Response & Respond
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var response = GrpcUtils.CreateResponse(HttpStatusCode.NotAcceptable);
            entry.Completion.SetResult(response);

            await FluentActions.Awaiting(async () => await task).Should().ThrowAsync<RpcException>();
        }

        [Fact]
        public async Task DeleteStateAsync_WithCancelledToken()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient, ThrowOperationCanceledOnCancellation = true })
                .Build();

            var ctSource = new CancellationTokenSource();
            CancellationToken ct = ctSource.Token;
            ctSource.Cancel();

            await FluentActions.Awaiting(async () => await daprClient.DeleteStateAsync("testStore", "key", cancellationToken: ct))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GetStateEntryAsync_CanReadState()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.GetStateEntryAsync<Widget>("testStore", "test");

            // Create Response & Respond
            var data = new Widget() { Size = "small", Color = "yellow", };
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            await SendResponseWithState(data, entry);

            // Get response and validate
            var state = await task;
            state.Value.Size.Should().Be("small");
            state.Value.Color.Should().Be("yellow");
        }

        [Fact]
        public async Task GetStateEntryAsync_CanReadEmptyState_ReturnsDefault()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.GetStateEntryAsync<Widget>("testStore", "test");

            // Create Response & Respond
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            await SendResponseWithState<Widget>(null, entry);

            var state = await task;
            state.Key.Should().Be("test");
            state.Value.Should().BeNull();
        }

        [Fact]
        public async Task GetStateEntryAsync_CanSaveState()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.GetStateEntryAsync<Widget>("testStore", "test");

            // Create Response & Respond
            var data = new Widget() { Size = "small", Color = "yellow", };
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            await SendResponseWithState(data, entry);

            var state = await task;
            state.Key.Should().Be("test");
            state.Value.Size.Should().Be("small");
            state.Value.Color.Should().Be("yellow");

            // Modify the state and save it
            state.Value.Color = "green";
            var task2 = state.SaveAsync();

            // Get Request and validate
            httpClient.Requests.TryDequeue(out entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.SaveStateRequest>(entry.Request);

            request.StoreName.Should().Be("testStore");
            request.States.Count.Should().Be(1);
            var requestState = request.States[0];
            requestState.Key.Should().Be("test");

            var stateJson = requestState.Value.ToStringUtf8();
            var stateFromRequest = JsonSerializer.Deserialize<Widget>(stateJson);
            stateFromRequest.Size.Should().Be("small");
            stateFromRequest.Color.Should().Be("green");
        }

        [Fact]
        public async Task GetStateEntryAsync_CanDeleteState()
        {
            // Configure client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var task = daprClient.GetStateEntryAsync<Widget>("testStore", "test");

            // Create Response & Respond
            var data = new Widget() { Size = "small", Color = "yellow", };
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            await SendResponseWithState(data, entry);

            var state = await task;
            state.Key.Should().Be("test");
            state.Value.Size.Should().Be("small");
            state.Value.Color.Should().Be("yellow");

            state.Value.Color = "green";
            var task2 = state.DeleteAsync();

            // Get Request and validate
            httpClient.Requests.TryDequeue(out entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.DeleteStateRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.Key.Should().Be("test");
        }

        [Theory]
        [InlineData(ConsistencyMode.Eventual, ConcurrencyMode.FirstWrite, StateConsistency.ConsistencyEventual, StateConcurrency.ConcurrencyFirstWrite)]
        [InlineData(ConsistencyMode.Eventual, ConcurrencyMode.LastWrite, StateConsistency.ConsistencyEventual, StateConcurrency.ConcurrencyLastWrite)]
        [InlineData(ConsistencyMode.Strong, ConcurrencyMode.FirstWrite, StateConsistency.ConsistencyStrong, StateConcurrency.ConcurrencyFirstWrite)]
        [InlineData(ConsistencyMode.Strong, ConcurrencyMode.LastWrite, StateConsistency.ConsistencyStrong, StateConcurrency.ConcurrencyLastWrite)]
        public async Task SaveStateAsync_ValidateOptions(
            ConsistencyMode consistencyMode,
            ConcurrencyMode concurrencyMode,
            StateConsistency expectedConsistency,
            StateConcurrency expectedConcurrency)
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var widget = new Widget() { Size = "small", Color = "yellow", };
            var stateOptions = new StateOptions
            {
                Concurrency = concurrencyMode,
                Consistency = consistencyMode
            };

            var metadata = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var task = daprClient.SaveStateAsync("testStore", "test", widget, stateOptions, metadata);

            // Get Request and validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.SaveStateRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.States.Count.Should().Be(1);
            var state = request.States[0];
            state.Key.Should().Be("test");
            state.Metadata.Count.Should().Be(2);
            state.Metadata.Keys.Contains("key1").Should().BeTrue();
            state.Metadata.Keys.Contains("key2").Should().BeTrue();
            state.Metadata["key1"].Should().Be("value1");
            state.Metadata["key2"].Should().Be("value2");
            state.Options.Concurrency.Should().Be(expectedConcurrency);
            state.Options.Consistency.Should().Be(expectedConsistency);

            var stateJson = state.Value.ToStringUtf8();
            var stateFromRequest = JsonSerializer.Deserialize<Widget>(stateJson);
            stateFromRequest.Size.Should().Be(widget.Size);
            stateFromRequest.Color.Should().Be(widget.Color);
        }

        [Theory]
        [InlineData(ConsistencyMode.Eventual, ConcurrencyMode.FirstWrite, StateConsistency.ConsistencyEventual, StateConcurrency.ConcurrencyFirstWrite)]
        [InlineData(ConsistencyMode.Eventual, ConcurrencyMode.LastWrite, StateConsistency.ConsistencyEventual, StateConcurrency.ConcurrencyLastWrite)]
        [InlineData(ConsistencyMode.Strong, ConcurrencyMode.FirstWrite, StateConsistency.ConsistencyStrong, StateConcurrency.ConcurrencyFirstWrite)]
        [InlineData(ConsistencyMode.Strong, ConcurrencyMode.LastWrite, StateConsistency.ConsistencyStrong, StateConcurrency.ConcurrencyLastWrite)]
        public async Task TrySaveStateAsync_ValidateOptions(
            ConsistencyMode consistencyMode,
            ConcurrencyMode concurrencyMode,
            StateConsistency expectedConsistency,
            StateConcurrency expectedConcurrency)
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var widget = new Widget() { Size = "small", Color = "yellow", };
            var stateOptions = new StateOptions
            {
                Concurrency = concurrencyMode,
                Consistency = consistencyMode
            };

            var metadata = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var task = daprClient.TrySaveStateAsync("testStore", "test", widget, "Test_Etag", stateOptions, metadata);

            // Get Request and validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.SaveStateRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.States.Count.Should().Be(1);
            var state = request.States[0];
            state.Key.Should().Be("test");
            state.Etag.Should().Be("Test_Etag");
            state.Metadata.Count.Should().Be(2);
            state.Metadata.Keys.Contains("key1").Should().BeTrue();
            state.Metadata.Keys.Contains("key2").Should().BeTrue();
            state.Metadata["key1"].Should().Be("value1");
            state.Metadata["key2"].Should().Be("value2");
            state.Options.Concurrency.Should().Be(expectedConcurrency);
            state.Options.Consistency.Should().Be(expectedConsistency);

            var stateJson = state.Value.ToStringUtf8();
            var stateFromRequest = JsonSerializer.Deserialize<Widget>(stateJson);
            stateFromRequest.Size.Should().Be(widget.Size);
            stateFromRequest.Color.Should().Be(widget.Color);
        }

        [Theory]
        [InlineData(ConsistencyMode.Eventual, ConcurrencyMode.FirstWrite, StateConsistency.ConsistencyEventual, StateConcurrency.ConcurrencyFirstWrite)]
        [InlineData(ConsistencyMode.Eventual, ConcurrencyMode.LastWrite, StateConsistency.ConsistencyEventual, StateConcurrency.ConcurrencyLastWrite)]
        [InlineData(ConsistencyMode.Strong, ConcurrencyMode.FirstWrite, StateConsistency.ConsistencyStrong, StateConcurrency.ConcurrencyFirstWrite)]
        [InlineData(ConsistencyMode.Strong, ConcurrencyMode.LastWrite, StateConsistency.ConsistencyStrong, StateConcurrency.ConcurrencyLastWrite)]
        public async Task DeleteStateAsync_ValidateOptions(
            ConsistencyMode consistencyMode,
            ConcurrencyMode concurrencyMode,
            StateConsistency expectedConsistency,
            StateConcurrency expectedConcurrency)
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var stateOptions = new StateOptions
            {
                Concurrency = concurrencyMode,
                Consistency = consistencyMode
            };

            var task = daprClient.DeleteStateAsync("testStore", "test", stateOptions);

            // Get Request and validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.DeleteStateRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.Key.Should().Be("test");
            request.Options.Concurrency.Should().Be(expectedConcurrency);
            request.Options.Consistency.Should().Be(expectedConsistency);
        }

        [Theory]
        [InlineData(ConsistencyMode.Eventual, ConcurrencyMode.FirstWrite, StateConsistency.ConsistencyEventual, StateConcurrency.ConcurrencyFirstWrite)]
        [InlineData(ConsistencyMode.Eventual, ConcurrencyMode.LastWrite, StateConsistency.ConsistencyEventual, StateConcurrency.ConcurrencyLastWrite)]
        [InlineData(ConsistencyMode.Strong, ConcurrencyMode.FirstWrite, StateConsistency.ConsistencyStrong, StateConcurrency.ConcurrencyFirstWrite)]
        [InlineData(ConsistencyMode.Strong, ConcurrencyMode.LastWrite, StateConsistency.ConsistencyStrong, StateConcurrency.ConcurrencyLastWrite)]
        public async Task TryDeleteStateAsync_ValidateOptions(
            ConsistencyMode consistencyMode,
            ConcurrencyMode concurrencyMode,
            StateConsistency expectedConsistency,
            StateConcurrency expectedConcurrency)
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var stateOptions = new StateOptions
            {
                Concurrency = concurrencyMode,
                Consistency = consistencyMode
            };

            var task = daprClient.TryDeleteStateAsync("testStore", "test", "Test_Etag", stateOptions);

            // Get Request and validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.DeleteStateRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.Key.Should().Be("test");
            request.Etag.Should().Be("Test_Etag");
            request.Options.Concurrency.Should().Be(expectedConcurrency);
            request.Options.Consistency.Should().Be(expectedConsistency);
        }

        private async Task SendResponseWithState<T>(T state, TestHttpClient.Entry entry, string etag = null)
        {
            var stateDate = TypeConverters.ToJsonByteString(state);
            var stateResponse = new Autogenerated.GetStateResponse
            {
                Data = stateDate
            };

            if (etag != null)
            {
                stateResponse.Etag = etag;
            }

            var streamContent = await GrpcUtils.CreateResponseContent(stateResponse);
            var response = GrpcUtils.CreateResponse(HttpStatusCode.OK, streamContent);
            entry.Completion.SetResult(response);
        }

        private async Task SendResponseWithBulkState(string key, string state, TestHttpClient.Entry entry)
        {
            var stateDate = TypeConverters.ToJsonByteString(state);
            var bulkResponse = new Autogenerated.GetBulkStateResponse();
            bulkResponse.Items.Add(new Autogenerated.BulkStateItem()
            {
                Key = key,
                Data = ByteString.CopyFromUtf8(state)
            });

            var streamContent = await GrpcUtils.CreateResponseContent(bulkResponse);
            var response = GrpcUtils.CreateResponse(HttpStatusCode.OK, streamContent);
            entry.Completion.SetResult(response);
        }

        private class Widget
        {
            public string Size { get; set; }

            public string Color { get; set; }
        }
    }
}
