﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Framework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.TestInfrastructure;

using Moq;

namespace Microsoft.Testing.Platform.UnitTests;

[TestGroup]
public class ArgumentArityTests : TestBase
{
    private readonly Mock<IPlatformOutputDevice> _outputDisplayMock = new();
    private readonly Mock<ITestApplicationModuleInfo> _testApplicationModuleInfoMock = new();
    private readonly Mock<IRuntimeFeature> _runtimeFeatureMock = new();
    private readonly Mock<IEnvironment> _environmentMock = new();
    private readonly Mock<IProcessHandler> _processHandlerMock = new();
    private readonly ICommandLineOptionsProvider[] _systemCommandLineOptionsProviders = new[]
    {
        new PlatformCommandLineProvider(),
    };

    private readonly ICommandLineOptionsProvider[] _extensionCommandLineOptionsProviders = new[]
    {
        new ExtensionCommandLineProviderMockOptionsWithDifferentArity(),
    };

    public ArgumentArityTests(ITestExecutionContext testExecutionContext)
        : base(testExecutionContext)
    {
    }

    public async Task ParseAndValidate_WhenOptionWithArityZeroIsCalledWithOneArgument_ReturnsFalse()
    {
        // Arrange
        string[] args = ["--zeroArgumentsOption arg"];
        CommandLineParseResult parseResult = CommandLineParser.Parse(args, new SystemEnvironment());
        _outputDisplayMock.Setup(x => x.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>()))
        .Callback((IOutputDeviceDataProducer message, IOutputDeviceData data) =>
            Assert.AreEqual($"Option '--zeroArgumentsOption' from provider 'Microsoft Testing Platform command line provider' (UID: PlatformCommandLineProvider) expects no arguments{Environment.NewLine}", ((TextOutputDeviceData)data).Text, StringComparer.Ordinal));

        var commandLineHandler = new CommandLineHandler(args, parseResult,
            _extensionCommandLineOptionsProviders, _systemCommandLineOptionsProviders, _testApplicationModuleInfoMock.Object, _runtimeFeatureMock.Object, _outputDisplayMock.Object, _environmentMock.Object, _processHandlerMock.Object);

        // Act
        bool result = await commandLineHandler.ParseAndValidateAsync();

        // Assert
        Assert.IsFalse(result);
        _outputDisplayMock.Verify(o => o.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>()), Times.Once);
    }

    public async Task ParseAndValidate_WhenOptionWithArityExactlyOneIsCalledWithTwoArguments_ReturnsFalse()
    {
        // Arrange
        string[] args = ["--exactlyOneArgumentsOption arg1", "arg2"];
        CommandLineParseResult parseResult = CommandLineParser.Parse(args, new SystemEnvironment());
        _outputDisplayMock.Setup(x => x.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>()))
        .Callback((IOutputDeviceDataProducer message, IOutputDeviceData data) =>
            Assert.AreEqual($"Option '--exactlyOneArgumentsOption' from provider 'Microsoft Testing Platform command line provider' (UID: PlatformCommandLineProvider) expects at most 1 arguments{Environment.NewLine}", ((TextOutputDeviceData)data).Text, StringComparer.Ordinal));

        var commandLineHandler = new CommandLineHandler(args, parseResult,
            _extensionCommandLineOptionsProviders, _systemCommandLineOptionsProviders, _testApplicationModuleInfoMock.Object, _runtimeFeatureMock.Object, _outputDisplayMock.Object, _environmentMock.Object, _processHandlerMock.Object);

        // Act
        bool result = await commandLineHandler.ParseAndValidateAsync();

        // Assert
        Assert.IsFalse(result);
        _outputDisplayMock.Verify(o => o.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>()), Times.Once);
    }

    public async Task ParseAndValidate_WhenOptionWithArityExactlyOneIsCalledWithoutArguments_ReturnsFalse()
    {
        // Arrange
        string[] args = ["--exactlyOneArgumentsOption"];
        CommandLineParseResult parseResult = CommandLineParser.Parse(args, new SystemEnvironment());
        _outputDisplayMock.Setup(x => x.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>()))
        .Callback((IOutputDeviceDataProducer message, IOutputDeviceData data) =>
            Assert.AreEqual($"Option '--exactlyOneArgumentsOption' from provider 'Microsoft Testing Platform command line provider' (UID: PlatformCommandLineProvider) expects at least 1 arguments{Environment.NewLine}", ((TextOutputDeviceData)data).Text, StringComparer.Ordinal));

        var commandLineHandler = new CommandLineHandler(args, parseResult,
            _extensionCommandLineOptionsProviders, _systemCommandLineOptionsProviders, _testApplicationModuleInfoMock.Object, _runtimeFeatureMock.Object, _outputDisplayMock.Object, _environmentMock.Object, _processHandlerMock.Object);

        // Act
        bool result = await commandLineHandler.ParseAndValidateAsync();

        // Assert
        Assert.IsFalse(result);
        _outputDisplayMock.Verify(o => o.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>()), Times.Once);
    }

    public async Task ParseAndValidate_WhenOptionWithArityZeroOrOneIsCalledWithTwoArguments_ReturnsFalse()
    {
        // Arrange
        string[] args = ["--zeroOrOneArgumentsOption arg1", "--zeroOrOneArgumentsOption arg2"];
        CommandLineParseResult parseResult = CommandLineParser.Parse(args, new SystemEnvironment());
        _outputDisplayMock.Setup(x => x.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>()))
        .Callback((IOutputDeviceDataProducer message, IOutputDeviceData data) =>
            Assert.AreEqual($"Option '--zeroOrOneArgumentsOption' from provider 'Microsoft Testing Platform command line provider' (UID: PlatformCommandLineProvider) expects at most 1 arguments{Environment.NewLine}", ((TextOutputDeviceData)data).Text, StringComparer.Ordinal));

        var commandLineHandler = new CommandLineHandler(args, parseResult,
            _extensionCommandLineOptionsProviders, _systemCommandLineOptionsProviders, _testApplicationModuleInfoMock.Object, _runtimeFeatureMock.Object, _outputDisplayMock.Object, _environmentMock.Object, _processHandlerMock.Object);

        // Act
        bool result = await commandLineHandler.ParseAndValidateAsync();

        // Assert
        Assert.IsFalse(result);
        _outputDisplayMock.Verify(o => o.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>()), Times.Once);
    }

    public async Task ParseAndValidate_WhenOptionWithArityOneOrMoreIsCalledWithoutArguments_ReturnsFalse()
    {
        // Arrange
        string[] args = ["--oneOrMoreArgumentsOption"];
        CommandLineParseResult parseResult = CommandLineParser.Parse(args, new SystemEnvironment());
        _outputDisplayMock.Setup(x => x.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>()))
        .Callback((IOutputDeviceDataProducer message, IOutputDeviceData data) =>
            Assert.AreEqual($"Option '--oneOrMoreArgumentsOption' from provider 'Microsoft Testing Platform command line provider' (UID: PlatformCommandLineProvider) expects at least 1 arguments{Environment.NewLine}", ((TextOutputDeviceData)data).Text, StringComparer.Ordinal));

        var commandLineHandler = new CommandLineHandler(args, parseResult,
            _extensionCommandLineOptionsProviders, _systemCommandLineOptionsProviders, _testApplicationModuleInfoMock.Object, _runtimeFeatureMock.Object, _outputDisplayMock.Object, _environmentMock.Object, _processHandlerMock.Object);

        // Act
        bool result = await commandLineHandler.ParseAndValidateAsync();

        // Assert
        Assert.IsFalse(result);
        _outputDisplayMock.Verify(o => o.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>()), Times.Once);
    }

    public async Task ParseAndValidate_WhenOptionsGetsTheExpectedNumberOfArguments_ReturnsTrue()
    {
        // Arrange
        string[] args = ["--zeroArgumentsOption", "--zeroOrOneArgumentsOption", "--zeroOrMoreArgumentsOption arg2", "--exactlyOneArgumentsOption arg1", "oneOrMoreArgumentsOption arg1"];
        CommandLineParseResult parseResult = CommandLineParser.Parse(args, new SystemEnvironment());
        var commandLineHandler = new CommandLineHandler(args, parseResult,
            _extensionCommandLineOptionsProviders, _systemCommandLineOptionsProviders, _testApplicationModuleInfoMock.Object, _runtimeFeatureMock.Object, _outputDisplayMock.Object, _environmentMock.Object, _processHandlerMock.Object);

        // Act
        bool result = await commandLineHandler.ParseAndValidateAsync();

        // Assert
        Assert.IsTrue(result);
    }

    private sealed class ExtensionCommandLineProviderMockOptionsWithDifferentArity : ICommandLineOptionsProvider
    {
        public string Uid { get; } = nameof(PlatformCommandLineProvider);

        /// <inheritdoc />
        public string Version { get; } = AppVersion.DefaultSemVer;

        /// <inheritdoc />
        public string DisplayName { get; } = "Microsoft Testing Platform command line provider";

        /// <inheritdoc />
        public string Description { get; } = "Built-in command line provider";

        /// <inheritdoc />
        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public CommandLineOption[] GetCommandLineOptions()
            => new CommandLineOption[]
            {
                new("zeroArgumentsOption", "Show command line zeroArgumentsOption.", ArgumentArity.Zero, false),
                new("zeroOrOneArgumentsOption", "Show command line zeroOrOneArgumentsOption.", ArgumentArity.ZeroOrOne, false),
                new("zeroOrMoreArgumentsOption", "Show command line zeroOrMoreArgumentsOption.", ArgumentArity.ZeroOrMore, false),
                new("exactlyOneArgumentsOption", "Show command line exactlyOneArgumentsOption.", ArgumentArity.ExactlyOne, false),
                new("oneOrMoreArgumentsOption", "Show command line oneOrMoreArgumentsOption.", ArgumentArity.OneOrMore, false),
            };

        public bool IsValidConfiguration(ICommandLineOptions commandLineOptions, out string? errorMessage)
        {
            errorMessage = null;
            return true;
        }

        public bool OptionArgumentsAreValid(CommandLineOption commandOption, string[] arguments, out string? errorMessage)
        {
            errorMessage = null;
            return true;
        }
    }
}
