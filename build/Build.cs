using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.NSwag;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.Docker.DockerTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.NSwag.NSwagTasks;
using static Nuke.Common.Tools.Xunit.XunitTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
//[AzurePipelines(
//    AzurePipelinesImage.UbuntuLatest,
//    InvokedTargets = new[] { nameof(Test), nameof(Push) },
//    TriggerBranchesInclude = new[] { "master", "develop" },
//    ImportSecrets = new[] { nameof(NugetApiKey) })] // https://github.com/nuke-build/nuke/issues/531
class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    [Parameter("Nuget feed - Where to publish te nuget packages")]
    readonly string NugetApiUrl = "https://api.nuget.org/v3/index.json";
    [Parameter("Nuget api key - Credentials to publish nuget packages")]
    readonly string NugetApiKey;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion/*(Framework="netcoreapp3.0")*/] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src"; // TODO

    AbsolutePath FoundationSourceDirectory => RootDirectory / "foundation";

    AbsolutePath ServicesSourceDirectory => RootDirectory / "services";

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    AbsolutePath NugetDirectory => ArtifactsDirectory / ".nuget";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            FoundationSourceDirectory.GlobDirectories("**/bin", "**/obj", "**/TestResults").ForEach(DeleteDirectory);
            ServicesSourceDirectory.GlobDirectories("**/bin", "**/obj", "**/TestResults", "**/logs").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .After(Compile)
        .Executes(() =>
        {
            var projects = Solution.AllProjects.Where(p =>
                p.Name.EndsWith("Tests"));
            DotNetTest(t => t
                .EnableNoBuild()
                .EnableNoRestore()
                .SetConfiguration(Configuration)
                .CombineWith(projects, (s, p) => s
                    .SetProjectFile(p)
                    .SetProcessWorkingDirectory(p.Directory)
                    .SetResultsDirectory("TestResults/")
                    .SetLogger("trx")));
        });

    Target Pack => _ => _
      .DependsOn(Compile)
      .Produces(NugetDirectory) // Azure artifacts http://www.nuke.build/docs/authoring-builds/ci-integration.html
      .Executes(() =>
      {
          EnsureCleanDirectory(ArtifactsDirectory);
          var projects = Solution.AllProjects.Where(p =>
             p.Name.StartsWith("Foundation"));
          DotNetPack(s => s
              .EnableNoBuild()
              .EnableNoRestore()
              .SetVersion(GitVersion.NuGetVersionV2)
              .SetNoDependencies(true)
              .SetConfiguration(Configuration)
              .CombineWith(projects, (s, p) => s
                .SetProject(p)
                .SetOutputDirectory(NugetDirectory)));
      });

    Target Push => _ => _
       .DependsOn(Pack)
       .Requires(() => NugetApiUrl)
       .Requires(() => NugetApiKey)
       .OnlyWhenStatic(() => IsServerBuild)
       .Executes(() =>
       {
           GlobFiles(NugetDirectory, "*.nupkg")
               .NotEmpty()
               .Where(f => !f.EndsWith("symbols.nupkg"))
               .ForEach(f =>
               {
                   DotNetNuGetPush(s => s
                       .SetTargetPath(f)
                       .SetSource(NugetApiUrl)
                       .SetApiKey(NugetApiKey)
                   );
               });
       });

    Target Publish => _ => _
        .After(Test)
        .Executes(() =>
        {
            var projects = Solution.AllProjects.Where(p =>
                !p.Name.Contains("Tests")
                && !p.Name.Contains("build")
                && p.Is(ProjectType.CSharpProject));

            DotNetPublish(s => s
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore()
                .CombineWith(projects, (x, p) => x
                    .SetProject(p)
                    .SetOutput(ArtifactsDirectory / p.Name)));
        });

    //Target PublishArtifacts => _ => _
    //.DependsOn(Pack)
    //.Executes(() =>
    //{
    //    AzurePipelines.Instance?.UploadArtifacts(NugetDirectory, "nugets", "package");

    //    //AzurePipelines.Instance?.PublishCodeCoverage(
    //    //    AzurePipelinesCodeCoverageToolType.Cobertura,
    //    //    CoverageReportDirectory / "coverage.xml",
    //    //    CoverageReportDirectory);
    //});

    Target GenerateClient => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var clientDir = SourceDirectory;
            var clientProjDir = clientDir / "ServiceDemo.Client";
            EnsureCleanDirectory(clientProjDir);

            var openApiPath = clientDir / "ServiceDemo.json";

            NSwagAspNetCoreToOpenApi(x => x
                .SetNSwagRuntime("NetCore31")
                .SetAssembly(SourceDirectory / "ServiceDemo.Api" / "bin" / Configuration.ToString() / "netcoreapp3.1" / "ServiceDemo.Api.dll")
                .SetDocumentName("v1")
                .EnableUseDocumentProvider()
                .SetOutputType(SchemaType.OpenApi3)
                .SetOutput(openApiPath)
            );

            NSwagSwaggerToCSharpClient(x => x
                .SetNSwagRuntime("NetCore31")
                .SetInput(openApiPath)
                .SetOutput(clientProjDir / "ServiceDemo.Client.cs")
                .SetNamespace("ServiceDemo.Clients")
                .SetGenerateClientInterfaces(true)
                .SetGenerateExceptionClasses(true)
                .SetExceptionClass("{controller}ClientException")
            );

            var version = GitRepository.Branch.Equals("main", StringComparison.OrdinalIgnoreCase) ? GitVersion.MajorMinorPatch : GitVersion.NuGetVersionV2;

            DotNet($"new classlib -o {clientProjDir}", workingDirectory: clientProjDir);
            DeleteFile(clientProjDir / "Class1.cs");
            DotNet($"add package Newtonsoft.Json", workingDirectory: clientProjDir);
            DotNet("add package System.ComponentModel.Annotations", workingDirectory: clientProjDir);

            DotNetPack(x => x
                .SetProject(clientProjDir)
                .SetOutputDirectory(ArtifactsDirectory)
                .SetConfiguration(Configuration)
                .SetVersion(version)
                .SetIncludeSymbols(true)
           );
        });
}
