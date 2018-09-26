using System;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;

namespace CommandTaskRunner
{
    public class CommandDropHandler : IDropHandler
    {
        private readonly ITextDocumentFactoryService _documentFactory;
        private readonly IWpfTextView _view;
        private string _fileName;
        private DTE2 _dte;

        public CommandDropHandler(ITextDocumentFactoryService documentFactory, IWpfTextView view)
        {
            _documentFactory = documentFactory;
            _view = view;
            _dte = (DTE2)Package.GetGlobalService(typeof(DTE));
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            ITextDocument document;

            if (!_documentFactory.TryGetTextDocument(_view.TextDataModel.DocumentBuffer, out document))
                return DragDropPointerEffects.None;

            ITextSnapshot snapshot = _view.TextBuffer.CurrentSnapshot;
            string bufferContent = snapshot.GetText();
            Newtonsoft.Json.Linq.JObject json = CommandHelpers.GetJsonContent(document.FilePath, _fileName, bufferContent);

            using (ITextEdit edit = _view.TextBuffer.CreateEdit())
            {
                edit.Replace(0, snapshot.Length, json.ToString());
                edit.Apply();
            }

            return DragDropPointerEffects.Link;
        }

        public void HandleDragCanceled() { }
        public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo) { return DragDropPointerEffects.Link; }
        public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo) { return DragDropPointerEffects.Link; }

        public bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            _fileName = GetImageFilename(dragDropInfo);

            if (string.IsNullOrEmpty(_fileName) || !CommandHelpers.IsFileSupported(_fileName) || _dte.ActiveDocument == null)
                return false;

            string activeFile = Path.GetFileName(_dte.ActiveDocument.FullName);

            if (Constants.FILENAME.Equals(activeFile, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static string GetImageFilename(DragDropInfo info)
        {
            var data = new DataObject(info.Data);

            if (info.Data.GetDataPresent("FileDrop"))
            {
                // The drag and drop operation came from the file system
                StringCollection files = data.GetFileDropList();

                if (files != null && files.Count == 1)
                    return files[0];
            }
            else if (info.Data.GetDataPresent("CF_VSSTGPROJECTITEMS"))
            {
                return data.GetText(); // The drag and drop operation came from the VS solution explorer
            }
            else if (info.Data.GetDataPresent("MultiURL"))
            {
                return data.GetText();
            }

            return null;
        }
    }
}
