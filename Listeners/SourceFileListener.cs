using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using EnvDTE;
using Microsoft.VisualStudio;

namespace TsWspCompilation.Listeners
{

    [Export(typeof(IWpfTextViewCreationListener))]
    //[Export(typeof(IVsTextViewCreationListener))]
    [ContentType("javascript")]
    [ContentType("typescript")]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class SourceFileListener : IWpfTextViewCreationListener
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        private ITextDocument _document;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out _document))
            {
                _document.FileActionOccurred += DocumentSaved;
            }

            textView.Closed += TextviewClosed;
        }

        private void TextviewClosed(object sender, EventArgs e)
        {
            IWpfTextView view = (IWpfTextView)sender;

            if (view != null)
                view.Closed -= TextviewClosed;

            if (_document != null)
                _document.FileActionOccurred -= DocumentSaved;
        }



        private void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                string sourceFile = e.FilePath;

                TypeScriptCompiler.Compile(sourceFile);

            }
        }
    }
}
