# Dependency purposes and consumer impact

This is a source-level catalogue of the product projects under `src/`, their
external package references, and the packages injected by `MSTest.Sdk`. Use it
to answer **which feature introduces a dependency, what it does, and whether
it reaches consumers**. It is not a universal resolved dependency graph:
versions, target frameworks, RIDs, SDK profiles and optional features change
the closure. The exact package's nuspec and a representative consumer's
`project.assets.json` remain authoritative.

Baseline: source at `e3d67c4458315461201f6620374af1fbb9c4a722`, inspected on
2026-10-22. Published examples below use explicit package versions; they are
not predictions of the next release. Update the affected rows when changing
references, packing, profiles or dependency purposes.

## Reading the graph

- A `PackageVersion` in [Directory.Packages.props](../Directory.Packages.props)
  is a version definition, not evidence that every product depends on it.
  Item-group labels such as "Test dependencies" are not a shipping boundary:
  `Microsoft.Extensions.FileSystemGlobbing` is used by the shipping Azure
  DevOps report extension despite that label.
- A `ProjectReference` is not necessarily a NuGet dependency. Some projects
  are bundled into another package; some are generators or source-only
  helpers. Check custom pack targets, copied assemblies, native payloads and
  generated SDK references as well as the final nuspec.
- `PrivateAssets="all"` can suppress propagation to a downstream package
  consumer. It does not remove the reference from the producing project's
  restore, prove the DLL is absent from the nupkg, or isolate assemblies at
  runtime.
- A runtime opt-out (for example disabling telemetry) does not remove a
  package reference. Removing an optional feature's reference changes its
  functionality; it is not a transparent version-compatibility fix.
- A library's purpose and its **minimum required version** are separate
  claims. An API call establishes use, not that the newest major is required.

## Entry points

| Consumer choice | Functionality and dependency boundary |
|---|---|
| `MSTest.TestFramework` | Assertions, test attributes and framework extensions. The package bundles the core framework assembly and declares `MSTest.Analyzers`; it does not depend on MTP, Coverage or Microsoft.Extensions. |
| `MSTest.TestAdapter` | Test discovery/execution and host integration. Declares the framework, MTP MSBuild integration, telemetry, TRX abstractions and VSTest ObjectModel. Several implementation assemblies are bundled rather than exposed as package dependencies. |
| `MSTest` | Convenience meta-package: adapter, framework, TRX reporting, `Microsoft.NET.Test.Sdk` except for the classic UWP group, and `Microsoft.Testing.Extensions.CodeCoverage`. Coverage's closure therefore affects users even if they never collect coverage. |
| `MSTest.Sdk` | MSBuild project SDK. Its generated props and imported targets inject framework/adapter/runner packages and selected extensions into the consumer. Its own nuspec is not the injected graph. |
| `Microsoft.Testing.Platform` | BCL-only host, command line, execution, logging/configuration abstractions, IPC and extension model. No external package dependencies; the build enforces this boundary. |
| Explicit extensions | Each reference adds that extension's graph. Using the core host does not imply using every extension. |

Source entry points: [framework packaging](../src/TestFramework/TestFramework.Extensions/TestFramework.Extensions.csproj),
[adapter](../src/Adapter/MSTest.TestAdapter/MSTest.TestAdapter.csproj),
[meta-package](../src/Package/MSTest/MSTest.csproj),
[SDK](../src/Package/MSTest.Sdk/MSTest.Sdk.csproj), and
[MTP core](../src/Platform/Microsoft.Testing.Platform/Microsoft.Testing.Platform.csproj).

### SDK activation

Read the actual selected [SDK targets](../src/Package/MSTest.Sdk/Sdk/Sdk.targets),
not just the SDK's package dependencies.

| Selection | Additional graph to inspect |
|---|---|
| Classic MTP runner | Framework, adapter and platform; each enabled extension in [ClassicEngine.targets](../src/Package/MSTest.Sdk/Sdk/Runner/ClassicEngine.targets). |
| `TestingExtensionsProfile=Default` | [Common.targets](../src/Package/MSTest.Sdk/Sdk/Runner/Common.targets) enables Coverage and TRX at this source baseline. |
| `TestingExtensionsProfile=AllMicrosoft` | Default extensions plus crash/hang dumps, retry, hot reload, Fakes, HTML, Azure DevOps and GitHub Actions reports in ClassicEngine.targets. This is a selected set, not literally every package in the repository. |
| `TestingExtensionsProfile=None` | No profile-selected extensions. Explicitly enabled extensions, adapter dependencies and application-model requirements still apply. |
| `EnableMicrosoftTestingExtensions...` | Individually adds/removes the named supported extension reference; inspect the condition and selected runner. |
| `UseVSTest=true` | [VSTest.targets](../src/Package/MSTest.Sdk/Sdk/VSTest/VSTest.targets) adds framework, adapter and Microsoft.NET.Test.Sdk. |
| Native AOT | [NativeAOT.targets](../src/Package/MSTest.Sdk/Sdk/Runner/NativeAOT.targets) adds the source generator and MTP MSBuild integration as well as the adapter/framework; Coverage with `PublishAot=true` also adds Microsoft.CodeCoverage.MSBuild. |
| Packaged WinUI/UWP application | Common.targets conditionally adds Microsoft.Testing.Extensions.PackagedApp.MSBuild and its controller payload. |
| `EnableAspireTesting=true` | [Aspire.targets](../src/Package/MSTest.Sdk/Sdk/Features/Aspire.targets) injects Aspire.Hosting.Testing. Its upstream hosting/testing graph is not a core MSTest requirement. |
| `EnablePlaywright=true` | [Playwright.targets](../src/Package/MSTest.Sdk/Sdk/Features/Playwright.targets) injects Microsoft.Playwright.MSTest.v4 for browser testing. |
| `EnableWindowsUIAutomation=true` | [WindowsUIAutomation.targets](../src/Package/MSTest.Sdk/Sdk/Features/WindowsUIAutomation.targets) injects MSTest.Windows.UIAutomation for desktop UI testing. |

[Learn's SDK guide](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-sdk)
lists Azure DevOps and GitHub reports in Default from 4.3.0, whereas the
inspected targets enable those automatically only for AllMicrosoft. Verify
the exact shipped SDK and evaluated project before assigning that graph to a
Default consumer.

## Platform extensions: what adds what

Every extension below references MTP directly or through another listed
extension. Paths link to the owning project's references and conditions.
"No additional external reference" does not mean no MTP dependency or no
bundled implementation.

| Component | Functionality | Additional references or payload |
|---|---|---|
| [Configuration](../src/Platform/Microsoft.Testing.Extensions.Configuration/Microsoft.Testing.Extensions.Configuration.csproj) | Import an externally owned configuration into MTP. | Microsoft.Extensions.Configuration.Abstractions: `IConfiguration` interop, not a dependency on every configuration provider. |
| [Logging](../src/Platform/Microsoft.Testing.Extensions.Logging/Microsoft.Testing.Extensions.Logging.csproj) | Bridge MTP logging to Microsoft.Extensions.Logging. | Microsoft.Extensions.Logging: logging factory/provider integration. This package's graph can include logging, options and DI abstractions. |
| [Hosting](../src/Platform/Microsoft.Testing.Extensions.Hosting/Microsoft.Testing.Extensions.Hosting.csproj) | Integrate MTP with an externally owned generic host. | Microsoft.Extensions.Hosting.Abstractions plus the Configuration and Logging extensions. The concrete Hosting package's repository version is not this abstraction's minimum. |
| [Telemetry](../src/Platform/Microsoft.Testing.Extensions.Telemetry/Microsoft.Testing.Extensions.Telemetry.csproj) | Send test-platform telemetry. | Microsoft.ApplicationInsights and an explicit System.Diagnostics.DiagnosticSource reference. The source comment cites the older transitive 5.0.0 baseline being unmaintained; that records the original rationale, not proof of the current pin's support status. Published 2.5.1 metadata exposes the explicit DiagnosticSource floor in the netstandard2.0 group. |
| [OpenTelemetry](../src/Platform/Microsoft.Testing.Extensions.OpenTelemetry/Microsoft.Testing.Extensions.OpenTelemetry.csproj) | Export test telemetry through OpenTelemetry. | OpenTelemetry and OpenTelemetry.Exporter.OpenTelemetryProtocol. Their diagnostics, logging, networking and serialization closure is upstream-owned. Distinct from the Application Insights extension. |
| [AzureDevOpsReport](../src/Platform/Microsoft.Testing.Extensions.AzureDevOpsReport/Microsoft.Testing.Extensions.AzureDevOpsReport.csproj) | Publish test/artifact information with Azure Pipelines commands. | Microsoft.Extensions.FileSystemGlobbing for artifact path patterns; System.Text.Json on netstandard2.0 for serialization. |
| [GitHubActionsReport](../src/Platform/Microsoft.Testing.Extensions.GitHubActionsReport/Microsoft.Testing.Extensions.GitHubActionsReport.csproj) | GitHub workflow groups, annotations and summaries. | System.Text.Json on netstandard2.0. |
| [CtrfReport](../src/Platform/Microsoft.Testing.Extensions.CtrfReport/Microsoft.Testing.Extensions.CtrfReport.csproj) | Serialize results in CTRF format. | Unconditional System.Text.Json source reference; inspect the final per-TFM package groups rather than assuming framework-provided JSON removes the declared requirement. |
| [JUnitReport](../src/Platform/Microsoft.Testing.Extensions.JUnitReport/Microsoft.Testing.Extensions.JUnitReport.csproj) | Produce JUnit XML reports. | Unconditional System.Text.Json source reference for the report journal; do not infer JSON is the final report format from the package reference. |
| [HtmlReport](../src/Platform/Microsoft.Testing.Extensions.HtmlReport/Microsoft.Testing.Extensions.HtmlReport.csproj) | Produce a browser-readable report. | Unconditional System.Text.Json source reference for report data/journal serialization; inspect final per-TFM groups. |
| [HangDump](../src/Platform/Microsoft.Testing.Extensions.HangDump/Microsoft.Testing.Extensions.HangDump.csproj) | Capture dumps when execution hangs. | Microsoft.Diagnostics.NETCore.Client for .NET diagnostic dump requests; platform-specific dump implementation also exists. |
| [CrashDump](../src/Platform/Microsoft.Testing.Extensions.CrashDump/Microsoft.Testing.Extensions.CrashDump.csproj) | Configure crash dump collection/reporting. | TrxReport.Abstractions, with no additional external reference. |
| [TrxReport](../src/Platform/Microsoft.Testing.Extensions.TrxReport/Microsoft.Testing.Extensions.TrxReport.csproj) | Write TRX test results. | TrxReport.Abstractions, with no additional external reference. |
| [TrxReport.Abstractions](../src/Platform/Microsoft.Testing.Extensions.TrxReport.Abstractions/Microsoft.Testing.Extensions.TrxReport.Abstractions.csproj) | Shared result/reporting contracts. | No additional external reference. |
| [Retry](../src/Platform/Microsoft.Testing.Extensions.Retry/Microsoft.Testing.Extensions.Retry.csproj) | Retry failed tests. | No additional external reference. |
| [HotReload](../src/Platform/Microsoft.Testing.Extensions.HotReload/Microsoft.Testing.Extensions.HotReload.csproj) | Coordinate test reruns with hot reload. | No additional external reference. |
| [MSBuild](../src/Platform/Microsoft.Testing.Extensions.MSBuild/Microsoft.Testing.Extensions.MSBuild.csproj) | Runtime side of MSBuild test orchestration. | No additional external reference. Distinct from the task package below. |
| [VSTestBridge](../src/Platform/Microsoft.Testing.Extensions.VSTestBridge/Microsoft.Testing.Extensions.VSTestBridge.csproj) | Run VSTest adapters through MTP. | Microsoft.TestPlatform.ObjectModel plus telemetry and TRX abstractions; Microsoft.TestPlatform.Filter.Source is private source/build input. Native MSTest MTP integration does not require this bridge at the inspected baseline. |
| [PackagedApp](../src/Platform/Microsoft.Testing.Extensions.PackagedApp/Microsoft.Testing.Extensions.PackagedApp.csproj) | Runtime support for packaged test applications. | No additional external reference. |
| [PackagedApp.Controller](../src/Platform/Microsoft.Testing.Extensions.PackagedApp.Controller/Microsoft.Testing.Extensions.PackagedApp.Controller.csproj) | Desktop-side activation/control and results for packaged applications. | MSBuild, PackagedApp, TRX, HangDump and Retry extensions; external CodeCoverage. Trace the controller's bundled payload even when its project reference is private. |
| [PackagedApp.MSBuild](../src/Platform/Microsoft.Testing.Extensions.PackagedApp.MSBuild/Microsoft.Testing.Extensions.PackagedApp.MSBuild.csproj) | Build integration and delivery of the packaged-app controller. | Platform.MSBuild and PackagedApp dependencies; private controller project supplying payload. |
| [VideoRecorder](../src/Platform/Microsoft.Testing.Extensions.VideoRecorder/Microsoft.Testing.Extensions.VideoRecorder.csproj) | Video-recording extension. | No additional external reference at this baseline; do not assume an unmerged/native recorder implementation or a stable release. |
| [Platform.AI](../src/Platform/Microsoft.Testing.Platform.AI/Microsoft.Testing.Platform.AI.csproj) | AI service integration contracts. | Microsoft.Extensions.AI. |
| [AzureFoundry](../src/Platform/Microsoft.Testing.Extensions.AzureFoundry/Microsoft.Testing.Extensions.AzureFoundry.csproj) | Provide an Azure/OpenAI-backed AI client. | Platform.AI, Azure.AI.OpenAI, Azure.Core and Microsoft.Extensions.AI.OpenAI for client, credential and chat abstractions/adapters. |
| [Platform.MSBuild](../src/Platform/Microsoft.Testing.Platform.MSBuild/Microsoft.Testing.Platform.MSBuild.csproj) | MSBuild tasks and build assets to discover/run test applications. | MTP and bundled runtime MSBuild extension; Microsoft.Build.Framework, Microsoft.Build.Utilities.Core and conditional Microsoft.Win32.Registry are private build/task references. Inspect `_MSBuildTasks` and copied payload, not just runtime dependencies. |
| [ServerMode.Client.Sources](../src/Platform/Microsoft.Testing.Platform.ServerMode.Client.Sources/Microsoft.Testing.Platform.ServerMode.Client.Sources.csproj) | Source-injected JSON-RPC client. | Source-only package with dependencies suppressed when packing; no runtime package dependency. |

Extensions' in-repository MTP references also acquire the next-major upper
bound through [Platform build targets](../src/Platform/Directory.Build.targets).
A changed upper bound matters as well as a raised minimum.

## Framework, adapter, analyzer and SDK build inputs

| Owning component | External package(s) | Why / propagation |
|---|---|---|
| Adapter and platform services | System.Memory; System.Threading.Tasks.Extensions | Memory/span and async compatibility on the older target groups; conditions differ between the two projects. |
| Adapter | Microsoft.TestPlatform.ObjectModel | VSTest test cases, results and host contracts; a declared consumer dependency. |
| Adapter / VSTestBridge | Microsoft.TestPlatform.Filter.Source | Shared filter implementation compiled into the product; private source/build dependency, not a consumer runtime package requirement. |
| [MSTest.Extensions.Hosting](../src/Adapter/MSTest.Extensions.Hosting/MSTest.Extensions.Hosting.csproj) | Microsoft.Extensions.DependencyInjection.Abstractions | Test-class service injection; also pulls framework, adapter, analyzers and MTP Hosting interop. |
| Framework Windows asset | Microsoft.WindowsAppSDK | Compile WinUI-specific framework integration. Private reference; inspect target-specific packaging before inferring runtime distribution. |
| [MSTest.Windows.UIAutomation](../src/TestFramework/TestFramework.Windows.UIAutomation/TestFramework.Windows.UIAutomation.csproj) | No direct external reference | Desktop automation atop the framework; SDK opt-in. |
| [MSTest.Analyzers](../src/Analyzers/MSTest.Analyzers.Package/MSTest.Analyzers.Package.csproj) package and its analyzer/code-fix projects | Microsoft.CodeAnalysis.Common; Microsoft.CodeAnalysis.CSharp.Workspaces; Microsoft.CodeAnalysis.Analyzers | Compiler analysis and code-fix APIs plus authoring diagnostics. Implementation projects are bundled as analyzers/code fixes; Roslyn host APIs are not ordinary test-runtime dependencies. |
| [MSTest.GlobalConfigsGenerator](../src/Analyzers/MSTest.GlobalConfigsGenerator/MSTest.GlobalConfigsGenerator.csproj) | Microsoft.CodeAnalysis | Build tool generating analyzer configuration. Not a test-runtime requirement. |
| [MSTest.SourceGeneration](../src/Analyzers/MSTest.SourceGeneration/MSTest.SourceGeneration.csproj) | Microsoft.CodeAnalysis.CSharp; Microsoft.CodeAnalysis.Analyzers | Compile-time test source generation; private references. Uses the source-generator-specific Roslyn version override. |
| [TestFramework.SourceGeneration](../src/TestFramework/TestFramework.SourceGeneration/TestFramework.SourceGeneration.csproj) | Microsoft.CodeAnalysis.CSharp; Microsoft.CodeAnalysis.Analyzers | Internal build-time generator for the core framework, not a separately published package. |
| [Core TestFramework](../src/TestFramework/TestFramework/TestFramework.csproj) | No direct external reference | Nonpackable implementation bundled by TestFramework.Extensions as MSTest.TestFramework, not a public package named after the project. |
| MSTest.Sdk build | Microsoft.DotNet.Build.Tasks.Templating | Replace SDK template versions when building the SDK; private build dependency, not an injected test dependency. Also imported for applicable builds by root targets. |
| Repository quality tooling | Microsoft.CodeAnalysis.BannedApiAnalyzers; Microsoft.CodeAnalysis.PublicApiAnalyzers; Microsoft.VisualStudio.Threading.Analyzers; StyleCop.Analyzers | Analyze this repository's code. Do not mistake an analyzer used to build the product for a dependency shipped to every test project. |

Test-only and development projects under `test/`, `samples/`, `tools/` and
`eng/` have their own references (assertion libraries, mocking, benchmarks,
alternate test frameworks, SDK tooling, etc.). They are outside this product
catalogue. Root [Directory.Build.targets](../Directory.Build.targets) also
conditionally injects Microsoft.Testing.Extensions.AffectedTests and
Microsoft.Testing.Extensions.AffectedTests.Storage.AzureDevOps into this
repository's test applications. Those conditions do not make them universal
MSTest consumer dependencies. Evaluate imports before classifying any new
reference as test-only or shipping.

## Upstream tooling and its transitive dependencies

These packages are not implemented here. Their exact versioned dependency
groups must be inspected separately; the table explains the function, not a
guarantee that their entire closure is required by every MSTest project.

| Package | Functionality / dependencies to follow |
|---|---|
| Microsoft.NET.Test.Sdk | VSTest test-project build/host integration. Follow Microsoft.TestPlatform.TestHost and Microsoft.CodeCoverage for the selected TFM/version. |
| Microsoft.Testing.Extensions.CodeCoverage | MTP coverage collection/instrumentation. Follow DependencyModel, diagnostics/symbol readers, JSON, IO.Pipelines and instrumentation dependencies from its versioned nuspec. |
| Microsoft.CodeCoverage.MSBuild | Build-time instrumentation for Native AOT coverage; SDK-injected only for the relevant AOT/Coverage selection. |
| Microsoft.Testing.Extensions.Fakes | Fakes integration selected by the SDK; independently versioned, with separate runtime/tooling requirements. |
| Aspire.Hosting.Testing | Aspire application-host testing; brings its own hosting/distributed-application closure. |
| Microsoft.Playwright.MSTest.v4 | Browser-testing integration with Playwright and MSTest; brings the Playwright client/tooling closure. |

### Why would coverage need DependencyModel?

Coverage instrumentation can need to locate assemblies referenced by the
assembly being instrumented. The
[DependencyModel APIs](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencymodel)
describe compilation/runtime libraries and read dependency manifests;
[its resolution APIs](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencymodel.resolution)
locate reference assemblies and package assets. This is assembly/dependency
resolution functionality, not the generic-host or DI container.

Keep three evidence levels separate:

1. **Declared dependency:** Coverage's published package metadata proves that
   consumers must satisfy its DependencyModel minimum.
2. **Functional purpose:** dependency-manifest reading and assembly resolution
   explain what this library can provide to an instrumenter. Attribute the
   exact call site to an accessible upstream source or an owner-confirmed
   explanation; an analogous instrumenter or a package name is not proof of
   the implementation in a particular Coverage release.
3. **Version necessity:** neither the package edge nor that functional purpose
   proves that DependencyModel 10.x is indispensable. Request the required
   API, bug fix, security/servicing baseline or owner-confirmed constraint.

For a concrete published example:

| MSTest | Included Coverage minimum | Coverage's DependencyModel minimum |
|---|---|---|
| [4.0.2](https://www.nuget.org/packages/MSTest/4.0.2) | [18.1.0](https://www.nuget.org/packages/Microsoft.Testing.Extensions.CodeCoverage/18.1.0) | 6.0.2 |
| [4.3.0](https://www.nuget.org/packages/MSTest/4.3.0) | [18.9.0](https://www.nuget.org/packages/Microsoft.Testing.Extensions.CodeCoverage/18.9.0) | 10.0.8 |
| [4.5.1](https://www.nuget.org/packages/MSTest/4.5.1) | [18.12.0](https://www.nuget.org/packages/Microsoft.Testing.Extensions.CodeCoverage/18.12.0) | 10.0.10 |

Those minimums occur in the checked net8.0 and netstandard2.0 Coverage groups.
Coverage 18.12.0 also declares System.Text.Json, System.Text.Encodings.Web and
System.IO.Pipelines >=10.0.10. Other target groups add compatibility libraries
such as Microsoft.Bcl.AsyncInterfaces, System.Buffers, System.Memory,
System.Runtime.CompilerServices.Unsafe and System.Threading.Tasks.Extensions.
The instrumenter also declares Microsoft.DiaSymReader, Mono.Cecil and
conditional System.Reflection.Metadata. These are symbol/assembly,
serialization, IO and compatibility responsibilities, not assertion APIs.
Their precise call sites and minimum-version reasons remain upstream-owned.

This is a consequential transitive-major shift inside an MSTest 4.x release
line, not proof that downgrading Coverage is safe. A lower direct pin cannot
erase an upstream declared minimum without a downgrade/conflict.

## Reviewing and maintaining the catalogue

Follow the [dependency-impact review contract](../.github/workflows/README.md#dependency-impact-review-contract).
For every affected package record the introducing feature, consuming project,
source/API use, shipping or bundled path, TFM/RID conditions, old/new minimum
and reason it must change. Preserve unknowns instead of inventing justifications.

For an exact consumer closure, retain the selected SDK/package versions,
profile, runner, TFM/RID, NuGet configuration and central pins, then compare
before/after assets or lock files. Inspect the final nuspec and SDK-generated
references for each affected shipping package. NuGet resolves a version per
project graph; cross-repository coordinated upgrades are a consumer constraint,
not a rule that every repository must always use one global version.

### Decision and validation record

The existing expert reviewer already assesses version changes. A README alone
would be passive; this change links the catalogue and the review contract from
the live expert-reviewer instructions and broadens assessment triggers to graph
changes without a version edit. Reuse the existing consolidated review output,
validators and severity mapping rather than adding a second notification bot,
recipient list, permission or workflow trigger.

The catalogue uses source references and versioned public metadata. It does
not publish private upstream implementation, promise a fully restored closure
for every configuration, or establish minimum-version necessity without
upstream evidence. No package versions or runtime behavior are changed.
Static guidance/link and routing checks can establish prompt wiring; they do
not prove that a future model will report every dependency regression.
