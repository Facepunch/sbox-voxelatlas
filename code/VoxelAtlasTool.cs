using Sandbox;
using System.Collections.Generic;
using Tools;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System;

namespace Facepunch.Voxels;

public class Sprite
{
	[JsonIgnore] public Pixmap Image { get; set; }

	public string FilePath { get; set; }
	public string Name { get; set; }

	public void Load()
	{
		//var texture = Texture.Load( FileSystem.Root, FilePath );
		var absolutePath = Path.Join( Directory.GetCurrentDirectory(), FilePath ).NormalizeFilename( false );

		Image = new Pixmap( 32, 32 );
		Image.Clear( new Color( 0f, 0f, 0f, 0f ) );

		Paint.Target( Image );
		Paint.Draw( new Rect( 0f, 0f, 32f, 32f ), absolutePath );
	}
}

public class SpritePreview : Widget
{
	public Sprite Sprite { get; private set; }

	public SpritePreview( Widget parent ) : base( parent )
	{
	}

	public void SetSprite( Sprite sprite )
	{
		Sprite = sprite;
		Width = 32;
		Height = 32;
	}

	protected override void OnPaint()
	{
		if ( Sprite != null )
		{
			Paint.SetBrush( Sprite.Image );
			Paint.ClearPen();
			Paint.Scale( 1f, 1f );
			Paint.DrawRect( new Rect( 0, 0, Sprite.Image.Width, Sprite.Image.Height ) );
		}

		base.OnPaint();
	}
}

public class AtlasPreview : Widget
{
	public List<Sprite> Sprites { get; private set; }
	public List<SpritePreview> Previews { get; private set; }

	public AtlasPreview( Widget parent ) : base( parent )
	{
	}

	public void SetSprites( List<Sprite> sprites )
	{
		if ( Previews  != null )
		{
			foreach ( var preview in Previews )
			{
				preview.Destroy();
			}
		}

		var maxWidth = Parent.Width - 80;

		Previews = new();
		Sprites = sprites;
		Width = 0f;
		Height = 32f;

		var x = 0f;
		var y = 0f;

		foreach ( var sprite in sprites )
		{
			var preview = new SpritePreview( this );
			preview.SetSprite( sprite );
			preview.Visible = true;
			preview.Position = new Vector2( x, y );

			Width = MathF.Min( Width + 32f, maxWidth );

			x += 32f;

			if ( x + 32f >= maxWidth )
			{
				Height += 32f;
				y += 32f;
				x = 0f;
			}

			Previews.Add( preview );
		}
	}

	protected override void OnPaint()
	{
		base.OnPaint();
	}
}

public class Atlas
{
	public List<Sprite> Sprites { get; set; } = new();
	public string SpriteFolder { get; set; }
	public string FileName { get; set; }

	public Atlas()
	{

	}

	public void LoadSprites()
	{
		Sprites.Clear();

		var absolutePath = Path.Join( Directory.GetCurrentDirectory(), SpriteFolder ).NormalizeFilename( false );

		foreach ( var file in Directory.EnumerateFiles( absolutePath, "*.png" ) )
		{
			var relativePath = Path.GetRelativePath( Directory.GetCurrentDirectory(), file ).NormalizeFilename( false );
			var fileName = Path.GetFileNameWithoutExtension( relativePath );
			var sprite = new Sprite();

			sprite.FilePath = relativePath;
			sprite.Name = fileName;
			sprite.Load();

			Sprites.Add( sprite );
		}

		Sprites.Sort( ( a, b ) => a.Name.CompareTo( b.Name ) );
	}
}

public class SpriteWidget : Widget
{
	public SpriteWidget( Widget parent ) : base( parent )
	{
	}

	public Sprite Sprite { get; set; }

	protected override void OnPaint()
	{
		if ( Sprite != null )
		{
			Width = Sprite.Image.Width;
			Height = Sprite.Image.Height;

			Paint.SetBrush( Sprite.Image );
			Paint.ClearPen();
			Paint.DrawRect( new Rect( 0, 0, Width, Height ) );
		}

		base.OnPaint();
	}
}

[Tool( "Voxel Atlas", "grid_on", "Create atlases by packing multiple images into a single texture" )]
public class VoxelAtlasTool : Window
{
	private Atlas CurrentAtlas { get; set; }
	private Option SaveOption { get; set; }
	private Option FolderOption { get; set; }
	private Pixmap AtlasPixmap { get; set; }
	private AtlasPreview Preview { get; set; }
	private Widget View { get; set; }

	public VoxelAtlasTool()
	{
		CurrentAtlas = null;
		Title = "Voxel Atlas";
		ResizeButtonsVisible = false;
		MaximumSize = new Vector2( 800f, 800f );
		MinimumSize = MaximumSize;
		Size = MaximumSize;

		Initialize();
		Show();
		Focus();
	}

	protected override void OnResize()
	{
		Initialize();
		base.OnResize();
	}

	private void BuildMenuBar()
	{
		var menu = MenuBar.AddMenu( "File" );

		menu.AddOption( "Rebuild All", "autorenew", RebuildAll, "Ctrl+R" );
		menu.AddOption( "Create...", "add", Create, "Ctrl+N" );

		SaveOption = menu.AddOption( "Save", "save", Save, "Ctrl+S" );
		SaveOption.Enabled = false;

		menu.AddOption( "Load", "upload_file", Load, "Ctrl+O" );

		FolderOption = menu.AddOption( "Load Sprites..", "folder", LoadSprites );
		FolderOption.Enabled = false;

		menu.AddOption( "Quit", "disabled_by_default", Close );
	}

	private void Create()
	{
		CurrentAtlas = new();

		var fileDialog = new FileDialog( null );
		fileDialog.Title = "Create Atlas..";
		fileDialog.DefaultSuffix = ".atlas.json";
		fileDialog.SetFindFile();
		fileDialog.SetModeSave();
		fileDialog.SetNameFilter( "Atlas (*.atlas.json)" );

		if ( !fileDialog.Execute() )
			return;

		CurrentAtlas.FileName = Path.GetRelativePath( Directory.GetCurrentDirectory(), fileDialog.SelectedFile ).NormalizeFilename( false );
		Title = $"Voxel Atlas ({CurrentAtlas.FileName})";

		var json = JsonSerializer.Serialize( CurrentAtlas );
		File.WriteAllText( fileDialog.SelectedFile, json );

		UpdatePixmap();
		Initialize();
	}

	private void Load()
	{
		var fileDialog = new FileDialog( null );
		fileDialog.Title = $"Load Atlas";
		fileDialog.SetNameFilter( "Atlas (*.atlas.json)" );

		if ( !fileDialog.Execute() )
			return;

		LoadAtlas( fileDialog.SelectedFile );
	}

	private void LoadAtlas( string path )
	{
		var json = File.ReadAllText( path );
		CurrentAtlas = JsonSerializer.Deserialize<Atlas>( json );
		CurrentAtlas.LoadSprites();
		Title = $"Voxel Atlas ({CurrentAtlas.FileName})";

		UpdatePixmap();
		Initialize();
	}

	private async void RebuildAll()
	{
		var files = Directory.GetFiles( Directory.GetCurrentDirectory(), "*.atlas.json", SearchOption.AllDirectories );

		foreach ( var file in files )
		{
			await Task.Delay( 500 );

			LoadAtlas( file );
			Save();
		}
	}

	private void Save()
	{
		if ( CurrentAtlas == null ) return;

		CurrentAtlas.FileName = CurrentAtlas.FileName.NormalizeFilename( false );

		var json = JsonSerializer.Serialize( CurrentAtlas );
		File.WriteAllText( CurrentAtlas.FileName, json );

		var path = Path.GetDirectoryName( CurrentAtlas.FileName );
		var name = Path.GetFileNameWithoutExtension( CurrentAtlas.FileName );

		UpdatePixmap();

		var savePath = Path.Join( Directory.GetCurrentDirectory(), Path.Join( path, $"{name}.png" ) ).NormalizeFilename( false );
		AtlasPixmap.SavePng( savePath );
	}

	private void UpdatePixmap()
	{
		var width = 0;
		var height = 32;

		foreach ( var sprite in CurrentAtlas.Sprites )
		{
			width += 32;

			if ( width >= 2048 )
			{
				height += 32;
				width = 0;
			}
		}

		var pixmap = new Pixmap( width, height );
		var x = 0;
		var y = 0;

		pixmap.Clear( new Color( 0f, 0f, 0f, 0f ) );

		Paint.Target( pixmap );

		foreach ( var sprite in CurrentAtlas.Sprites )
		{
			Paint.Draw( new Rect( x, y, 32f, 32f ), sprite.Image );

			x += 32;

			if ( x >= width )
			{
				y += 32;
				x = 0;
			}
		}

		AtlasPixmap = pixmap;
	}

	private void LoadSprites()
	{
		var fileDialog = new FileDialog( null );
		fileDialog.SetFindDirectory();

		if ( !fileDialog.Execute() )
			return;

		var path = Path.GetRelativePath( Directory.GetCurrentDirectory(), fileDialog.SelectedFile ).NormalizeFilename( false );
		CurrentAtlas.SpriteFolder = path;
		CurrentAtlas.LoadSprites();

		UpdatePixmap();
		Initialize();
	}

	[Event.Hotload]
	private void Initialize()
	{
		Clear();

		BuildMenuBar();

		if ( View == null )
		{
			View = new Widget( this );
			View.SetLayout( LayoutMode.TopToBottom );
			View.Position = new Vector2( 0f, 64f );
			View.Visible = true;
		}

		if ( CurrentAtlas != null )
		{
			View.Width = Width;
			View.Height = Height;

			if ( Preview == null )
			{
				Preview = new AtlasPreview( View );
			}

			Preview.SetSprites( CurrentAtlas.Sprites );
			Preview.Position = new Vector2( Width * 0.5f, Height * 0.5f ) - Preview.Size * 0.5f;
			Preview.Position = Preview.Position - new Vector2( 0f, 64f );
			Preview.Visible = true;

			SaveOption.Enabled = true;
			FolderOption.Enabled = true;
		}
	}
}
