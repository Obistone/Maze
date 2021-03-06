using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Maze.ModuleManagement;
using Maze.ModuleManagement.Loader;
using Maze.Server.Connection.Modules;
using Maze.Server.Service.Modules;
using Maze.Server.Service.Modules.PackageManagement;
using Maze.Server.Service.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Maze.Server.Service.Tests.Modules
{
    //public class ModulePackageManagerTests
    //{
    //    private readonly ILogger _testLogger;

    //    public ModulePackageManagerTests(ITestOutputHelper output)
    //    {
    //        _testLogger = new TestOutputLogger(output);
    //    }

    //    [Fact]
    //    public async Task TestInstallPowerUserTools()
    //    {
    //        var sourceRepositoryProvider = TestSourceRepositoryUtility.CreateSourceRepositoryProvider(new[]
    //        {
    //            new PackageSource("http://localhost:40221/nuget"), 
    //            TestSourceRepositoryUtility.V3PackageSource,
    //            new PackageSource(new Uri(Path.GetFullPath("modules"), UriKind.Absolute).AbsoluteUri)
    //        });

    //        var sources = sourceRepositoryProvider.GetRepositories().ToList();

    //        var token = CancellationToken.None;
    //        var package = new PackageIdentity("PowerUserTools", new NuGetVersion(1, 0, 0));
    //        var testProject = new TestModuleProject
    //        {
    //            PrimarySources = sources.Take(1).ToImmutableList(),
    //            DependencySources = sources.Skip(1).Take(1).ToImmutableList(),
    //            LocalSourceRepository = sources.Skip(2).First()
    //        };
    //        var packageManager = new ModulePackageManager(testProject, new Dictionary<NuGetFramework, PackageIdentity>());
    //        var actions =
    //            await packageManager.PreviewInstallPackageAsync(package, new ResolutionContext(), _testLogger, token);

    //    }
    //}

    //public class TestModuleProject : IModuleProject
    //{
    //    public TestModuleProject()
    //    {
    //        PrimaryPackages = ImmutableList<PackageIdentity>.Empty;
    //        InstalledPackages = ImmutableDictionary<PackageIdentity, IImmutableList<PackageIdentity>>.Empty;
    //    }

    //    public NuGetFramework Framework { get; } = FrameworkConstants.CommonFrameworks.MazeServer10;
    //    public Runtime Runtime { get; }
    //    public Architecture Architecture { get; }
    //    public IImmutableList<PackageIdentity> PrimaryPackages { get; }
    //    public IImmutableDictionary<PackageIdentity, IImmutableList<PackageIdentity>> InstalledPackages { get; }
    //    public IImmutableList<SourceRepository> PrimarySources { get; set; }
    //    public IImmutableList<SourceRepository> DependencySources { get; set; }
    //    public SourceRepository LocalSourceRepository { get; set; }
    //    public IModulesDirectory ModulesDirectory { get; set; }

    //    public Task<bool> InstallPackageAsync(PackageIdentity packageIdentity, DownloadResourceResult downloadResourceResult,
    //        CancellationToken token)
    //    {
    //        return Task.FromResult(true);
    //    }

    //    public Task<bool> UninstallPackageAsync(PackageIdentity packageIdentity, CancellationToken token)
    //    {
    //        return Task.FromResult(true);
    //    }

    //    public Task SetServerModulesLock(IReadOnlyList<PackageIdentity> primaryModules, PackagesLock serverLock)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task AddModulesLock(NuGetFramework framework, PackagesLock packagesLock)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}