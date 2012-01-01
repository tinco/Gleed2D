using System ;
using System.ComponentModel.Composition ;
using System.Reflection ;
using System.Windows.Forms ;
using Gleed2D.Core ;
using Gleed2D.Core.Controls ;

namespace Gleed2D.Plugins
{
	[Export(typeof(IEditorPlugin))]
	public class PathEditorPlugin : IEditorPlugin
	{
		readonly DefaultDragDropHandler _dragDropHandler;

		public PathEditorPlugin( )
		{
			_dragDropHandler = new DefaultDragDropHandler( 
				DragDropEffects.Move,
				whenEnteringEditor: whenEnteringEditor,
				whenDraggingOverEditor: whenDraggingOverEditor,
				whenDroppedOntoEditor: whenDroppedOntoEditor );
		}

		void whenEnteringEditor( DragEventArgs dragEventArgs )
		{
			dragEventArgs.Effect=DragDropEffects.Move;
		}

		void whenDraggingOverEditor( IEditor editor, DragEventArgs dragEventArgs )
		{
			dragEventArgs.Effect=DragDropEffects.Move;
		}

		void whenDroppedOntoEditor( IEditor editor )
		{
			editor.StartCreatingEntityNow(
				new EntityCreationProperties
					{
						Name = "Blah",
						PluginType = GetType( ),
					} ) ;
		}

		public Type EditorType
		{
			get
			{
				return typeof( PathItemEditor ) ;
			}
		}

		public Control ControlForAboutBox
		{
			get
			{
				return new PluginDescriptionControl( this ) ;
			}
		}

		public string CategoryName
		{
			get
			{
				return @"Editors/Shapes" ;
			}
		}

		public void InitialiseInUi( IMainForm mainForm )
		{
			ICategoryTabPage tab = mainForm.TryGetTabForCategory( CategoryName ) ;
			
			if( tab==null )
			{
				tab= buildTab( ) ;
				
				mainForm.AddCategoryTab( tab ) ;
			}

			tab.AddPlugin( this ) ;
		}

		ICategoryTabPage buildTab( )
		{
			var tabPage = new DefaultCategoryTabPage
				{
					CategoryName = CategoryName
				} ;
			
			return tabPage ;
		}

		public string Name
		{
			get
			{
				return @"Path" ;
			}
		}

		public ImageProperties ToolboxImage
		{
			get
			{
				return Images.SummonImage( Assembly.GetExecutingAssembly(  ), @"Gleed2D.Plugins.Resources.primitive_path.png" ) ;
			}
		}

		public ImageProperties Icon
		{
			get
			{
				return Images.SummonImage( Assembly.GetExecutingAssembly(  ), @"Gleed2D.Plugins.Resources.primitive_path.png" ) ;
			}
		}

		public IHandleDragDrop DragDropHandler
		{
			get
			{
				return _dragDropHandler ;
			}
		}
	}
}