using Gtk;
using Gdk;
using System;
using System.Runtime.InteropServices;

public class StockIcons {
	public static void Initialize ()
	{
		Gtk.StockItem [] stock_items = {	
			new Gtk.StockItem ("f-spot-browse", "Browse", 0, Gdk.ModifierType.ShiftMask, null),
			new Gtk.StockItem ("f-spot-camera", "Camera", 0, Gdk.ModifierType.ShiftMask, null),
			new Gtk.StockItem ("f-spot-crop", "Crop", 0, Gdk.ModifierType.ShiftMask, null),
			new Gtk.StockItem ("f-spot-edit-image", "Edit Image", 0, Gdk.ModifierType.ShiftMask, null),
			new Gtk.StockItem ("f-spot-fullscreen", "Fullscreen", 0, Gdk.ModifierType.ShiftMask, null),
			new Gtk.StockItem ("f-spot-slideshow", "Slideshow", 0, Gdk.ModifierType.ShiftMask, null),
			new Gtk.StockItem ("f-spot-logo", "Logo", 0, Gdk.ModifierType.ShiftMask, null),
			new Gtk.StockItem ("f-spot-question-mark", "Question", 0, Gdk.ModifierType.ShiftMask, null),
			new Gtk.StockItem ("f-spot-rotate-270", "Rotate _Left", 0, Gdk.ModifierType.ShiftMask, null),
			new Gtk.StockItem ("f-spot-rotate-90", "Rotate _Right", 0, Gdk.ModifierType.ShiftMask, null),
			new Gtk.StockItem ("f-spot-loading", "Loading", 0, Gdk.ModifierType.ShiftMask, null)
		};
		
		IconFactory icon_factory = new IconFactory ();
		icon_factory.AddDefault ();

		foreach (Gtk.StockItem item in stock_items) {
			Pixbuf pixbuf = PixbufUtils.LoadFromAssembly (item.StockId + ".png");
			IconSet icon_set = new IconSet (pixbuf);
			icon_factory.Add (item.StockId, icon_set);
		}

		Gtk.StockManager.Add (stock_items);
	}
}
