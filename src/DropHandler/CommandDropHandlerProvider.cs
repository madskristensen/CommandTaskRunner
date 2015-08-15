using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Utilities;

namespace CommandTaskRunner
{
    [Export(typeof(IDropHandlerProvider))]
    [DropFormat("FileDrop")]
    [DropFormat("CF_VSSTGPROJECTITEMS")]
    [DropFormat("CF_VSREFPROJECTITEMS")]
    [Name("CommandDropHandler")]
    [ContentType("json")]
    [Order(Before = "DefaultFileDropHandler")]
    public class CommandDropHandlerProvider : IDropHandlerProvider
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public IDropHandler GetAssociatedDropHandler(IWpfTextView wpfTextView)
        {
            return wpfTextView.Properties.GetOrCreateSingletonProperty(() => new CommandDropHandler(TextDocumentFactoryService, wpfTextView));
        }
    }
}
