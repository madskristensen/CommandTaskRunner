using System;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
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

        public CommandDropHandler(ITextDocumentFactoryService documentFactory, IWpfTextView view)
        {
            _documentFactory = documentFactory;
            _view = view;
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            ITextDocument document;

            if (!_documentFactory.TryGetTextDocument(_view.TextDataModel.DocumentBuffer, out document))
                return DragDropPointerEffects.None;

            var snapshot = _view.TextBuffer.CurrentSnapshot;
            string bufferContent = snapshot.GetText();
            var json = CommandHelpers.GetJsonContent(document.FilePath, _fileName, bufferContent);

            using (var edit = _view.TextBuffer.CreateEdit())
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

            if (string.IsNullOrEmpty(_fileName) || !CommandHelpers.IsFileSupported(_fileName) || VSPackage._dte.ActiveDocument == null)
                return false;

            string activeFile = Path.GetFileName(VSPackage._dte.ActiveDocument.FullName);

            if (Constants.FILENAME.Equals(activeFile, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static string GetImageFilename(DragDropInfo info)
        {
            DataObject data = new DataObject(info.Data);

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
