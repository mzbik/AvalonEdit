using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Utils;
using System.Security.RightsManagement;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Handles text input to a document, either from UI events or internally generated
	/// </summary>
	public class TextInputHandler
	{
		private TextArea textArea;
		/// <summary>
		/// Initialize handler and saves parent TextArea
		/// </summary>
		public TextInputHandler( TextArea textArea)
		{
			this.textArea = textArea;
		}


		/// <summary>
		/// Performs text input.
		/// This raises the <see cref="TextArea.TextEntering"/> event, replaces the selection with the text,
		/// and then raises the <see cref="TextArea.TextEntered"/> event.
		/// </summary>
		public void PerformTextInput(string text)
		{
			TextComposition textComposition = new TextComposition(InputManager.Current, textArea, text);
			TextCompositionEventArgs e = new TextCompositionEventArgs(Keyboard.PrimaryDevice, textComposition);
			e.RoutedEvent = UIElement.TextInputEvent;
			PerformTextInput(e);
		}

		/// <summary>
		/// Performs text input.
		/// This raises the <see cref="TextArea.TextEntering"/> event, replaces the selection with the text,
		/// and then raises the <see cref="TextArea.TextEntered"/> event.
		/// </summary>
		public void PerformTextInput(TextCompositionEventArgs e)
		{
			if (e == null)
				throw new ArgumentNullException("e");
			if (textArea.Document == null)
				throw ThrowUtil.NoDocumentAssigned();
			textArea.OnTextEntering(e);
			if (!e.Handled) {
				if (e.Text == "\n" || e.Text == "\r" || e.Text == "\r\n")
					ReplaceSelectionWithNewLine();
				else {
					if (textArea.OverstrikeMode && textArea.SelectionManager.Selection.IsEmpty && textArea.Document.GetLineByNumber(textArea.Caret.Line).EndOffset > textArea.Caret.Offset)
						EditingCommands.SelectRightByCharacter.Execute(null, textArea);
					ReplaceSelectionWithText(e.Text);
				}
				textArea.OnTextEntered(e);
				textArea.Caret.BringCaretToView();
			}
		}

		void ReplaceSelectionWithNewLine()
		{
			string newLine = TextUtilities.GetNewLineFromDocument(textArea.Document, textArea.Caret.Line);
			using (textArea.Document.RunUpdate()) {
				ReplaceSelectionWithText(newLine);
				if (textArea.IndentationStrategy != null) {
					DocumentLine line = textArea.Document.GetLineByNumber(textArea.Caret.Line);
					ISegment[] deletable = GetDeletableSegments(line);
					if (deletable.Length == 1 && deletable[0].Offset == line.Offset && deletable[0].Length == line.Length) {
						// use indentation strategy only if the line is not read-only
						textArea.IndentationStrategy.IndentLine(textArea.Document, line);
					}
				}
			}
		}

		internal void RemoveSelectedText()
		{
			if (textArea.Document == null)
				throw ThrowUtil.NoDocumentAssigned();
			textArea.SelectionManager.Selection.ReplaceSelectionWithText(string.Empty);
#if DEBUG
			if (!textArea.SelectionManager.Selection.IsEmpty) {
				foreach (ISegment s in textArea.SelectionManager.Selection.Segments) {
					Debug.Assert(textArea.ReadOnlySectionProvider.GetDeletableSegments(s).Count() == 0);
				}
			}
#endif
		}

		internal void ReplaceSelectionWithText(string newText)
		{
			if (newText == null)
				throw new ArgumentNullException("newText");
			if (textArea.Document == null)
				throw ThrowUtil.NoDocumentAssigned();
			textArea.SelectionManager.Selection.ReplaceSelectionWithText(newText);
		}

		internal ISegment[] GetDeletableSegments(ISegment segment)
		{
			var deletableSegments = textArea.ReadOnlySectionProvider.GetDeletableSegments(segment);
			if (deletableSegments == null)
				throw new InvalidOperationException("ReadOnlySectionProvider.GetDeletableSegments returned null");
			var array = deletableSegments.ToArray();
			int lastIndex = segment.Offset;
			for (int i = 0; i < array.Length; i++) {
				if (array[i].Offset < lastIndex)
					throw new InvalidOperationException("ReadOnlySectionProvider returned incorrect segments (outside of input segment / wrong order)");
				lastIndex = array[i].EndOffset;
			}
			if (lastIndex > segment.EndOffset)
				throw new InvalidOperationException("ReadOnlySectionProvider returned incorrect segments (outside of input segment / wrong order)");
			return array;
		}

	}
}
