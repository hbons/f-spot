/* 
 * ItemAction.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */
using Gtk;
using Mono.Unix;
using FSpot.Filters;
using System;

namespace FSpot {
	public abstract class ItemAction : Action {
		protected BrowsablePointer item;
		
		public ItemAction (BrowsablePointer pointer,
				   string name,
				   string label,
				   string tooltip,
				   string stock_id) : base (name, label, tooltip, stock_id)
		{
			item = pointer;
			item.Changed += ItemChanged;
		}

	        protected virtual void ItemChanged (BrowsablePointer sender, 
						    BrowsablePointerChangedArgs args)
		{
			Sensitive = item.IsValid;
		}
	}

	public class RotateAction : ItemAction {
		protected RotateDirection direction;
		
		public RotateAction (BrowsablePointer pointer,
				     RotateDirection direction,
				     string name,
				     string label,
				     string tooltip,
				     string stock_id) 
			: base (pointer, name, label, tooltip, stock_id)
		{
			this.direction = direction;
		}

		protected override void OnActivated ()
		{
			RotateOperation op = new RotateOperation (item.Current, direction);

			while (op.Step ());

			item.Collection.MarkChanged (item.Index);
		}
	}

	public class RotateLeftAction : RotateAction {
		public RotateLeftAction (BrowsablePointer p) 
			: base (p,
				RotateDirection.Counterclockwise,
				"RotateItemLeft", 
				Catalog.GetString ("Rotate Left"), 
				Catalog.GetString ("Rotate picture left"),
				"f-spot-rotate-270")
		{
		}
	}

	public class RotateRightAction : RotateAction {
		public RotateRightAction (BrowsablePointer p) 
			: base (p,
				RotateDirection.Clockwise,
				"RotateItemRight", 
				Catalog.GetString ("Rotate Right"), 
				Catalog.GetString ("Rotate picture left"),
				"f-spot-rotate-90")
		{
		}
	}

	public class NextPictureAction : ItemAction {
		public NextPictureAction (BrowsablePointer p)
			: base (p,
				"NextPicture",
				Catalog.GetString ("Next"),
				Catalog.GetString ("Next picture"),
				Stock.GoForward)
		{
		}

		protected override void ItemChanged (BrowsablePointer p,
						     BrowsablePointerChangedArgs args)
		{
			Sensitive = item.Index < item.Collection.Count -1;
		}
		
		protected override void OnActivated ()
		{
			item.MoveNext ();
		}
	}

	public class PreviousPictureAction : ItemAction {
		public PreviousPictureAction (BrowsablePointer p)
			: base (p,
				"PreviousPicture",
				Catalog.GetString ("Previous"),
				Catalog.GetString ("Previous picture"),
				Stock.GoBack)
		{
		}

		protected override void ItemChanged (BrowsablePointer p,
						     BrowsablePointerChangedArgs args)
		{
			Sensitive =  item.Index > 0;
		}
		
		protected override void OnActivated ()
		{
			item.MovePrevious ();
		}
	}

	// FIXME this class is a hack to work around the considerable brokeness
	// in the gaps between Photo and IBrowsable* It helps but it shouldn't
	// be so introspective.
	internal class EditTarget {
		BrowsablePointer item;
		bool created_version = false;
		uint version;
		Photo photo;
		
		public EditTarget (BrowsablePointer item)
		{
			this.item = item;
			photo = item.Current as Photo;
			if (photo != null) {
				version = photo.DefaultVersionId;
				bool create = version == Photo.OriginalVersionId;
				
				if (create) {
					version = photo.CreateDefaultModifiedVersion (photo.DefaultVersionId, false);
					created_version = true;
				}
			}
		}
		
		public Uri Uri {
			get {
				if (photo != null)
					return photo.VersionUri (version);
				else 
					return item.Current.DefaultVersionUri;
			}
		}
		
		public void Commit ()
		{
			PhotoQuery q = item.Collection as PhotoQuery;
			if (photo != null && q != null) {
				photo.DefaultVersionId = version;
				q.Commit (item.Index);
			} else {
				item.Collection.MarkChanged (item.Index);
			}
		}
		
		public void Delete ()
		{
			if (created_version)
				photo.DeleteVersion (version);
		}
		
	}
	
	public class FilterAction : ItemAction {
		public FilterAction (BrowsablePointer pointer,
				     string name,
				     string label,
				     string tooltip,
				     string stock_id) : base (pointer, name, label, tooltip, stock_id)
		{
		}

		protected virtual IFilter BuildFilter ()
		{
			throw new ApplicationException ("No filter specified");
		}
		

		protected override void OnActivated ()
		{
			if (!item.IsValid)
				throw new ApplicationException ("attempt to filter invalid item");
			
		        using (FilterRequest req = new FilterRequest (item.Current.DefaultVersionUri)) {
				IFilter filter = BuildFilter ();
				if (filter.Convert (req)) {
					// The filter did something so lets ve
					EditTarget target = new EditTarget (item);
					
					Gnome.Vfs.Result result = Gnome.Vfs.Result.Ok;
					result = Gnome.Vfs.Xfer.XferUri (new Gnome.Vfs.Uri (req.Current.ToString ()),
									 new Gnome.Vfs.Uri (target.Uri.ToString ()),
									 Gnome.Vfs.XferOptions.Default,
									 Gnome.Vfs.XferErrorMode.Abort, 
									 Gnome.Vfs.XferOverwriteMode.Replace, 
									 delegate {
										 System.Console.WriteLine ("progress");
										 return 1;
									 });
					
					if (result == Gnome.Vfs.Result.Ok) {
						System.Console.WriteLine ("Done modifying image");
						target.Commit ();
					} else {
						target.Delete ();
						throw new ApplicationException (String.Format (
											       "{0}: error moving to destination {1}",
											       this, target.ToString ()));
					}
				}
			}
		}
	}

	public class AutoColor : FilterAction {
		public AutoColor (BrowsablePointer p)
			: base (p, "Color", 
				Catalog.GetString ("Auto Color"),
				Catalog.GetString ("Automatically adjust the colors"),
				"f-spot-sepia")
		{
		}

		protected override IFilter BuildFilter ()
		{
			return new AutoStretch ();
		}
	}
}
