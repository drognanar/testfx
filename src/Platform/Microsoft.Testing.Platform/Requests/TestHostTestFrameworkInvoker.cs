﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Microsoft.Testing.Platform.Capabilities;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.Resources;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.TestHost;

namespace Microsoft.Testing.Platform.Requests;

internal class TestHostTestFrameworkInvoker(IServiceProvider serviceProvider) : ITestFrameworkInvoker, IOutputDeviceDataProducer, IDataProducer
{
    protected IServiceProvider ServiceProvider { get; } = serviceProvider;

    public string Uid => nameof(TestHostTestFrameworkInvoker);

    public string Version => AppVersion.DefaultSemVer;

    public string DisplayName => string.Empty;

    public string Description => string.Empty;

    public Type[] DataTypesProduced => new[] { typeof(TestRequestExecutionTimeInfo) };

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public async Task ExecuteAsync(ITestFramework testFrameworkAdapter, ClientInfo client, CancellationToken cancellationToken)
    {
        ILogger<TestHostTestFrameworkInvoker> logger = ServiceProvider.GetLoggerFactory().CreateLogger<TestHostTestFrameworkInvoker>();

        await logger.LogInformationAsync($"TestFrameworkAdapter Uid: '{testFrameworkAdapter.Uid}' Version: '{testFrameworkAdapter.Version}' DisplayName: '{testFrameworkAdapter.DisplayName}' Description: '{testFrameworkAdapter.Description}'");

        foreach (ICapability capability in ServiceProvider.GetTestFrameworkCapabilities().Capabilities)
        {
            if (capability is ITestNodesTreeFilterTestFrameworkCapability testNodesTreeFilterCapability)
            {
                await logger.LogInformationAsync($"ITestNodesTreeFilterCapability.IsSupported: {testNodesTreeFilterCapability.IsSupported}");
            }
        }

        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        SessionUid sessionId = ServiceProvider.GetTestSessionContext().SessionId;
        CreateTestSessionResult createTestSessionResult = await testFrameworkAdapter.CreateTestSessionAsync(new(sessionId, client, cancellationToken));
        await HandleTestSessionResultAsync(createTestSessionResult.IsSuccess, createTestSessionResult.WarningMessage, createTestSessionResult.ErrorMessage);

        ITestExecutionRequestFactory testExecutionRequestFactory = ServiceProvider.GetTestExecutionRequestFactory();
        TestExecutionRequest request = await testExecutionRequestFactory.CreateRequestAsync(new(sessionId, client));
        IMessageBus messageBus = ServiceProvider.GetMessageBus();

        // Execute the test request
        await ExecuteRequestAsync(testFrameworkAdapter, request, messageBus, cancellationToken);

        CloseTestSessionResult closeTestSessionResult = await testFrameworkAdapter.CloseTestSessionAsync(new(sessionId, client, cancellationToken));
        await HandleTestSessionResultAsync(closeTestSessionResult.IsSuccess, closeTestSessionResult.WarningMessage, closeTestSessionResult.ErrorMessage);
        DateTimeOffset endTime = DateTimeOffset.UtcNow;
        await messageBus.PublishAsync(this, new TestRequestExecutionTimeInfo(new TimingInfo(startTime, endTime, stopwatch.Elapsed)));
    }

    public virtual async Task ExecuteRequestAsync(ITestFramework testFrameworkAdapter, TestExecutionRequest request, IMessageBus messageBus, CancellationToken cancellationToken)
    {
        using SemaphoreSlim requestSemaphore = new(1);
        await testFrameworkAdapter.ExecuteRequestAsync(new(request, messageBus, requestSemaphore, cancellationToken));
        await requestSemaphore.WaitAsync(cancellationToken);
    }

    private async Task HandleTestSessionResultAsync(bool isSuccess, string? warningMessage, string? errorMessage)
    {
        if (warningMessage is not null)
        {
            IOutputDevice outputDisplay = ServiceProvider.GetOutputDevice();
            await outputDisplay.DisplayAsync(this, FormattedTextOutputDeviceDataHelper.CreateYellowConsoleColorText(warningMessage));
        }

        if (!isSuccess)
        {
            ITestApplicationProcessExitCode testApplicationProcessExitCode = ServiceProvider.GetTestApplicationProcessExitCode();
            await testApplicationProcessExitCode.SetTestAdapterTestSessionFailureAsync(errorMessage
                ?? PlatformResources.TestHostAdapterInvokerFailedTestSessionErrorMessage);
        }
    }
}
