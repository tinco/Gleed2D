﻿using System;
using System.Collections.Generic;
using System.ComponentModel ;
using System.Diagnostics ;
using System.Drawing;
using System.IO ;
using System.Threading ;
using System.Windows.Forms;
using Gleed2D.InGame ;
using Ookii.Dialogs ;

namespace Gleed2D.Core.Controls
{
	public partial class TexturePickerControl : UserControl
	{
    	readonly Stopwatch _stopwatch = new Stopwatch();

		public event EventHandler<PathToFolderChangedEventArgs> PathToFolderChanged ;
		public event EventHandler<DraggingTextureEventArgs> DraggingTextureEvent;
		public event EventHandler<EntityChosenEventArgs> TextureChosen;

		readonly ImageList _imageList48;
		readonly ImageList _imageList96;
		readonly ImageList _imageList64;
		readonly ImageList _imageList128;
		readonly ImageList _imageList256;
		
		public TexturePickerControl()
		{
			InitializeComponent( ) ;

			_imageList48 = new ImageList( ) ;
			_imageList64 = new ImageList( ) ;
			_imageList96 = new ImageList( ) ;
			_imageList128 = new ImageList( ) ;
			_imageList256 = new ImageList( ) ;
			
			init( _imageList48, 48 ) ;
			init( _imageList64, 64 ) ;
			init( _imageList96, 96 ) ;
			init( _imageList128, 128 ) ;

			uiThumbnailSizesCombo.Items.Add( "48x48" ) ;
			uiThumbnailSizesCombo.Items.Add( "64x64" ) ;
			uiThumbnailSizesCombo.Items.Add( "96x96" ) ;
			uiThumbnailSizesCombo.Items.Add( "128x128" ) ;
			uiThumbnailSizesCombo.Items.Add( "256x256" ) ;

			uiThumbnailSizesCombo.SelectedIndex = 1 ;

			Gdi.SetListViewSpacing( uiListView, 128 + 8, 128 + 32 ) ;

			fileSystemWatcher.NotifyFilter =
				NotifyFilters.LastWrite |
					NotifyFilters.FileName |
						NotifyFilters.DirectoryName ;

			fileSystemWatcher.Changed += ( sender, e ) => loadFolder(
				new PathToFolder
					{
						AbsolutePath = fileSystemWatcher.Path
					} ) ;

			fileSystemWatcher.Deleted += ( sender1, e1 ) => loadFolder(
				new PathToFolder
					{
						AbsolutePath = fileSystemWatcher.Path
					} ) ;
		}

		void init( ImageList imageList, int size )
		{
			imageList.ColorDepth = ColorDepth.Depth32Bit;
			imageList.ImageSize = new Size(size, size);
			imageList.TransparentColor = Color.Transparent;
		}

		void uiListView_DragDrop(object sender, DragEventArgs e)
		{
			//uiListView.Cursor = Cursors.Default ;
			//IoC.MainForm.SetCursorForCanvas( Cursors.Default ) ;
		}

		void uiListView_DragOver(object sender, DragEventArgs e)
		{
			//e.Effect = DragDropEffects.Move;
		}

		void uiListView_ItemDrag(object sender, ItemDragEventArgs e)
		{
			var item = (ListViewItem)e.Item;
			
			if (item.Tag.ToString() == @"folder")
			{
				return;
			}
			
			IoC.MainForm.SetToolStripStatusLabel1( item.ToolTipText );
			
			var bitmap = new Bitmap(uiListView.LargeImageList.Images[item.ImageKey]);
			
			new Cursor(bitmap.GetHicon());

			var creationProperties = item.Tag as EntityCreationProperties ;

			var plugin = (IPlugin)Activator.CreateInstance(creationProperties.PluginType);

			var handlerForPlugin = plugin.CreateDragDropHandler();
			handlerForPlugin[@"PathToTexture"] = @"whatever";
			handlerForPlugin[@"CreationProperties"] = creationProperties;

			uiListView.DoDragDrop(new HandleDraggingOfAssets(handlerForPlugin), DragDropEffects.Move);
		}

		void uiListView_GiveFeedback(object sender, GiveFeedbackEventArgs e)
		{
			//e.UseDefaultCursors = false;
			//Cursor.Current = _dragCursor;
			//IoC.MainForm.SetCursorForCanvas(Cursors.Default);
		}

		void buttonFolderUpClick(object sender, EventArgs e)
		{
			//when clicking a texuture, uifolderText.Text is the same name as the folder - should just be the folder selected.
			DirectoryInfo directoryInfo = Directory.GetParent(uiFolderText.Text);
			
			if (directoryInfo == null)
			{
				return;
			}

			bool shouldCancel = notifyPathChanged(directoryInfo.FullName) ;

			if( shouldCancel )
			{
				return ;
			}
			
			loadFolder(
				new PathToFolder
					{
						AbsolutePath = directoryInfo.FullName
					} ) ;
		}

		bool notifyPathChanged( string path )
		{
			if (PathToFolderChanged != null)
			{
				var pathToFolderChangedEventArgs = new PathToFolderChangedEventArgs
					{
						Cancel = false,
						ChosenFolder = path
					} ;
				
				PathToFolderChanged(this, pathToFolderChangedEventArgs);

				return pathToFolderChangedEventArgs.Cancel ;
			}
			
			return true ;
		}

		void chooseFolderClick(object sender, EventArgs e)
		{
			using( var dialog = new VistaFolderBrowserDialog( ) )
			{
				dialog.SelectedPath = uiFolderText.Text ;

				if( dialog.ShowDialog( ) == DialogResult.OK )
				{
					string selectedPath = dialog.SelectedPath ;

					bool shouldCancel = notifyPathChanged(selectedPath) ;

					if (!shouldCancel)
					{
						loadFolder(
							new PathToFolder
								{
									AbsolutePath = selectedPath
								});
					}
				}
			}
		}

		void comboSizeSelectedIndexChanged(object sender, EventArgs e)
		{
			switch (uiThumbnailSizesCombo.SelectedIndex)
			{
				case 0:
					uiListView.LargeImageList = _imageList48;
					Gdi.SetListViewSpacing(uiListView, 48 + 8, 48 + 32);
					break;
				case 1:
					uiListView.LargeImageList = _imageList64;
					Gdi.SetListViewSpacing(uiListView, 64 + 8, 64 + 32);
					break;
				case 2:
					uiListView.LargeImageList = _imageList96;
					Gdi.SetListViewSpacing(uiListView, 96 + 8, 96 + 32);
					break;
				case 3:
					uiListView.LargeImageList = _imageList128;
					Gdi.SetListViewSpacing(uiListView, 128 + 8, 128 + 32);
					break;
				case 4:
					uiListView.LargeImageList = _imageList256;
					Gdi.SetListViewSpacing(uiListView, 256 + 8, 256 + 32);
					break;
			}
		}

		void populateTexturesWithItemInFolder(PathToFolder path)
		{
			var imageLists = new[ ]
				{
					_imageList48, _imageList64, _imageList96, _imageList128, _imageList256
				} ;

			imageLists.ForEach( il => il.Images.Clear( ) ) ;

			Image folderImage = Resource1.FolderImage;
	
			imageLists.ForEach( il => il.Images.Add( folderImage ) ) ;

			uiListView.Clear();

			var directoryInfo = new DirectoryInfo(path.AbsolutePath);

			uiFolderText.Text = directoryInfo.FullName;
			
			DirectoryInfo[] directories = directoryInfo.GetDirectories();
			
			foreach (DirectoryInfo eachDirectoryInfo in directories)
			{
				var listViewItem = new ListViewItem
					{
						Text = eachDirectoryInfo.Name,
						ToolTipText = eachDirectoryInfo.Name,
						ImageIndex = 0,
						Tag = "folder",
						Name = eachDirectoryInfo.FullName
					} ;
				
				uiListView.Items.Add(listViewItem);
			}

			const string filters = "*.jpg;*.png;*.bmp;" ;

			var fileList = new List<FileInfo>();
			
			string[] extensions = filters.Split(';');
			
			foreach (string filter in extensions)
			{
				fileList.AddRange(directoryInfo.GetFiles(filter));
			}
			
			FileInfo[] files = fileList.ToArray();
			
			foreach (FileInfo file in files)
			{
				Bitmap bmp ;
				using( var stream = File.OpenRead( file.FullName ) )
				{
					bmp = (Bitmap) Image.FromStream( stream ) ;
				}
				
				_imageList48.Images.Add(file.FullName, getThumbnail(bmp, 48, 48));
				_imageList64.Images.Add(file.FullName, getThumbnail(bmp, 64, 64));
				_imageList96.Images.Add(file.FullName, getThumbnail(bmp, 96, 96));
				_imageList128.Images.Add(file.FullName, getThumbnail(bmp, 128, 128));
				_imageList256.Images.Add(file.FullName, getThumbnail(bmp, 256, 256));

				var creationProperties = new EntityCreationProperties
					{
					 	Name = CreationPropertiesForEachItem.Name,
						PluginType = CreationPropertiesForEachItem.PluginType
					} ;
				
				creationProperties.AddParameter( @"FullPath" , file.FullName ) ;

				var lvi = new ListViewItem
					{
						Tag = creationProperties,
						Name = file.FullName,
						Text = file.Name,
						ImageKey = file.FullName,
						ToolTipText = string.Format( @"{0} ({1} x {2})", file.Name, bmp.Width.ToString( ), bmp.Height.ToString( ) )
					} ;

				uiListView.Items.Add(lvi);
			}
		}

		public EntityCreationProperties CreationPropertiesForEachItem
		{
			get	;set;
		}

		//todo: move this out into an event
		void uiListView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			ListViewItem focusedItem = uiListView.FocusedItem;

			if( TextureChosen != null )
			{
				var creationProperties = focusedItem.Tag as EntityCreationProperties;

				TextureChosen(
					this,
					new EntityChosenEventArgs
						{
							EntityCreationProperties = creationProperties
						} ) ;
			}

			string itemtype = focusedItem.Tag.ToString();
		
			if (itemtype == @"folder")
			{
				loadFolder(
					new PathToFolder
						{
							AbsolutePath = focusedItem.Name
						} ) ;
			}
		}

		void listView1Click(object sender, EventArgs e)
		{
			//todo: move
			IoC.MainForm.SetToolStripStatusLabel1( uiListView.FocusedItem.ToolTipText);
		}


		void loadFolder(PathToFolder path)
		{
			fileSystemWatcher.Path = path.AbsolutePath ;
			
			populateTexturesWithItemInFolder(path);
		}

		static Image getThumbnail(Bitmap bitmap, int iamgeWidth, int imageHeight)
		{
			var retBmp = new Bitmap(iamgeWidth, imageHeight, System.Drawing.Imaging.PixelFormat.Format64bppPArgb);
			
			Graphics graphics = Graphics.FromImage(retBmp);
			
			int tnWidth = iamgeWidth, tnHeight = imageHeight;
			
			if (bitmap.Width > bitmap.Height)
			{
				tnHeight = (int)((bitmap.Height / (float)bitmap.Width) * tnWidth);
			}
			else if (bitmap.Width < bitmap.Height)
			{
				tnWidth = (int)((bitmap.Width / (float)bitmap.Height) * tnHeight);
			}
			
			int iLeft = (iamgeWidth / 2) - (tnWidth / 2);
			int iTop = (imageHeight / 2) - (tnHeight / 2);
			
			graphics.DrawImage(bitmap, iLeft, iTop, tnWidth, tnHeight);
			
			retBmp.Tag = bitmap;
			
			return retBmp;
		}

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var passedObject = (PassedObject)e.UserState;

        	string fullName = passedObject.fileinfo.FullName ;
        	
			Bitmap bitmap = passedObject.bmp ;

        	_imageList48.Images.Add(fullName, getThumbnail(bitmap, 48, 48));
            _imageList64.Images.Add(fullName, getThumbnail(bitmap, 64, 64));
            _imageList96.Images.Add(fullName, getThumbnail(bitmap, 96, 96));
            _imageList128.Images.Add(fullName, getThumbnail(bitmap, 128, 128));
            _imageList256.Images.Add(fullName, getThumbnail(bitmap, 256, 256));

        	uiListView.Items[ fullName ].ImageKey = fullName ;
        	
			uiListView.Items[ fullName ].ToolTipText = string.Format(
        		"{0} ({1} x {2})", passedObject.fileinfo.Name, bitmap.Width, bitmap.Height ) ;

			IoC.MainForm.SetToolStripStatusLabel1( e.ProgressPercentage.ToString( ) ) ;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _stopwatch.Stop();
            
			IoC.MainForm.SetToolStripStatusLabel1( string.Format( @"Time: {0}", _stopwatch.Elapsed.TotalSeconds.ToString() ) );
            
			_stopwatch.Reset();
        }

		class PassedObject //for passing to background worker
        {
            public Bitmap bmp;
            public FileInfo fileinfo;
        }

        public void loadfolder_background(string path)
        {
            if (backgroundWorker1.IsBusy)
            {
            	backgroundWorker1.CancelAsync();
            }
            while (backgroundWorker1.IsBusy)
            {
                Application.DoEvents();
                Thread.Sleep(50);
            }
            
			_imageList48.Images.Clear();
            _imageList64.Images.Clear();
            _imageList96.Images.Clear();
            _imageList128.Images.Clear();
            _imageList256.Images.Clear();

			uiListView.Clear();
            
			var directoryInfo = new DirectoryInfo(path);
            uiFolderText.Text = directoryInfo.FullName;

            DirectoryInfo[] folders = directoryInfo.GetDirectories();
            foreach (DirectoryInfo folder in folders)
            {
                Image img = Resource1.FolderImage;

            	string folderName = folder.Name ;

            	_imageList48.Images.Add(folderName, img);
                _imageList64.Images.Add(folderName, img);
                _imageList96.Images.Add(folderName, img);
                _imageList128.Images.Add(folderName, img);
                _imageList256.Images.Add(folderName, img);

            	var item = new ListViewItem
            		{
            			Text = folderName,
            			ToolTipText = folderName,
            			ImageIndex = _imageList128.Images.IndexOfKey( folderName ),
            			Tag = @"folder",
            			Name = folder.FullName
            		} ;
            	
				uiListView.Items.Add(item);
            }

            const string filters = @"*.jpg;*.png;*.gif;*.bmp;*.tga" ;
            var fileList = new List<FileInfo>();
            string[] extensions = filters.Split(';');
            foreach (string filter in extensions)
            {
            	fileList.AddRange(directoryInfo.GetFiles(filter));
            }
            
			FileInfo[] files = fileList.ToArray();

            var bmp = new Bitmap(1, 1);
            bmp.SetPixel(0, 0, Color.Azure);
            _imageList48.Images.Add("default", bmp);
            _imageList64.Images.Add("default", bmp);
            _imageList96.Images.Add("default", bmp);
            _imageList128.Images.Add("default", bmp);
            _imageList256.Images.Add("default", bmp);
            
			foreach (FileInfo file in files)
            {
            	var item = new ListViewItem
            		{
            			Name = file.FullName,
            			Text = file.Name,
            			ImageKey = @"default",
            			Tag = "file"
            		} ;
            	
				uiListView.Items.Add(item);
            }
            
            _stopwatch.Start();
            
			backgroundWorker1.RunWorkerAsync(files);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var files = (FileInfo[])e.Argument;
            var worker = (BackgroundWorker)sender;
            
			int filesprogressed = 0;
            
			foreach (FileInfo file in files)
            {
                try
                {
                	var po = new PassedObject
                		{
                			bmp = new Bitmap( file.FullName ),
                			fileinfo = file
                		} ;
                	
					if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                    
					filesprogressed++;
                    
					worker.ReportProgress(filesprogressed, po);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                }
            }
        }

		public void SetFolder( PathToFolder contentRootFolder )
		{
			uiFolderText.Text = contentRootFolder.AbsolutePath ;

			loadFolder( contentRootFolder );
		}

		private void uiListView_DragEnter(object sender, DragEventArgs e)
		{
			var handler = DraggingTextureEvent ;
			if( handler != null )
			{
				handler(
					this,
					new DraggingTextureEventArgs
						{
							DragEventType = DragEventType.DragEnter,
							PathToTexture = string.Empty
						} ) ;
			}
		}
	}
}
