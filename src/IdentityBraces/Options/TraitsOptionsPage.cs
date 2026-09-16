using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace IdentityBraces.Options
{
    /// <summary>
    /// Tools &gt; Options &gt; Identity Braces &gt; Traits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="UIElementDialogPage"/> rather than a <see cref="DialogPage"/>, because a
    /// property grid cannot show you what a trait looks like — and with eighty-six of them,
    /// being shown is the only way anyone is going to find the good ones.
    /// <see cref="GeneralOptionsPage"/> stays a property grid: its settings are scalars, which
    /// is exactly what that grid is for.
    /// </para>
    /// <para>
    /// The page owns a clone of the settings and hands it to the control to edit. Nothing is
    /// written until <see cref="SaveSettingsToStorage"/>, so Cancel discards the whole session
    /// — including the preview's view of it, which is drawn from the same clone.
    /// </para>
    /// </remarks>
    [Guid("6D5E1A0B-3C4F-4A28-9E71-2B8C5F0D91A4")]
    [ComVisible(true)]
    public sealed class TraitsOptionsPage : UIElementDialogPage
    {
        private TraitEditorControl _control;
        private IdentityBracesSettings _working;

        /// <summary>
        /// The page's content. Built lazily, because Visual Studio constructs every registered
        /// options page when the dialog opens, not when the page is selected.
        /// </summary>
        protected override UIElement Child
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_control == null)
                {
                    _working = IdentityBracesSettings.Current.Clone();
                    _control = new TraitEditorControl(_working);
                }

                return _control;
            }
        }

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Rebind();
        }

        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_working != null)
            {
                IdentityBracesSettings.Save(_working);

                // Save normalises in place — over-budget layers are scaled down — and bumps the
                // version. Re-cloning means a second Apply starts from what was actually stored
                // rather than from the numbers that were rejected.
                Rebind();
            }
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Rebind();
            base.OnActivate(e);
        }

        /// <remarks>
        /// Raised for Cancel as well as OK. The preview braces carry live animation clocks, and
        /// a clock whose element has been dropped from the visual tree carries on running for
        /// the life of the process — so they have to be stopped on the way out however the
        /// dialog was dismissed.
        /// </remarks>
        protected override void OnClosed(EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_control != null)
            {
                _control.Shutdown();
            }

            base.OnClosed(e);
        }

        private void Rebind()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _working = IdentityBracesSettings.Current.Clone();

            if (_control != null)
            {
                _control.Reload(_working);
            }
        }
    }
}
