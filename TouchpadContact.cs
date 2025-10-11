using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RawInput.Touchpad
{
	public struct TouchpadContact : IEquatable<TouchpadContact>
	{
		public int ContactId { get; }
		public int X { get; }
		public int Y { get; }

		// New: physical or logical coordinate ranges
		public int XMin { get; }
		public int XMax { get; }
		public int YMin { get; }
		public int YMax { get; }

		public TouchpadContact(int contactId, int x, int y,
			int xMin = 0, int xMax = 0, int yMin = 0, int yMax = 0)
		{
			ContactId = contactId;
			X = x;
			Y = y;
			XMin = xMin;
			XMax = xMax;
			YMin = yMin;
			YMax = yMax;
		}

		public override bool Equals(object obj) => obj is TouchpadContact other && Equals(other);

		public bool Equals(TouchpadContact other) =>
			ContactId == other.ContactId &&
			X == other.X &&
			Y == other.Y &&
			XMin == other.XMin &&
			XMax == other.XMax &&
			YMin == other.YMin &&
			YMax == other.YMax;

		public static bool operator ==(TouchpadContact a, TouchpadContact b) => a.Equals(b);
		public static bool operator !=(TouchpadContact a, TouchpadContact b) => !a.Equals(b);

		public override int GetHashCode() => (ContactId, X, Y, XMin, XMax, YMin, YMax).GetHashCode();

		public override string ToString() =>
			$"Contact ID:{ContactId} Point:{X},{Y} Range:X[{XMin}-{XMax}] Y[{YMin}-{YMax}]";

		public void WriteTo(BinaryWriter bw)
		{
			bw.Write(ContactId);
			bw.Write(X);
			bw.Write(Y);
			bw.Write(XMin);
			bw.Write(XMax);
			bw.Write(YMin);
			bw.Write(YMax);
		}
	}

	internal class TouchpadContactCreator
	{
		public int? ContactId { get; set; }
		public int? X { get; set; }
		public int? Y { get; set; }

		// New: optional range properties
		public int? XMin { get; set; }
		public int? XMax { get; set; }
		public int? YMin { get; set; }
		public int? YMax { get; set; }

		public bool TryCreate(out TouchpadContact contact)
		{
			if (ContactId.HasValue && X.HasValue && Y.HasValue)
			{
				contact = new TouchpadContact(
					ContactId.Value, X.Value, Y.Value,
					XMin ?? 0, XMax ?? 0, YMin ?? 0, YMax ?? 0
				);
				return true;
			}

			contact = default;
			return false;
		}

		public void Clear()
		{
			ContactId = null;
			X = null;
			Y = null;
			XMin = null;
			XMax = null;
			YMin = null;
			YMax = null;
		}
	}
	}