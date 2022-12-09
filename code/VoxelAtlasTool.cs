using Sandbox;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System;
using Editor;

namespace Facepunch.Voxels;

public class Sprite
{
	[JsonIgnore] public Pixmap Image { get; set; }

	public string FilePath { get; set; }
	public string Name { get; set; }

	public void Load( Atlas atlas )
	{
		//var texture = Texture.Load( FileSystem.Root, FilePath );
		var root = Path.GetDirectoryName( atlas.FileName );
		var absolutePath = Path.Join( root, FilePath ).NormalizeFilename( false );

		Image = new Pixmap( atlas.SpriteSize, atlas.SpriteSize );
		Image.Clear( new Color( 0f, 0f, 0f, 0f ) );

		Paint.Target( Image );
		Paint.Draw( new Rect( 0f, 0f, atlas.SpriteSize, atlas.SpriteSize ), absolutePath );
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
		Width = sprite.Image.Width;
		Height = sprite.Image.Height;
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

	public void SetAtlas( Atlas atlas )
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
		Sprites = atlas.Sprites;
		Width = 0f;
		Height = atlas.SpriteSize;

		var x = 0f;
		var y = 0f;

		foreach ( var sprite in Sprites )
		{
			var preview = new SpritePreview( this );
			preview.SetSprite( sprite );
			preview.Visible = true;
			preview.Position = new Vector2( x, y );

			Width = MathF.Min( Width + atlas.SpriteSize, maxWidth );

			x += atlas.SpriteSize;

			if ( x + atlas.SpriteSize >= maxWidth )
			{
				Height += atlas.SpriteSize;
				y += atlas.SpriteSize;
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
	public int SpriteSize { get; set; } = 32;

	[JsonIgnore]
	public string FileName { get; set; }

	public void LoadSprites()
	{
		Sprites.Clear();

		var root = Path.GetDirectoryName( FileName );
		var absolutePath = Path.Join( root, SpriteFolder ).NormalizeFilename( false );

		foreach ( var file in Directory.EnumerateFiles( absolutePath, "*.png" ) )
		{
			var relativePath = Path.GetRelativePath( root, file ).NormalizeFilename( false );
			var fileName = Path.GetFileNameWithoutExtension( relativePath );
			var sprite = new Sprite();

			sprite.FilePath = relativePath;
			sprite.Name = fileName;
			sprite.Load( this );

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
	private IntEditor SpriteSizeSlider { get; set; }
	private Widget View { get; set; }

	public VoxelAtlasTool()
	{
		CurrentAtlas = null;
		Title = "Voxel Atlas";
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

	protected override void OnPaint()
	{
		base.OnPaint();

		if ( CurrentAtlas == null )
		{
			var center = Size * 0.5f;
			var boxSize = new Vector2( Width * 0.8f, Height * 0.1f );
			var text = "No atlas is currently loaded.";

			Paint.ClearBrush();
			Paint.SetPen( Color.Red.Darken( 0.5f ) );
			Paint.SetBrush( Color.Red.Darken( 0.7f ) );

			var boxRect = new Rect( center.x - boxSize.x * 0.5f, center.y - boxSize.y * 0.5f, boxSize.x, boxSize.y );
			Paint.DrawRect( boxRect, 8f );

			Paint.SetPen( Color.Red.Darken( 0.3f ) );
			Paint.SetDefaultFont( 12 );
			Paint.DrawText( boxRect, text, TextFlag.Center );
		}
		else if ( SpriteSizeSlider != null )
		{
			var center = new Vector2( Width * 0.5f, 120f );
			var boxSize = new Vector2( Width * 0.5f, 80f );
			var text = "Sprite Size";

			Paint.ClearBrush();
			Paint.SetPen( Color.Green.Darken( 0.5f ) );
			Paint.SetBrush( Color.Green.Darken( 0.7f ) );

			var boxRect = new Rect( center.x - boxSize.x * 0.5f, center.y - boxSize.y * 0.5f, boxSize.x, boxSize.y );
			Paint.DrawRect( boxRect, 8f );

			Paint.SetPen( Color.Green.Darken( 0.3f ) );
			Paint.SetDefaultFont( 12 );
			Paint.DrawText( boxRect.Shrink( 0f, 16f, 0f, 0f ), text, TextFlag.CenterTop );
		}
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

		CurrentAtlas.FileName = fileDialog.SelectedFile.NormalizeFilename( false );
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
		Log.Info( path );
		CurrentAtlas.FileName = path.NormalizeFilename( false );
		CurrentAtlas.LoadSprites();

		Title = $"Voxel Atlas ({CurrentAtlas.FileName})";

		if ( SpriteSizeSlider != null )
		{
			SpriteSizeSlider.Value = CurrentAtlas.SpriteSize;
		}

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

		var json = JsonSerializer.Serialize( CurrentAtlas );
		File.WriteAllText( CurrentAtlas.FileName, json );

		var path = Path.GetDirectoryName( CurrentAtlas.FileName );
		var name = Path.GetFileNameWithoutExtension( CurrentAtlas.FileName ).Replace( ".atlas", "" );

		UpdatePixmapPowerOfTwo();

		var savePath = Path.Join( path, $"{name}.png" ).NormalizeFilename( false );
		AtlasPixmap.SavePng( savePath );

		UpdatePixmap();
	}

	private void UpdatePixmapPowerOfTwo()
	{
		var pixmap = new Pixmap( 2048, 2048 );
		var x = 0;
		var y = 0;

		pixmap.Clear( new Color( 0f, 0f, 0f, 0f ) );

		Paint.Target( pixmap );

		foreach ( var sprite in CurrentAtlas.Sprites )
		{
			Paint.Draw( new Rect( x, y, CurrentAtlas.SpriteSize, CurrentAtlas.SpriteSize ), sprite.Image );

			x += CurrentAtlas.SpriteSize;

			if ( x >= 2048 )
			{
				y += CurrentAtlas.SpriteSize;
				x = 0;
			}
		}

		AtlasPixmap = pixmap;
	}

	private void UpdatePixmap()
	{
		var width = 0;
		var height = CurrentAtlas.SpriteSize;

		foreach ( var sprite in CurrentAtlas.Sprites )
		{
			width += CurrentAtlas.SpriteSize;

			if ( width >= 2048 )
			{
				height += CurrentAtlas.SpriteSize;
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
			Paint.Draw( new Rect( x, y, CurrentAtlas.SpriteSize, CurrentAtlas.SpriteSize ), sprite.Image );

			x += CurrentAtlas.SpriteSize;

			if ( x >= width )
			{
				y += CurrentAtlas.SpriteSize;
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

		var root = Path.GetDirectoryName( CurrentAtlas.FileName );
		var path = Path.GetRelativePath( root, fileDialog.SelectedFile ).NormalizeFilename( false );
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
			if ( SpriteSizeSlider != null )
			{
				SpriteSizeSlider.Destroy();
				SpriteSizeSlider = null;

			}
			if ( SpriteSizeSlider == null )
			{
				SpriteSizeSlider = new IntEditor( View );
				SpriteSizeSlider.Value = CurrentAtlas.SpriteSize;
				SpriteSizeSlider.OnValueEdited += OnSpriteSizeChanged;
			}

			View.Width = Width;
			View.Height = Height;

			if ( Preview == null )
			{
				Preview = new AtlasPreview( View );
			}

			Preview.SetAtlas( CurrentAtlas );
			Preview.Position = new Vector2( Width * 0.5f, Height * 0.5f ) - Preview.Size * 0.5f;
			Preview.Position = Preview.Position - new Vector2( 0f, 64f );
			Preview.Visible = true;

			if ( SpriteSizeSlider != null )
			{
				SpriteSizeSlider.Width = View.Width * 0.3f;
				SpriteSizeSlider.Position = new Vector2( View.Width * 0.5f - SpriteSizeSlider.Width * 0.5f, 60f );
				SpriteSizeSlider.Visible = true;
			}

			FolderOption.Enabled = true;
			SaveOption.Enabled = true;
		}
		else if ( SpriteSizeSlider != null )
		{
			SpriteSizeSlider.Visible = false;
		}
	}

	private void OnSpriteSizeChanged()
	{
		if ( CurrentAtlas != null )
		{
			CurrentAtlas.SpriteSize = SpriteSizeSlider.Value;
			CurrentAtlas.LoadSprites();
			UpdatePixmap();
			Initialize();
		}
	}
}
