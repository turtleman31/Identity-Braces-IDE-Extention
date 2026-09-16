using System;
using System.Runtime.InteropServices;
using System.Threading;
using IdentityBraces.Options;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace IdentityBraces
{
    /// <summary>
    /// Exists only to register the options page.
    /// </summary>
    /// <remarks>
    /// Everything that actually draws is a MEF component and is composed by the editor
    /// without this package. There is deliberately no <c>ProvideAutoLoad</c>: the extension
    /// must not add anything to Visual Studio's start-up time, and
    /// <see cref="IdentityBracesSettings"/> reads its own file rather than waiting on a
    /// package-provided service.
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideOptionPage(
        typeof(GeneralOptionsPage),
        categoryName: "Identity Braces",
        pageName: "General",
        categoryResourceID: 0,
        pageNameResourceID: 0,
        supportsAutomation: true)]
    // supportsAutomation is false here: the page's state is a weight table, not a set of
    // scalar properties, so there is nothing for DTE to bind to and claiming otherwise would
    // put an empty automation object in front of anyone scripting it.
    [ProvideOptionPage(
        typeof(TraitsOptionsPage),
        categoryName: "Identity Braces",
        pageName: "Traits",
        categoryResourceID: 0,
        pageNameResourceID: 0,
        supportsAutomation: false)]
    public sealed class IdentityBracesPackage : AsyncPackage
    {
        public const string PackageGuidString = "23A44597-F51E-4CE3-8067-F87D0B2EB12C";

        protected override Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            return Task.CompletedTask;
        }
    }
}
