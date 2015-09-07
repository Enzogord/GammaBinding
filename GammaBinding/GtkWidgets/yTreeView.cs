﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Data.Bindings;
using Gamma.Binding;
using Gamma.Binding.Core;
using Gamma.ColumnConfig;
using Gamma.GtkWidgets.Cells;
using Gtk;

namespace Gamma.GtkWidgets
{
	[ToolboxItem (true)]
	[Category ("Gamma Gtk")]
	public class yTreeView : TreeView
	{
		public BindingControler<yTreeView> Binding { get; private set;}

		IColumnsConfig columnsConfig;
		public IColumnsConfig ColumnsConfig {
			get {
				return columnsConfig;
			}
			set {if (columnsConfig == value)
					return;
				columnsConfig = value;
				ReconfigureColumns ();
			}
		}

		object itemsDataSource;

		public virtual object ItemsDataSource {
			get { return itemsDataSource; }
			set {
				if (value is IObservableList)
				{
					if(Reorderable)
						YTreeModel = new ObservableListReorderableTreeModel (value as IObservableList);
					else
						YTreeModel = new ObservableListTreeModel (value as IObservableList);
				}
				else if(value is IList)
				{
					YTreeModel = new ListTreeModel (value as IList);
				}
				else
					return;
				itemsDataSource = value; }
		}

		private IyTreeModel yTreeModel;

		public IyTreeModel YTreeModel {
			get {
				return yTreeModel;
			}
			set {
				yTreeModel = value;
				Model = yTreeModel == null ? null : yTreeModel.Adapter;
			}
		}

		public yTreeView ()
		{
			Binding = new BindingControler<yTreeView> (this);
		}

		bool ReconfigureColumns ()
		{
			while (Columns.Length > 0)
				RemoveColumn(Columns[0]);

			foreach (var col in ColumnsConfig.ConfiguredColumns)
			{
				TreeViewColumn tvc = new TreeViewColumn ();
				tvc.Title = col.Title;
				foreach(var render in col.ConfiguredRenderers)
				{
					var cell = render.GetRenderer () as CellRenderer;
					if(cell is CellRendererSpin)
					{
						(cell as CellRendererSpin).EditingStarted += OnNumbericNodeCellEditingStarted;
						(cell as CellRendererSpin).Edited += NumericNodeCellEdited;
					}
					if(cell is CellRendererCombo)
					{
						(cell as CellRendererCombo).Edited += ComboNodeCellEdited;
					}
					tvc.PackStart (cell, render.IsExpand);
					tvc.SetCellDataFunc (cell, NodeRenderColumnFunc);
				}
				AppendColumn (tvc);
			}

			return true;
		}

		private void OnNumbericNodeCellEditingStarted (object o, Gtk.EditingStartedArgs args)
		{
			var cell = o as INodeCellRenderer;
			if(cell != null)
			{
				object obj = YTreeModel.NodeAtPath (new TreePath(args.Path));
				if (cell.DataPropertyInfo != null) {
					object propValue = cell.DataPropertyInfo.GetValue (obj, null);
					var spin = o as CellRendererSpin;
					if(spin != null)
					{
						//WORKAROUND to fix GTK bug that CellRendererSpin start editing only with integer number
						spin.Adjustment.Value = Convert.ToDouble (propValue);
					}
				}
			}
		}

		private void NumericNodeCellEdited (object o, Gtk.EditedArgs args)
		{
			TreeIter iter;

			INodeCellRenderer cell =  o as INodeCellRenderer;
			//CellRendererSpin cellSpin =  o as CellRendererSpin;

			if (cell != null) {
				// Resolve path as it was passed in the arguments
				Gtk.TreePath tp = new Gtk.TreePath (args.Path);
				// Change value in the original object
				if (YTreeModel.Adapter.GetIter (out iter, tp)) {
					object obj = YTreeModel.NodeFromIter (iter);

					if (cell.DataPropertyInfo.CanWrite && !String.IsNullOrWhiteSpace(args.NewText)) {
						object newval = System.Convert.ChangeType (args.NewText, cell.DataPropertyInfo.PropertyType);
						cell.DataPropertyInfo.SetValue (obj, newval, null);
					}
				}
			}
		}
			
		private void ComboNodeCellEdited (object o, Gtk.EditedArgs args)
		{
			Gtk.TreeIter iter;

			INodeCellRenderer cell =  o as INodeCellRenderer;
			CellRendererCombo combo = o as CellRendererCombo;

			if (cell != null) {
				// Resolve path as it was passed in the arguments
				Gtk.TreePath tp = new Gtk.TreePath (args.Path);
				// Change value in the original object
				if (YTreeModel.Adapter.GetIter (out iter, tp)) {
					object obj = YTreeModel.NodeFromIter (iter);
					if (cell.DataPropertyInfo.CanWrite && !String.IsNullOrWhiteSpace(args.NewText)) {
						foreach (object[] row in (ListStore)combo.Model)
						{
							if((string)row[(int)NodeCellRendererColumns.title] == args.NewText)
							{
								cell.DataPropertyInfo.SetValue (obj, row[(int)NodeCellRendererColumns.value], null);
								break;
							}
						}
					}
				}
			}
		}

		private void NodeRenderColumnFunc (Gtk.TreeViewColumn aColumn, Gtk.CellRenderer aCell, 
			Gtk.TreeModel aModel, Gtk.TreeIter aIter)
		{
			object node = YTreeModel.NodeFromIter (aIter);
			var nodeCell = aCell as INodeCellRenderer;
			if (nodeCell != null)
			{
				try
				{
					nodeCell.RenderNode (node);
				}
				catch(Exception ex)
				{
					throw new InvalidProgramException (
						String.Format ("Exception inside rendering column {0}.", aColumn.Title),
						ex
					);
				}
			}
		}

		public object[] GetSelectedObjects()
		{
			TreePath[] tp = Selection.GetSelectedRows();
			object[] rows = new object[tp.Length];
			for (int i=0; i<rows.Length; i++) {
				rows[i] = YTreeModel.NodeAtPath (tp[i]);
			}
			return (rows);
		}

		public object GetSelectedObject()
		{
			TreeIter iter;
			if (Selection.GetSelected (out iter))
				return YTreeModel.NodeFromIter (iter);
			else
				return null;
		}

	}
}

