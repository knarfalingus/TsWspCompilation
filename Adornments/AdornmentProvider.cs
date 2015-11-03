﻿using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace TsWspCompilation.Adornments
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("javascript")]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class AdornmentProvider : IWpfTextViewCreationListener
    {
        private const string _propertyName = "ShowWatermark";
        private const double _initOpacity = 0.3D;
        //SettingsManager _settingsManager;

        private static bool _isVisible, _hasLoaded;

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        public SVsServiceProvider serviceProvider { get; set; }

        private void LoadSettings()
        {
            _hasLoaded = true;

            //_settingsManager = new ShellSettingsManager(serviceProvider);
            //SettingsStore store = _settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            //LogoAdornment.VisibilityChanged += AdornmentVisibilityChanged;

            _isVisible = true;// store.GetBoolean(Constants.FILENAME, _propertyName, true);
        }

        //private void AdornmentVisibilityChanged(object sender, bool isVisible)
        //{
        //    WritableSettingsStore wstore = _settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        //    _isVisible = isVisible;

        //    if (!wstore.CollectionExists(Constants.FILENAME))
        //        wstore.CreateCollection(Constants.FILENAME);

        //    wstore.SetBoolean(Constants.FILENAME, _propertyName, isVisible);
        //}

        public void TextViewCreated(IWpfTextView textView)
        {
            if (!_hasLoaded)
                LoadSettings();

            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                string fileName = Path.GetFileName(document.FilePath).ToLowerInvariant();

                if (string.IsNullOrEmpty(fileName))
                    return;

                CreateAdornments(document, textView);
            }
        }

        private void CreateAdornments(ITextDocument document, IWpfTextView textView)
        {
            string fileName = document.FilePath;
            {
                var item = TsWspPackage.DTE.Solution.FindProjectItem(fileName);

                if (item == null || item.ContainingProject == null)
                    return;

                try {

                    if (TsWspHelpers.HasParentTypescriptFile(fileName))
                    {
                        GeneratedAdornment generated = new GeneratedAdornment(textView, _isVisible, _initOpacity);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
        }
    }
}