// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Microsoft.Testing.Platform.ServerMode.Client;

namespace Microsoft.Testing.Platform.ServerMode.Client.Sources.UnitTests;

[TestClass]
public sealed class MtpServerProcessTests
{
    [TestMethod]
    public async Task WaitForExitAsync_ProcessExits_ReturnsTrue()
    {
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "--info",
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;

        bool exited = await MtpServerProcess.WaitForExitAsync(
            process,
            TimeSpan.FromSeconds(30),
            TestContext.CancellationToken).ConfigureAwait(false);

        Assert.IsTrue(exited);
    }

    [TestMethod]
    public async Task WaitForExitAsync_ProcessDoesNotExitBeforeTimeout_ReturnsFalse()
    {
        using var process = Process.GetCurrentProcess();

        bool exited = await MtpServerProcess.WaitForExitAsync(
            process,
            TimeSpan.FromMilliseconds(20),
            TestContext.CancellationToken).ConfigureAwait(false);

        Assert.IsFalse(exited);
    }

    [TestMethod]
    public async Task WaitForExitAsync_CancellationRequested_Throws()
    {
        using var process = Process.GetCurrentProcess();
        using var cancellation = new CancellationTokenSource();
#pragma warning disable VSTHRD103 // Call async methods when in an async method - CancelAsync() is .NET 8+ only and this test also targets net462.
        cancellation.Cancel();
#pragma warning restore VSTHRD103

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => MtpServerProcess.WaitForExitAsync(process, TimeSpan.FromSeconds(30), cancellation.Token)).ConfigureAwait(false);
    }

    public TestContext TestContext { get; set; } = null!;
}
