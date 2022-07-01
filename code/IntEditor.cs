using Sandbox;
using System;
using Tools;

namespace Facepunch.Voxels
{
	public class IntEditor : Widget
	{
		public string Label { get; set; }
		public string Icon { get; set; } = "edit";
		public Color HighlightColor = Color.Yellow;

		public event Action OnValueEdited;

		private Vector2 LastDragPosition;
		private float DragSpeed = 1.0f;

		public int Value
		{
			get => LineEdit.Text.ToInt();

			set
			{
				LineEdit.Text = value.ToString();
			}
		}

		public LineEdit LineEdit { get; private set; }

		public IntEditor( Widget parent ) : base( parent )
		{
			LineEdit = new LineEdit( this );
			LineEdit.TextEdited += OnLineTextEdited;
			LineEdit.MinimumSize = 24f;
			LineEdit.NoSystemBackground = true;
			LineEdit.TranslucentBackground = true;

			Cursor = CursorShape.SizeH;

			MinimumSize = 24f;
			MaximumSize = new Vector2( 4096, 24f );
		}

		public IntEditor( string label, Widget parent ) : this( parent )
		{
			Label = label;
		}

		protected override void OnMouseEnter()
		{
			base.OnMouseEnter();
			Update();
		}

		protected override void OnMouseLeave()
		{
			base.OnMouseLeave();
			Update();
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			var h = Size.y;
			bool hovered = IsUnderMouse;
			if ( !Enabled ) hovered = false;

			Paint.Antialiasing = true;
			Paint.TextAntialiasing = true;

			Paint.SetPenEmpty();
			Paint.SetBrush( Color.Black.Lighten( 0.3f ) );
			Paint.DrawRect( LocalRect, 4f );

			Paint.SetPenEmpty();
			Paint.SetBrush( HighlightColor.Darken( hovered ? 0.7f : 0.8f ).Desaturate( 0.8f ) );
			Paint.DrawRect( new Rect( 0, 0, h, h ).Expand( -1 ), 4f - 1.0f );

			Paint.DrawRect( new Rect( h - 4f, 0, 4f, h ).Expand( -1 ) );

			Paint.SetPen( HighlightColor.Darken( hovered ? 0.0f : 0.1f ).Desaturate( hovered ? 0.0f : 0.2f ) );

			if ( string.IsNullOrEmpty( Label ) )
			{
				Paint.DrawIcon( new Rect( 0, h ), Icon, h - 6, TextFlag.Center );
			}
			else
			{
				Paint.SetFont( "Poppins", 9, 450 );
				Paint.DrawText( new Rect( 1, h - 1 ), Label, TextFlag.Center );
			}
		}

		protected override void DoLayout()
		{
			base.DoLayout();

			var h = Size.y;
			LineEdit.Position = new Vector2( h, 0f );
			LineEdit.Size = Size - new Vector2( h, 0f );
		}

		private void OnLineTextEdited( string obj )
		{
			OnValueEdited?.Invoke();
		}

		protected override void OnMousePress( MouseEvent e )
		{
			base.OnMousePress( e );

			if ( e.LeftMouseButton )
			{
				LastDragPosition = e.LocalPosition;
				DragSpeed = 1;
			}
		}

		protected override void OnMouseMove( MouseEvent e )
		{
			base.OnMouseMove( e );

			if ( e.ButtonState.HasFlag( MouseButtons.Left ) )
			{
				var delta = e.LocalPosition - LastDragPosition;
				LastDragPosition = e.LocalPosition;

				DragSpeed = (e.LocalPosition.y + 100.0f) / 100.0f;
				DragSpeed = DragSpeed.Clamp( 0.001f, 1000.0f );

				Value += (int)(delta.x * 0.1f * DragSpeed);
				OnValueEdited?.Invoke();
			}
		}
	}
}
