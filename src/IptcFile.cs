namespace FSpot.Iptc {
	public enum Format
	{
		Unknown,
		String,
		Numeric,
		Binary,
		Byte,
		Short,
		Int,
		Date,
		Time
	};

	public enum Record
	{
		Envelope = 1 << 8,
		Application = 2 << 8,
		NewsphotoParameter = 3 << 8,
		NotAllocated1 = 4 << 8,
		NotAllocated2 = 5 << 8,
		AbstractRelationship = 6 << 8,
		PreObjectData = 7 << 8,
		ObjectData = 8 << 8,
		PostObjectData = 9 << 8
	}

	public enum DataSetID
	{
		ModelVersion        = Record.Envelope | 0,
		Destination         = Record.Envelope | 5,
		FileFormat          = Record.Envelope | 20,
		FileFormatVersion   = Record.Envelope | 22,
		ServiceIdentifier   = Record.Envelope | 30,
		EnvelopeNumber      = Record.Envelope | 40,
		ProductID           = Record.Envelope | 50,
		EnvelopePriority    = Record.Envelope | 60,
		DateSent            = Record.Envelope | 70,
		TimeSent            = Record.Envelope | 80,
		CodedCharacterSet   = Record.Envelope | 90,
		UNO                 = Record.Envelope | 100,
		ARMIdentifier       = Record.Envelope | 120,
		ARMVersion          = Record.Envelope | 122,

		RecordVersion            = Record.Application | 0,
		ObjectTypeReference      = Record.Application | 3,
		ObjectAttributeReference = Record.Application | 4,
		ObjectName               = Record.Application | 5,
		EditStatus               = Record.Application | 8,
		Urgency                  = Record.Application | 10,
		SubjectReference         = Record.Application | 12,
		Category                 = Record.Application | 15,
		SupplementalCategory     = Record.Application | 20,
		FixtureIdentifier        = Record.Application | 22,
		Keywords                 = Record.Application | 25,
		ContentLocationCode      = Record.Application | 26,
		ContentLocationName      = Record.Application | 27,
		ReleaseDate              = Record.Application | 30,
		ReleaseTime              = Record.Application | 35,
		ExpirationDate           = Record.Application | 37,
		ExpirationTime           = Record.Application | 38,
		SpecialInstructions      = Record.Application | 40,
		ActionAdvised            = Record.Application | 42,
		ReferenceService         = Record.Application | 45,
		ReferenceDate            = Record.Application | 47,
		ReferenceNumber          = Record.Application | 50,
		DateCreated              = Record.Application | 55,
		TimeCreated              = Record.Application | 60,
		DigitalCreationDate      = Record.Application | 62,
		DigitalCreationTime      = Record.Application | 63,
		OriginatingProgram       = Record.Application | 65,
		ProgramVersion           = Record.Application | 70,
		ObjectCycle              = Record.Application | 75,
		ByLine                   = Record.Application | 80,
		ByLineTitle              = Record.Application | 85,
		City                     = Record.Application | 90,
		Sublocation              = Record.Application | 92,
		ProvinceState            = Record.Application | 95,
		PrimaryLocationCode      = Record.Application | 100,
		PrimaryLocationName      = Record.Application | 101,
		OriginalTransmissionReference = Record.Application | 103,
		Headline                 = Record.Application | 105,
		Credit                   = Record.Application | 110,
		Source                   = Record.Application | 115,
		CopyrightNotice          = Record.Application | 116,
		Contact                  = Record.Application | 118,
		CaptionAbstract          = Record.Application | 120,
		WriterEditor             = Record.Application | 122,
		RasterizedCaption        = Record.Application | 125,
		ImageType                = Record.Application | 130,
		ImageOrientation         = Record.Application | 131,
		LanguageIdentifier       = Record.Application | 135,
		AudioType                = Record.Application | 150,
		AudioSamplingRate        = Record.Application | 151,
		AudioSamplingReduction   = Record.Application | 152,
		AudioDuration            = Record.Application | 153,
		AudioOutcue              = Record.Application | 154,
		ObjectDataPreviewFileFormat = Record.Application | 200,
		ObjectDataPreviewFileFormatVersion  = Record.Application | 201,
		ObjectDataPreviewData    = Record.Application | 202,
		
		SizeMode                 = Record.PreObjectData | 10,
		MaxSubfileSize           = Record.PreObjectData | 20,
		ObjectDataSizeAnnounced  = Record.PreObjectData | 90,
		MaximumObjectDataSize    = Record.PreObjectData | 95,

		Subfile                  = Record.ObjectData | 10,

		ConfirmedObjectDataSize  = Record.PostObjectData | 10
	}

	public class DataSetInfo 
	{
		DataSetID ID;
		public string Name;
		public string Description;
		bool Mandatory;
		bool Repeatable;
		uint MinSize;
		uint MaxSize;
		Format Format;
		
		private static DataSetInfo [] datasets = {
			new DataSetInfo (DataSetID.ModelVersion, Format.Short, "Model Version", true, false, 2, 2, 
					 Mono.Posix.Catalog.GetString ("IPTC Information Interchange Model (IIM) Version number")),
			new DataSetInfo (DataSetID.Destination, Format.String, "Destination", false, true, 0, 1024, 
					 Mono.Posix.Catalog.GetString ("OSI Destination routing information")),
			new DataSetInfo (DataSetID.FileFormat, Format.Short, "File Format", true, false, 2, 2, 
					 Mono.Posix.Catalog.GetString ("IPTC file format")),
			new DataSetInfo (DataSetID.ServiceIdentifier, Format.String, "Service Identifier", true, false, 0, 10, 
					 Mono.Posix.Catalog.GetString ("Identifies the provider and product")),
			new DataSetInfo (DataSetID.EnvelopeNumber, Format.Numeric, "Envelope Number", true, false, 8, 8, 
					 Mono.Posix.Catalog.GetString ("A unique number identifying the envelope")), // FIXME
			new DataSetInfo (DataSetID.ProductID, Format.Numeric, "Product I.D.", false, true, 0, 32, 
					 Mono.Posix.Catalog.GetString ("A unique number")), // FIXME
			new DataSetInfo (DataSetID.EnvelopePriority, Format.Numeric, "Envelope Priority", false, false, 1, 1, 
					 Mono.Posix.Catalog.GetString ("The envelope handling priority between 1 (most urgent) and 9 (least urgent)")),
			new DataSetInfo (DataSetID.DateSent, Format.Date, "Date Sent", true, false, 8, 8, 
					 Mono.Posix.Catalog.GetString ("The year month and day (CCYYMMDD) the service sent the material")),
			new DataSetInfo (DataSetID.TimeSent, Format.Date, "Time Sent", false, false, 11, 11, 
					 Mono.Posix.Catalog.GetString ("The hour minute and second the (HHMMSS+HHMM) the service sent the material")),
			new DataSetInfo (DataSetID.CodedCharacterSet, Format.Time, "Coded Character Set", false, false, 0, 32, 
					 Mono.Posix.Catalog.GetString ("The character set designation")), // FIXME
			new DataSetInfo (DataSetID.UNO, Format.String, "Unique Name of Object", false, false, 14, 80,
					 Mono.Posix.Catalog.GetString ("External globally unique object identifier")),
			// UCD : IPR  : ODE            : OVI
			//   8 :[1-32]:[61 - IPR count]:[1-9]

			new DataSetInfo (DataSetID.ARMIdentifier, Format.Short, "ARM Identifier", false, false, 2, 2,
					 Mono.Posix.Catalog.GetString ("Abstract Relationship Method (ARM) identifier")),
			new DataSetInfo (DataSetID.ARMVersion, Format.Short, "ARM Version", false, false, 2, 2,
					 Mono.Posix.Catalog.GetString ("Abstract Relationship Method (ARM) version number.")),
			
			new DataSetInfo (DataSetID.RecordVersion, Format.Short, "Record Version", false, false, 2, 2,
					 Mono.Posix.Catalog.GetString ("Number identifying the IIM version this application record uses")),
			new DataSetInfo (DataSetID.ObjectTypeReference, Format.String, "Object Type Reference", false, false, 3, 64,
					 Mono.Posix.Catalog.GetString ("Number identifying the IIM version this application record uses")),
			// Object Type Number : Object Type Name
			//                  2 : [0-64]
		};

		public static DataSetInfo FindInfo (DataSetID id)
		{
			foreach (DataSetInfo info in datasets)
				if (id == (DataSetID)info.ID)
					return info;
						
			return new DataSetInfo (id, Format.Unknown, "Unknown", false, false, 3, 64,
						Mono.Posix.Catalog.GetString ("Unkown IIM DataSet"));
		}

		protected DataSetInfo (DataSetID id, Format format, string name, bool mandatory, bool repeatable, uint min, uint max, string description)
		{
			ID = id;
			Name = name;
			Description = description;
			Format = format;
		        Mandatory = mandatory;
			Repeatable = repeatable;
			MinSize = min;
			MaxSize = max;
		}
	}

	public class DataSet 
	{
		public byte RecordNumber;
		public byte DataSetNumber;
		public byte [] Data;
		
		const byte TagMarker = 0x1c;
		const ushort LengthMask = 1 << 15;

		public void Load (System.IO.Stream stream)
		{
			byte [] rec = new byte [5];
			stream.Read (rec, 0, rec.Length);
			if (rec [0] != TagMarker)
				throw new System.Exception (System.String.Format ("Invalid tag marker found {0} != 0x1c", 
							    TagMarker.ToString ("x")));
			
			RecordNumber = rec [1];
			DataSetNumber = rec [2];

			ulong length = FSpot.BitConverter.ToUInt16 (rec, 3, false);			

			if ((length & (LengthMask)) > 0) {
				// Note: if the high bit of the length is set the record is more than 32k long
				// and the length is stored in what would normaly be the record data, so we read
				// that data convert it to a long and continue on.
				ushort lsize = (ushort)((ushort)length & ~LengthMask);
				if (lsize > 8)
					throw new System.Exception ("Wow, that is a lot of data");

				byte [] ldata = new byte [8];
				stream.Read (ldata, 8 - lsize, lsize);
				length = FSpot.BitConverter.ToUInt64 (ldata, 0, false);
			}

			// FIXME if the length is greater than 32768 we re
			Data = new byte [length];
			stream.Read (Data, 0, Data.Length);
		}

		public DataSetID ID {
			get {
				return (DataSetID) (RecordNumber << 8 | DataSetNumber);
			}
		}
		
		public void Save (System.IO.Stream stream)
		{
			stream.WriteByte (TagMarker);
			stream.WriteByte (RecordNumber);
			stream.WriteByte (DataSetNumber);
			if (Data.Length < LengthMask) {
				byte [] len = FSpot.BitConverter.GetBytes ((ushort)Data.Length, false);
				stream.Write (len, 0, len.Length);
			} else {
				byte [] len =  FSpot.BitConverter.GetBytes ((ushort)LengthMask & 8, false);
				stream.Write (len, 0, len.Length);
				len = FSpot.BitConverter.GetBytes ((ulong) Data.Length, false);
				stream.Write (len, 0, len.Length);
			}
			stream.Write (Data, 0, Data.Length);
		}
	}

	public class IptcFile 
	{
		System.Collections.ArrayList sets = new System.Collections.ArrayList ();
		
		public IptcFile (System.IO.Stream stream)
		{
			Load (stream);
		}
		
		public void Load (System.IO.Stream stream)
		{
			while (stream.Position < stream.Length) {
				DataSet data = new DataSet ();
				data.Load (stream);
				DataSetInfo info = DataSetInfo.FindInfo (data.ID);
				System.Console.WriteLine ("{0}:{1} - {2} {3}", data.RecordNumber, data.DataSetNumber, 
							  data.ID.ToString (), info.Description);
				sets.Add (data);
			}
		}

		public void Save (System.IO.Stream stream) 
		{
			foreach (DataSet data in sets) {
				data.Save (stream);
			}
		}
	}
}
