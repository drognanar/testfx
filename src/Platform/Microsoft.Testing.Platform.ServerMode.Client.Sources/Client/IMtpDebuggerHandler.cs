// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Testing.Platform.ServerMode.Client;

internal static class MtpDebuggerMethods
{
    public const string Attach = "client/attachDebugger";
    public const string Launch = "client/launchDebugger";
}

/// <summary>
/// Handles debugger operations requested by an MTP server.
/// </summary>
internal interface IMtpDebuggerHandler
{
    /// <summary>
    /// Launches a process under the debugger and returns its process id.
    /// </summary>
    Task<int> LaunchAsync(MtpProcessStartInfo startInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Attaches the debugger to an existing process.
    /// </summary>
    Task AttachAsync(int processId, CancellationToken cancellationToken);
}

/// <summary>
/// Describes a process the MTP server asks the debugger provider to launch.
/// </summary>
internal sealed class MtpProcessStartInfo
{
    public MtpProcessStartInfo(
        string program,
        string? arguments,
        string? workingDirectory,
        IReadOnlyDictionary<string, string?> environmentVariables)
    {
        Program = program;
        Arguments = arguments;
        WorkingDirectory = workingDirectory;
        EnvironmentVariables = environmentVariables;
    }

    /// <summary>Gets the executable or managed program to launch.</summary>
    public string Program { get; }

    /// <summary>Gets the command-line arguments.</summary>
    public string? Arguments { get; }

    /// <summary>Gets the working directory.</summary>
    public string? WorkingDirectory { get; }

    /// <summary>Gets the additional environment variables.</summary>
    public IReadOnlyDictionary<string, string?> EnvironmentVariables { get; }
}
