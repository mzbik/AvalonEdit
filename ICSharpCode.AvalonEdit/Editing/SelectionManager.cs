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
using ICSharpCode.AvalonEdit.Utils;


namespace ICSharpCode.AvalonEdit.Editing
{
	class SelectionManager
	{
		public SelectionManager(TextArea textArea)
		{
			this.textArea = textArea;
			selection = emptySelection = new EmptySelection(textArea);
		}

		protected TextArea textArea;

		internal readonly Selection emptySelection;
		Selection selection;
		public void UpdateOnDocumentChanged(DocumentChangeEventArgs e)
		{
			selection = selection.UpdateOnDocumentChange(e);
		}

		/// <summary>
		/// Gets/Sets the selection in this text area.
		/// </summary>
		public Selection Selection {
			get { return selection; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				if (value.textArea != textArea)
					throw new ArgumentException("Cannot use a Selection instance that belongs to another text area.");
				if (!object.Equals(selection, value)) {
					//					Debug.WriteLine("Selection change from " + selection + " to " + value);
					var oldSelection = selection;
					selection = value;
					textArea.RaiseSelectionChanged(oldSelection, selection);

					// a selection change causes commands like copy/paste/etc. to change status
					CommandManager.InvalidateRequerySuggested();
				}
			}
		}

		/// <summary>
		/// Clears the current selection.
		/// </summary>
		public void ClearSelection()
		{
			this.Selection = emptySelection;
		}




	}
}
