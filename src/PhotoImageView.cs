//
// FSpot.Widgets.PhotoImageView.cs
//
// Copyright (c) 2004-2009 Novell, Inc.
//
// Author(s)
//	Larry Ewing  <lewing@novell.com>
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details.
//

using System;
using FSpot.Editors;
using FSpot.Utils;
using FSpot.Loaders;

using Gdk;

namespace FSpot.Widgets {
	public class PhotoImageView : ImageView {
#region public API
		public PhotoImageView (IBrowsableCollection query) : this (new BrowsablePointer (query, -1))
		{
		}

		public PhotoImageView (BrowsablePointer item) : base ()
		{
			Accelerometer.OrientationChanged += HandleOrientationChanged;
			Preferences.SettingChanged += OnPreferencesChanged;

			this.item = item;
			item.Changed += HandlePhotoItemChanged;
		}

		new public BrowsablePointer Item {
			get { return item; }
		}

		IBrowsableCollection query;
		public IBrowsableCollection Query {
			get { return item.Collection; }
		}

		public Loupe Loupe {
			get { return loupe; }
		}

		public Gdk.Pixbuf CompletePixbuf ()
		{
			//FIXME: this should be an async call
			if (loader != null)
				while (loader.Loading)
					Gtk.Application.RunIteration ();
			return this.Pixbuf;
		}

		public void Reload ()
		{
			if (Item == null || !Item.IsValid)
				return;
			
			HandlePhotoItemChanged (this, null);
		}

		// Zoom scaled between 0.0 and 1.0
		public double NormalizedZoom {
			get { return (Zoom - MIN_ZOOM) / (MAX_ZOOM - MIN_ZOOM); }
			set { Zoom = (value * (MAX_ZOOM - MIN_ZOOM)) + MIN_ZOOM; }
		}
		
		public event EventHandler PhotoChanged;
#endregion

#region Gtk widgetry
		protected override void OnStyleSet (Gtk.Style previous)
		{
			CheckPattern = new CheckPattern (this.Style.Backgrounds [(int)Gtk.StateType.Normal]);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if ((evnt.State & (ModifierType.Mod1Mask | ModifierType.ControlMask)) != 0)
				return base.OnKeyPressEvent (evnt);

			bool handled = true;
		
			// Scroll if image is zoomed in (scrollbars are visible)
			Gtk.ScrolledWindow scrolled_w = this.Parent as Gtk.ScrolledWindow;
			bool scrolled = scrolled_w != null && !this.Fit;
		
			// Go to the next/previous photo when not zoomed (no scrollbars)
			switch (evnt.Key) {
			case Gdk.Key.Up:
			case Gdk.Key.KP_Up:
			case Gdk.Key.Left:
			case Gdk.Key.KP_Left:
			case Gdk.Key.h:
			case Gdk.Key.H:
			case Gdk.Key.k:
			case Gdk.Key.K:
				if (scrolled)
					handled = false;
				else
					this.Item.MovePrevious ();
				break;
			case Gdk.Key.Page_Up:
			case Gdk.Key.KP_Page_Up:
			case Gdk.Key.BackSpace:
			case Gdk.Key.b:
			case Gdk.Key.B:
				this.Item.MovePrevious ();
				break;
			case Gdk.Key.Down:
			case Gdk.Key.KP_Down:
			case Gdk.Key.Right:
			case Gdk.Key.KP_Right:
			case Gdk.Key.j:
			case Gdk.Key.J:
			case Gdk.Key.l:
			case Gdk.Key.L:
				if (scrolled)
					handled = false;
				else
					this.Item.MoveNext ();
				break;
			case Gdk.Key.Page_Down:
			case Gdk.Key.KP_Page_Down:
			case Gdk.Key.space:
			case Gdk.Key.KP_Space:
			case Gdk.Key.n:
			case Gdk.Key.N:
				this.Item.MoveNext ();
				break;
			case Gdk.Key.Home:
			case Gdk.Key.KP_Home:
				this.Item.Index = 0;
				break;
			case Gdk.Key.r:
			case Gdk.Key.R:
				this.Item.Index = new Random().Next(0, this.Query.Count - 1);
				break;
			case Gdk.Key.End:
			case Gdk.Key.KP_End:
				this.Item.Index = this.Query.Count - 1;
				break;
			default:
				handled = false;
				break;
			}

			return handled || base.OnKeyPressEvent (evnt);
		}

		protected override void OnDestroyed ()
		{
			if (loader != null) {
				loader.AreaUpdated -= HandlePixbufAreaUpdated;
				loader.AreaPrepared -= HandlePixbufPrepared;
				loader.Dispose ();
			}
			base.OnDestroyed ();
		}

		//FIXME: I think OnRealized and OnUnrealized are here for the Loupe to work. If it's true, the Loupe should
		//listen to the Realized/Unrealized events and do its initialization on its own
		protected override void OnRealized ()
		{
			int [] attr = new int [] {
				(int) GdkGlx.GlxAttribute.Rgba,
				(int) GdkGlx.GlxAttribute.DepthSize, 16,
				(int) GdkGlx.GlxAttribute.DoubleBuffer,
				(int) GdkGlx.GlxAttribute.None
			};

			try {
				Glx = new GdkGlx.Context (Screen, attr);
				Colormap = Glx.GetColormap ();
			} catch (GdkGlx.GlxException e) {
				Console.WriteLine ("Error initializing the OpenGL context:{1} {0}", e, Environment.NewLine);
			}

			base.OnRealized ();
		}

		protected override void OnUnrealized ()
		{
			base.OnUnrealized ();

			if (Glx != null)
				Glx.Destroy ();
		}
#endregion

#region loader		
		uint timer;
		IImageLoader loader;
		void Load (Uri uri)
		{
			timer = Log.DebugTimerStart ();
			if (loader != null)
				loader.Dispose ();

			loader = ImageLoader.Create (uri);
			loader.AreaPrepared += HandlePixbufPrepared;
			loader.AreaUpdated += HandlePixbufAreaUpdated;
			loader.Completed += HandleDone;
			loader.Load (uri);
		}

		void HandlePixbufPrepared (object sender, AreaPreparedEventArgs args)
		{
			IImageLoader loader = sender as IImageLoader;
			if (loader != this.loader)
				return;

			if (!ShowProgress)
				return;

			Gdk.Pixbuf prev = this.Pixbuf;
			this.Pixbuf = loader.Pixbuf;
			PixbufOrientation = Accelerometer.GetViewOrientation (loader.PixbufOrientation);
			if (prev != null)
				prev.Dispose ();

			this.ZoomFit (args.ReducedResolution);
		}

		void HandlePixbufAreaUpdated (object sender, AreaUpdatedEventArgs args)
		{
			IImageLoader loader = sender as IImageLoader;
			if (loader != this.loader)
				return;

			if (!ShowProgress)
				return;

			Gdk.Rectangle area = this.ImageCoordsToWindow (args.Area);
			this.QueueDrawArea (area.X, area.Y, area.Width, area.Height);
		}

		void HandleDone (object sender, System.EventArgs args)
		{
			Log.DebugTimerPrint (timer, "Loading image took {0}");
			IImageLoader loader = sender as IImageLoader;
			if (loader != this.loader)
				return;

			Pixbuf prev = this.Pixbuf;
			if (Pixbuf != loader.Pixbuf)
				Pixbuf = loader.Pixbuf;

			if (Pixbuf == null) {
				// FIXME: Do we have test cases for this ???

				// FIXME in some cases the image passes completely through the
				// pixbuf loader without properly loading... I'm not sure what to do about this other
				// than try to load the image one last time.
				try {
					Log.Warning ("Falling back to file loader");
					Pixbuf = PhotoLoader.Load (item.Collection, item.Index);
				} catch (Exception e) {
					LoadErrorImage (e);
				}
			}

			if (loader.Pixbuf != null) //FIXME: this test in case the photo was loaded with the direct loader
				PixbufOrientation = Accelerometer.GetViewOrientation (loader.PixbufOrientation);
			else
				PixbufOrientation = PixbufOrientation.TopLeft;

			if (Pixbuf == null)
				LoadErrorImage (null);
			else
				ZoomFit ();

			progressive_display = true;

			if (prev != this.Pixbuf && prev != null)
				prev.Dispose ();
		}
#endregion
		
		protected BrowsablePointer item;
		protected Loupe loupe;
		protected Loupe sharpener;
		GdkGlx.Context Glx;

		void HandleOrientationChanged (object sender, EventArgs e)
		{
			Reload ();
		}
		
		bool progressive_display = true;
		bool ShowProgress {
			get { return progressive_display; }
		}

		void LoadErrorImage (System.Exception e)
		{
			// FIXME we should check the exception type and do something
			// like offer the user a chance to locate the moved file and
			// update the db entry, but for now just set the error pixbuf	
			Pixbuf old = Pixbuf;
			Pixbuf = new Pixbuf (PixbufUtils.ErrorPixbuf, 0, 0, 
					     PixbufUtils.ErrorPixbuf.Width, 
					     PixbufUtils.ErrorPixbuf.Height);
			if (old != null)
				old.Dispose ();

			PixbufOrientation = PixbufOrientation.TopLeft;
			ZoomFit (false);
		}

		void HandlePhotoItemChanged (object sender, BrowsablePointerChangedEventArgs args) 
		{
			// If it is just the position that changed fall out
			if (args != null && 
			    args.PreviousItem != null &&
			    Item.IsValid &&
			    (args.PreviousIndex != item.Index) &&
			    (this.Item.Current.DefaultVersionUri == args.PreviousItem.DefaultVersionUri))
				return;

			// Don't reload if the image didn't change at all.
			if (args != null && args.Changes != null &&
			    !args.Changes.DataChanged &&
			    args.PreviousItem != null &&
			    Item.IsValid &&
			    this.Item.Current.DefaultVersionUri == args.PreviousItem.DefaultVersionUri)
				return;

			// Same image, don't load it progressively
			if (args != null &&
			    args.PreviousItem != null && 
			    Item.IsValid && 
			    Item.Current.DefaultVersionUri == args.PreviousItem.DefaultVersionUri)
				progressive_display = false;

			try {
				if (Item.IsValid) 
					Load (Item.Current.DefaultVersionUri);
				else
					LoadErrorImage (null);
			} catch (System.Exception e) {
				Log.DebugException (e);
				LoadErrorImage (e);
			}
			
			Selection = Gdk.Rectangle.Zero;

			EventHandler eh = PhotoChanged;
			if (eh != null)
				eh (this, EventArgs.Empty);
		}
		

		private void HandleLoupeDestroy (object sender, EventArgs args)
		{
			if (sender == loupe)
				loupe = null;

			if (sender == sharpener)
				sharpener = null;

		}


		public void ShowHideLoupe ()
		{
			if (loupe == null) {
				loupe = new Loupe (this);
				loupe.Destroyed += HandleLoupeDestroy;
				loupe.Show ();
			} else {
				loupe.Destroy ();	
			}
			
		}
		
		public void ShowSharpener ()
		{
			if (sharpener == null) {
				sharpener = new Sharpener (this);
				sharpener.Destroyed += HandleLoupeDestroy;
			}

			sharpener.Show ();	
		}

		void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void LoadPreference (String key)
		{
			switch (key) {
			case Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE:
			case Preferences.COLOR_MANAGEMENT_ENABLED:
				Reload ();
				break;
			}
		}

		protected override void ApplyColorTransform (Pixbuf pixbuf)
		{
			FSpot.ColorManagement.ApplyScreenProfile (pixbuf);
		}
		
	}
}
