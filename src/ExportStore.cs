using Gdk;
using Gtk;
using System.Collections;
using System.IO;
using System.Data;
using System;
using FSpot;
using FSpot.Database;
using Hyena.Data.Sqlite;

public class ExportItem : DbItem {

    private uint image_id;
    public uint ImageId {
        get { return image_id; }
        set { image_id = value; }
    }

    private uint image_version_id;
    public uint ImageVersionId {
        get { return image_version_id; }
        set { image_version_id = value; }
    }

    private string export_type;
    public string ExportType {
        get { return export_type; }
        set { export_type = value; }
    }

    private string export_token;
    public string ExportToken {
        get { return export_token; }
        set { export_token = value; }
    }

    public ExportItem (uint id, uint image_id, uint image_version_id, string export_type, string export_token) : base (id)
    {
        this.image_id = image_id;
        this.image_version_id = image_version_id;
        this.export_type = export_type;
        this.export_token = export_token;
    }
}

public class ExportStore : DbStore<ExportItem> {
	
	public const string FlickrExportType = "fspot:Flickr";
	public const string OldFolderExportType = "fspot:Folder"; //This is obsolete and meant to be remove once db reach rev4
	public const string FolderExportType = "fspot:FolderUri";
	public const string PicasaExportType = "fspot:Picasa";
	public const string SmugMugExportType = "fspot:SmugMug";
	public const string Gallery2ExportType = "fspot:Gallery2";

	private void CreateTable ()
	{
		Database.Execute (
			"CREATE TABLE exports (\n" +
			"	id			INTEGER PRIMARY KEY NOT NULL, \n" +
			"	image_id		INTEGER NOT NULL, \n" +
			"	image_version_id	INTEGER NOT NULL, \n" +
			"	export_type		TEXT NOT NULL, \n" +
			"	export_token		TEXT NOT NULL\n" +
			")");
	}

	private ExportItem LoadItem (IDataReader reader)
	{
		return new ExportItem (Convert.ToUInt32 (reader["id"]), 
				       Convert.ToUInt32 (reader["image_id"]),
				       Convert.ToUInt32 (reader["image_version_id"]), 
				       reader["export_type"].ToString (), 
				       reader["export_token"].ToString ());
	}
	
	private void LoadAllItems ()
	{
		IDataReader reader = Database.Query("SELECT id, image_id, image_version_id, export_type, export_token FROM exports");

		while (reader.Read ()) {
			AddToCache (LoadItem (reader));
		}

		reader.Close ();
	}

	public ExportItem Create (uint image_id, uint image_version_id, string export_type, string export_token)
	{
		int id = Database.Execute(new HyenaSqliteCommand("INSERT INTO exports (image_id, image_version_id, export_type, export_token) VALUES (?, ?, ?, ?)",
		image_id, image_version_id, export_type, export_token));
		
		ExportItem item = new ExportItem ((uint)id, image_id, image_version_id, export_type, export_token);

		AddToCache (item);
		EmitAdded (item);

		return item;
	}
	
	public override void Commit (ExportItem item)
	{
		Database.Execute(new HyenaSqliteCommand("UPDATE exports SET image_id = ?, image_version_id = ?, export_type = ? SET export_token = ? WHERE id = ?",
                    item.ImageId, item.ImageVersionId, item.ExportType, item.ExportToken, item.Id));
		
		EmitChanged (item);
	}
	
	public override ExportItem Get (uint id)
	{
            // we never use this
            return null;
	}

	public ArrayList GetByImageId (uint image_id, uint image_version_id)
	{
        
		IDataReader reader = Database.Query(new HyenaSqliteCommand("SELECT id, image_id, image_version_id, export_type, export_token FROM exports WHERE image_id = ? AND image_version_id = ?",
                    image_id, image_version_id));
		ArrayList list = new ArrayList ();
		while (reader.Read ()) {
			list.Add (LoadItem (reader));
		}
		reader.Close ();

		return list;
	}
	
	public override void Remove (ExportItem item)
	{
		RemoveFromCache (item);

		Database.Execute(new HyenaSqliteCommand("DELETE FROM exports WHERE id = ?", item.Id));

		EmitRemoved (item);
	}

	// Constructor

	public ExportStore (FSpotDatabaseConnection database, bool is_new)
		: base (database, true)
	{
		if (is_new || !Database.TableExists ("exports"))
			CreateTable ();
		else
			LoadAllItems ();
	}
}
