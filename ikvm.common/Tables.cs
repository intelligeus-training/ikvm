/*
  Copyright (C) 2009-2012 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/
using System;
using System.Collections.Generic;
using System.Text;
using IKVM.Reflection.Emit;
using IKVM.Reflection.Writer;
using IKVM.Reflection.Reader;

namespace IKVM.Reflection.Metadata
{
	public abstract class Table
	{
		public bool Sorted;

		public bool IsBig => RowCount > 65535;

		public abstract int RowCount { get; set; }

		public abstract void Write(MetadataWriter mw);
		public abstract void Read(MetadataReader mr);

		public int GetLength(MetadataWriter md)
		{
			return RowCount * GetRowSize(new RowSizeCalc(md));
		}

		protected abstract int GetRowSize(RowSizeCalc rsc);

		protected sealed class RowSizeCalc
		{
			private readonly MetadataWriter mw;
			private int size;

			public RowSizeCalc(MetadataWriter mw)
			{
				this.mw = mw;
			}

			public RowSizeCalc AddFixed(int size)
			{
				this.size += size;
				return this;
			}

			public RowSizeCalc WriteStringIndex()
			{
				if (mw.bigStrings)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteGuidIndex()
			{
				if (mw.bigGuids)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteBlobIndex()
			{
				if (mw.bigBlobs)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteTypeDefOrRef()
			{
				if (mw.bigTypeDefOrRef)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteField()
			{
				if (mw.bigField)
				{
					size += 4;
				}
				else
				{
					size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteMethodDef()
			{
				if (mw.bigMethodDef)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteParam()
			{
				if (mw.bigParam)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteResolutionScope()
			{
				if (mw.bigResolutionScope)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteMemberRefParent()
			{
				if (mw.bigMemberRefParent)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteHasCustomAttribute()
			{
				if (mw.bigHasCustomAttribute)
				{
					size += 4;
				}
				else
				{
					size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteCustomAttributeType()
			{
				if (mw.bigCustomAttributeType)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteHasConstant()
			{
				if (mw.bigHasConstant)
				{
					size += 4;
				}
				else
				{
					size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteTypeDef()
			{
				if (mw.bigTypeDef)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteMethodDefOrRef()
			{
				if (mw.bigMethodDefOrRef)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteEvent()
			{
				if (mw.bigEvent)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteProperty()
			{
				if (mw.bigProperty)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteHasSemantics()
			{
				if (mw.bigHasSemantics)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteImplementation()
			{
				if (mw.bigImplementation)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteTypeOrMethodDef()
			{
				if (mw.bigTypeOrMethodDef)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteGenericParam()
			{
				if (mw.bigGenericParam)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteHasDeclSecurity()
			{
				if (mw.bigHasDeclSecurity)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteMemberForwarded()
			{
				if (mw.bigMemberForwarded)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteModuleRef()
			{
				if (mw.bigModuleRef)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public RowSizeCalc WriteHasFieldMarshal()
			{
				if (mw.bigHasFieldMarshal)
				{
					this.size += 4;
				}
				else
				{
					this.size += 2;
				}
				return this;
			}

			public int Value
			{
				get { return size; }
			}
		}
	}

	public abstract class Table<T> : Table
	{
		public T[] records = Empty<T>.Array;
		protected int rowCount;

		public sealed override int RowCount
		{
			get { return rowCount; }
			set { rowCount = value; records = new T[value]; }
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			throw new InvalidOperationException();
		}

		public int AddRecord(T newRecord)
		{
			if (rowCount == records.Length)
			{
				Array.Resize(ref records, Math.Max(16, records.Length * 2));
			}
			records[rowCount++] = newRecord;
			return rowCount;
		}

		public int AddVirtualRecord()
		{
			return ++rowCount;
		}

		public override void Write(MetadataWriter mw)
		{
			throw new InvalidOperationException();
		}
	}

	public abstract class SortedTable<T> : Table<T>
		where T : SortedTable<T>.IRecord
	{
		public interface IRecord
		{
			int SortKey { get; }
			int FilterKey { get; }
		}

		public struct Enumerable
		{
			private readonly SortedTable<T> table;
			private readonly int token;

			public Enumerable(SortedTable<T> table, int token)
			{
				this.table = table;
				this.token = token;
			}

			public Enumerator GetEnumerator()
			{
				T[] records = table.records;
				if (!table.Sorted)
				{
					return new Enumerator(records, table.RowCount - 1, -1, token);
				}
				int index = BinarySearch(records, table.RowCount, token & 0xFFFFFF);
				if (index < 0)
				{
					return new Enumerator(null, 0, 1, -1);
				}
				int start = index;
				while (start > 0 && (records[start - 1].FilterKey & 0xFFFFFF) == (token & 0xFFFFFF))
				{
					start--;
				}
				int end = index;
				int max = table.RowCount - 1;
				while (end < max && (records[end + 1].FilterKey & 0xFFFFFF) == (token & 0xFFFFFF))
				{
					end++;
				}
				return new Enumerator(records, end, start - 1, token);
			}

			private static int BinarySearch(T[] records, int length, int maskedToken)
			{
				int min = 0;
				int max = length - 1;
				while (min <= max)
				{
					int mid = min + ((max - min) / 2);
					int maskedValue = records[mid].FilterKey & 0xFFFFFF;
					if (maskedToken == maskedValue)
					{
						return mid;
					}
					else if (maskedToken < maskedValue)
					{
						max = mid - 1;
					}
					else
					{
						min = mid + 1;
					}
				}
				return -1;
			}
		}

		public struct Enumerator
		{
			private readonly T[] records;
			private readonly int token;
			private readonly int max;
			private int index;

			public Enumerator(T[] records, int max, int index, int token)
			{
				this.records = records;
				this.token = token;
				this.max = max;
				this.index = index;
			}

			public int Current
			{
				get { return index; }
			}

			public bool MoveNext()
			{
				while (index < max)
				{
					index++;
					if (records[index].FilterKey == token)
					{
						return true;
					}
				}
				return false;
			}
		}

		public Enumerable Filter(int token)
		{
			return new Enumerable(this, token);
		}

		protected void Sort()
		{
			ulong[] map = new ulong[rowCount];
			for (uint i = 0; i < map.Length; i++)
			{
				map[i] = ((ulong)records[i].SortKey << 32) | i;
			}
			Array.Sort(map);
			T[] newRecords = new T[rowCount];
			for (int i = 0; i < map.Length; i++)
			{
				newRecords[i] = records[(int)map[i]];
			}
			records = newRecords;
		}
	}

	public sealed class ModuleTable : Table<ModuleTable.Record>
	{
		internal const int Index = 0x00;

		internal struct Record
		{
			internal short Generation;
			internal int Name; // -> StringHeap
			internal int Mvid; // -> GuidHeap
			internal int EncId; // -> GuidHeap
			internal int EncBaseId; // -> GuidHeap
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Generation = mr.ReadInt16();
				records[i].Name = mr.ReadStringIndex();
				records[i].Mvid = mr.ReadGuidIndex();
				records[i].EncId = mr.ReadGuidIndex();
				records[i].EncBaseId = mr.ReadGuidIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].Generation);
				mw.WriteStringIndex(records[i].Name);
				mw.WriteGuidIndex(records[i].Mvid);
				mw.WriteGuidIndex(records[i].EncId);
				mw.WriteGuidIndex(records[i].EncBaseId);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(2)
				.WriteStringIndex()
				.WriteGuidIndex()
				.WriteGuidIndex()
				.WriteGuidIndex()
				.Value;
		}

		internal void Add(short generation, int name, int mvid, int encid, int encbaseid)
		{
			Record record = new Record();
			record.Generation = generation;
			record.Name = name;
			record.Mvid = mvid;
			record.EncId = encid;
			record.EncBaseId = encbaseid;
			AddRecord(record);
		}
	}

	public sealed class TypeRefTable : Table<TypeRefTable.Record>
	{
		internal const int Index = 0x01;

		internal struct Record
		{
			internal int ResolutionScope;
			internal int TypeName;
			internal int TypeNamespace;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].ResolutionScope = mr.ReadResolutionScope();
				records[i].TypeName = mr.ReadStringIndex();
				records[i].TypeNamespace = mr.ReadStringIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteResolutionScope(records[i].ResolutionScope);
				mw.WriteStringIndex(records[i].TypeName);
				mw.WriteStringIndex(records[i].TypeNamespace);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteResolutionScope()
				.WriteStringIndex()
				.WriteStringIndex()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				moduleBuilder.FixupPseudoToken(ref records[i].ResolutionScope);
			}
		}
	}

	public sealed class TypeDefTable : Table<TypeDefTable.Record>
	{
		internal const int Index = 0x02;

		internal struct Record
		{
			internal int Flags;
			internal int TypeName;
			internal int TypeNamespace;
			internal int Extends;
			internal int FieldList;
			internal int MethodList;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Flags = mr.ReadInt32();
				records[i].TypeName = mr.ReadStringIndex();
				records[i].TypeNamespace = mr.ReadStringIndex();
				records[i].Extends = mr.ReadTypeDefOrRef();
				records[i].FieldList = mr.ReadField();
				records[i].MethodList = mr.ReadMethodDef();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			mw.ModuleBuilder.WriteTypeDefTable(mw);
		}

		public int AllocToken()
		{
			return 0x02000000 + AddVirtualRecord();
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(4)
				.WriteStringIndex()
				.WriteStringIndex()
				.WriteTypeDefOrRef()
				.WriteField()
				.WriteMethodDef()
				.Value;
		}
	}

	public sealed class FieldPtrTable : Table<int>
	{
		internal const int Index = 0x03;

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i] = mr.ReadField();
			}
		}
	}

	public sealed class FieldTable : Table<FieldTable.Record>
	{
		internal const int Index = 0x04;

		internal struct Record
		{
			internal short Flags;
			internal int Name;
			internal int Signature;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Flags = mr.ReadInt16();
				records[i].Name = mr.ReadStringIndex();
				records[i].Signature = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			mw.ModuleBuilder.WriteFieldTable(mw);
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(2)
				.WriteStringIndex()
				.WriteBlobIndex()
				.Value;
		}
	}

	public sealed class MethodPtrTable : Table<int>
	{
		internal const int Index = 0x05;

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i] = mr.ReadMethodDef();
			}
		}
	}

	public sealed class MethodDefTable : Table<MethodDefTable.Record>
	{
		internal const int Index = 0x06;
		private int baseRVA;

		internal struct Record
		{
			internal int RVA;
			internal short ImplFlags;
			internal short Flags;
			internal int Name;
			internal int Signature;
			internal int ParamList;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].RVA = mr.ReadInt32();
				records[i].ImplFlags = mr.ReadInt16();
				records[i].Flags = mr.ReadInt16();
				records[i].Name = mr.ReadStringIndex();
				records[i].Signature = mr.ReadBlobIndex();
				records[i].ParamList = mr.ReadParam();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			mw.ModuleBuilder.WriteMethodDefTable(baseRVA, mw);
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(8)
				.WriteStringIndex()
				.WriteBlobIndex()
				.WriteParam()
				.Value;
		}

		public void Fixup(TextSection code)
		{
			baseRVA = (int)code.MethodBodiesRVA;
		}
	}

	public sealed class ParamPtrTable : Table<int>
	{
		internal const int Index = 0x07;

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i] = mr.ReadParam();
			}
		}
	}

	public sealed class ParamTable : Table<ParamTable.Record>
	{
		internal const int Index = 0x08;

		public struct Record
		{
			internal short Flags;
			internal short Sequence;
			internal int Name;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Flags = mr.ReadInt16();
				records[i].Sequence = mr.ReadInt16();
				records[i].Name = mr.ReadStringIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			mw.ModuleBuilder.WriteParamTable(mw);
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(4)
				.WriteStringIndex()
				.Value;
		}
	}

	public sealed class InterfaceImplTable : SortedTable<InterfaceImplTable.Record>
	{
		public const int Index = 0x09;

		public struct Record : IRecord
		{
			internal int Class;
			internal int Interface;

			int IRecord.SortKey
			{
				get { return Class; }
			}

			int IRecord.FilterKey
			{
				get { return Class; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Class = mr.ReadTypeDef();
				records[i].Interface = mr.ReadTypeDefOrRef();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteTypeDef(records[i].Class);
				mw.WriteEncodedTypeDefOrRef(records[i].Interface);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteTypeDef()
				.WriteTypeDefOrRef()
				.Value;
		}

		public void Fixup()
		{
			for (int i = 0; i < rowCount; i++)
			{
				int token = records[i].Interface;
				switch (token >> 24)
				{
					case 0:
						break;
					case TypeDefTable.Index:
						token = (token & 0xFFFFFF) << 2 | 0;
						break;
					case TypeRefTable.Index:
						token = (token & 0xFFFFFF) << 2 | 1;
						break;
					case TypeSpecTable.Index:
						token = (token & 0xFFFFFF) << 2 | 2;
						break;
					default:
						throw new InvalidOperationException();
				}
				records[i].Interface = token;
			}
			// LAMESPEC the CLI spec says that InterfaceImpl should be sorted by { Class, Interface },
			// but it appears to only be necessary to sort by Class (and csc emits InterfaceImpl records in
			// source file order, so to be able to support round tripping, we need to retain ordering as well).
			Sort();
		}
	}

	public sealed class MemberRefTable : Table<MemberRefTable.Record>
	{
		public const int Index = 0x0A;

		internal struct Record
		{
			internal int Class;
			internal int Name;
			internal int Signature;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Class = mr.ReadMemberRefParent();
				records[i].Name = mr.ReadStringIndex();
				records[i].Signature = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteMemberRefParent(records[i].Class);
				mw.WriteStringIndex(records[i].Name);
				mw.WriteBlobIndex(records[i].Signature);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteMemberRefParent()
				.WriteStringIndex()
				.WriteBlobIndex()
				.Value;
		}

		public int FindOrAddRecord(Record record)
		{
			for (int i = 0; i < rowCount; i++)
			{
				if (records[i].Class == record.Class
					&& records[i].Name == record.Name
					&& records[i].Signature == record.Signature)
				{
					return i + 1;
				}
			}
			return AddRecord(record);
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				moduleBuilder.FixupPseudoToken(ref records[i].Class);
			}
		}
	}

	public sealed class ConstantTable : SortedTable<ConstantTable.Record>
	{
		public const int Index = 0x0B;

		internal struct Record : IRecord
		{
			public short Type;
			public int Parent;
			public int Value;

			int IRecord.SortKey
			{
				get { return EncodeHasConstant(Parent); }
			}

			int IRecord.FilterKey
			{
				get { return Parent; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Type = mr.ReadInt16();
				records[i].Parent = mr.ReadHasConstant();
				records[i].Value = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].Type);
				mw.WriteHasConstant(records[i].Parent);
				mw.WriteBlobIndex(records[i].Value);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(2)
				.WriteHasConstant()
				.WriteBlobIndex()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				moduleBuilder.FixupPseudoToken(ref records[i].Parent);
			}
			Sort();
		}

		public static int EncodeHasConstant(int token)
		{
			switch (token >> 24)
			{
				case FieldTable.Index:
					return (token & 0xFFFFFF) << 2 | 0;
				case ParamTable.Index:
					return (token & 0xFFFFFF) << 2 | 1;
				case PropertyTable.Index:
					return (token & 0xFFFFFF) << 2 | 2;
				default:
					throw new InvalidOperationException();
			}
		}

		public object GetRawConstantValue(Module module, int parent)
		{
			foreach (int i in Filter(parent))
			{
				ByteReader br = module.GetBlob(module.Constant.records[i].Value);
				switch (module.Constant.records[i].Type)
				{
					// see ModuleBuilder.AddConstant for the encodings
					case Signature.ELEMENT_TYPE_BOOLEAN:
						return br.ReadByte() != 0;
					case Signature.ELEMENT_TYPE_I1:
						return br.ReadSByte();
					case Signature.ELEMENT_TYPE_I2:
						return br.ReadInt16();
					case Signature.ELEMENT_TYPE_I4:
						return br.ReadInt32();
					case Signature.ELEMENT_TYPE_I8:
						return br.ReadInt64();
					case Signature.ELEMENT_TYPE_U1:
						return br.ReadByte();
					case Signature.ELEMENT_TYPE_U2:
						return br.ReadUInt16();
					case Signature.ELEMENT_TYPE_U4:
						return br.ReadUInt32();
					case Signature.ELEMENT_TYPE_U8:
						return br.ReadUInt64();
					case Signature.ELEMENT_TYPE_R4:
						return br.ReadSingle();
					case Signature.ELEMENT_TYPE_R8:
						return br.ReadDouble();
					case Signature.ELEMENT_TYPE_CHAR:
						return br.ReadChar();
					case Signature.ELEMENT_TYPE_STRING:
						{
							char[] chars = new char[br.Length / 2];
							for (int j = 0; j < chars.Length; j++)
							{
								chars[j] = br.ReadChar();
							}
							return new String(chars);
						}
					case Signature.ELEMENT_TYPE_CLASS:
						if (br.ReadInt32() != 0)
						{
							throw new BadImageFormatException();
						}
						return null;
					default:
						throw new BadImageFormatException();
				}
			}
			throw new InvalidOperationException();
		}
	}

	public sealed class CustomAttributeTable : SortedTable<CustomAttributeTable.Record>
	{
		public const int Index = 0x0C;

		public struct Record : IRecord
		{
			public int Parent;
			public int Type;
			public int Value;

			int IRecord.SortKey
			{
				get { return EncodeHasCustomAttribute(Parent); }
			}

			int IRecord.FilterKey
			{
				get { return Parent; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Parent = mr.ReadHasCustomAttribute();
				records[i].Type = mr.ReadCustomAttributeType();
				records[i].Value = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteHasCustomAttribute(records[i].Parent);
				mw.WriteCustomAttributeType(records[i].Type);
				mw.WriteBlobIndex(records[i].Value);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteHasCustomAttribute()
				.WriteCustomAttributeType()
				.WriteBlobIndex()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			int[] genericParamFixup = moduleBuilder.GenericParam.GetIndexFixup();
			for (int i = 0; i < rowCount; i++)
			{
				moduleBuilder.FixupPseudoToken(ref records[i].Type);
				moduleBuilder.FixupPseudoToken(ref records[i].Parent);
				if (records[i].Parent >> 24 == GenericParamTable.Index)
				{
					records[i].Parent = (GenericParamTable.Index << 24) + genericParamFixup[(records[i].Parent & 0xFFFFFF) - 1] + 1;
				}
				// TODO if we ever add support for custom attributes on DeclSecurity or GenericParamConstraint
				// we need to fix them up here (because they are sorted tables, like GenericParam)
			}
			Sort();
		}

		public static int EncodeHasCustomAttribute(int token)
		{
			switch (token >> 24)
			{
				case MethodDefTable.Index:
					return (token & 0xFFFFFF) << 5 | 0;
				case FieldTable.Index:
					return (token & 0xFFFFFF) << 5 | 1;
				case TypeRefTable.Index:
					return (token & 0xFFFFFF) << 5 | 2;
				case TypeDefTable.Index:
					return (token & 0xFFFFFF) << 5 | 3;
				case ParamTable.Index:
					return (token & 0xFFFFFF) << 5 | 4;
				case InterfaceImplTable.Index:
					return (token & 0xFFFFFF) << 5 | 5;
				case MemberRefTable.Index:
					return (token & 0xFFFFFF) << 5 | 6;
				case ModuleTable.Index:
					return (token & 0xFFFFFF) << 5 | 7;
				// LAMESPEC spec calls this Permission table
				case DeclSecurityTable.Index:
					//return (token & 0xFFFFFF) << 5 | 8;
					throw new NotImplementedException();
				case PropertyTable.Index:
					return (token & 0xFFFFFF) << 5 | 9;
				case EventTable.Index:
					return (token & 0xFFFFFF) << 5 | 10;
				case StandAloneSigTable.Index:
					return (token & 0xFFFFFF) << 5 | 11;
				case ModuleRefTable.Index:
					return (token & 0xFFFFFF) << 5 | 12;
				case TypeSpecTable.Index:
					return (token & 0xFFFFFF) << 5 | 13;
				case AssemblyTable.Index:
					return (token & 0xFFFFFF) << 5 | 14;
				case AssemblyRefTable.Index:
					return (token & 0xFFFFFF) << 5 | 15;
				case FileTable.Index:
					return (token & 0xFFFFFF) << 5 | 16;
				case ExportedTypeTable.Index:
					return (token & 0xFFFFFF) << 5 | 17;
				case ManifestResourceTable.Index:
					return (token & 0xFFFFFF) << 5 | 18;
				case GenericParamTable.Index:
					return (token & 0xFFFFFF) << 5 | 19;
				case GenericParamConstraintTable.Index:
					//return (token & 0xFFFFFF) << 5 | 20;
					throw new NotImplementedException();
				case MethodSpecTable.Index:
					return (token & 0xFFFFFF) << 5 | 21;
				default:
					throw new InvalidOperationException();
			}
		}
	}

	public sealed class FieldMarshalTable : SortedTable<FieldMarshalTable.Record>
	{
		public const int Index = 0x0D;

		public struct Record : IRecord
		{
			internal int Parent;
			internal int NativeType;

			int IRecord.SortKey
			{
				get { return EncodeHasFieldMarshal(Parent); }
			}

			int IRecord.FilterKey
			{
				get { return Parent; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Parent = mr.ReadHasFieldMarshal();
				records[i].NativeType = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteHasFieldMarshal(records[i].Parent);
				mw.WriteBlobIndex(records[i].NativeType);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteHasFieldMarshal()
				.WriteBlobIndex()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				records[i].Parent = moduleBuilder.ResolvePseudoToken(records[i].Parent);
			}
			Sort();
		}

		public static int EncodeHasFieldMarshal(int token)
		{
			switch (token >> 24)
			{
				case FieldTable.Index:
					return (token & 0xFFFFFF) << 1 | 0;
				case ParamTable.Index:
					return (token & 0xFFFFFF) << 1 | 1;
				default:
					throw new InvalidOperationException();
			}
		}
	}

	public sealed class DeclSecurityTable : SortedTable<DeclSecurityTable.Record>
	{
		public const int Index = 0x0E;

		public struct Record : IRecord
		{
			public short Action;
			public int Parent;
			public int PermissionSet;

			int IRecord.SortKey
			{
				get { return Parent; }
			}

			int IRecord.FilterKey
			{
				get { return Parent; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Action = mr.ReadInt16();
				records[i].Parent = mr.ReadHasDeclSecurity();
				records[i].PermissionSet = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].Action);
				mw.WriteHasDeclSecurity(records[i].Parent);
				mw.WriteBlobIndex(records[i].PermissionSet);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(2)
				.WriteHasDeclSecurity()
				.WriteBlobIndex()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				int token = records[i].Parent;
				moduleBuilder.FixupPseudoToken(ref token);
				// do the HasDeclSecurity encoding, so that we can sort the table
				switch (token >> 24)
				{
					case TypeDefTable.Index:
						token = (token & 0xFFFFFF) << 2 | 0;
						break;
					case MethodDefTable.Index:
						token = (token & 0xFFFFFF) << 2 | 1;
						break;
					case AssemblyTable.Index:
						token = (token & 0xFFFFFF) << 2 | 2;
						break;
					default:
						throw new InvalidOperationException();
				}
				records[i].Parent = token;
			}
			Sort();
		}
	}

	public sealed class ClassLayoutTable : SortedTable<ClassLayoutTable.Record>
	{
		public const int Index = 0x0f;

		public struct Record : IRecord
		{
			public short PackingSize;
			public int ClassSize;
			public int Parent;

			int IRecord.SortKey
			{
				get { return Parent; }
			}

			int IRecord.FilterKey
			{
				get { return Parent; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].PackingSize = mr.ReadInt16();
				records[i].ClassSize = mr.ReadInt32();
				records[i].Parent = mr.ReadTypeDef();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			Sort();
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].PackingSize);
				mw.Write(records[i].ClassSize);
				mw.WriteTypeDef(records[i].Parent);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(6)
				.WriteTypeDef()
				.Value;
		}
	}

	public sealed class FieldLayoutTable : SortedTable<FieldLayoutTable.Record>
	{
		public const int Index = 0x10;

		public struct Record : IRecord
		{
			public int Offset;
			public int Field;

			int IRecord.SortKey
			{
				get { return Field; }
			}

			int IRecord.FilterKey
			{
				get { return Field; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Offset = mr.ReadInt32();
				records[i].Field = mr.ReadField();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].Offset);
				mw.WriteField(records[i].Field);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(4)
				.WriteField()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				records[i].Field = moduleBuilder.ResolvePseudoToken(records[i].Field) & 0xFFFFFF;
			}
			Sort();
		}
	}

	public sealed class StandAloneSigTable : Table<int>
	{
		internal const int Index = 0x11;

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i] = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteBlobIndex(records[i]);
			}
		}

		protected override int GetRowSize(Table.RowSizeCalc rsc)
		{
			return rsc.WriteBlobIndex().Value;
		}

		public int FindOrAddRecord(int blob)
		{
			for (int i = 0; i < rowCount; i++)
			{
				if (records[i] == blob)
				{
					return i + 1;
				}
			}
			return AddRecord(blob);
		}
	}

	public sealed class EventMapTable : SortedTable<EventMapTable.Record>
	{
		public const int Index = 0x12;

		public struct Record : IRecord
		{
			public int Parent;
			public int EventList;

			int IRecord.SortKey => Parent;

			int IRecord.FilterKey => Parent;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Parent = mr.ReadTypeDef();
				records[i].EventList = mr.ReadEvent();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteTypeDef(records[i].Parent);
				mw.WriteEvent(records[i].EventList);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteTypeDef()
				.WriteEvent()
				.Value;
		}
	}

	public sealed class EventPtrTable : Table<int>
	{
		public const int Index = 0x13;

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i] = mr.ReadEvent();
			}
		}
	}

	public sealed class EventTable : Table<EventTable.Record>
	{
		public const int Index = 0x14;

		public struct Record
		{
			public short EventFlags;
			public int Name;
			public int EventType;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].EventFlags = mr.ReadInt16();
				records[i].Name = mr.ReadStringIndex();
				records[i].EventType = mr.ReadTypeDefOrRef();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].EventFlags);
				mw.WriteStringIndex(records[i].Name);
				mw.WriteTypeDefOrRef(records[i].EventType);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(2)
				.WriteStringIndex()
				.WriteTypeDefOrRef()
				.Value;
		}
	}

	public sealed class PropertyMapTable : SortedTable<PropertyMapTable.Record>
	{
		public const int Index = 0x15;

		public struct Record : IRecord
		{
			public int Parent;
			public int PropertyList;

			int IRecord.SortKey
			{
				get { return Parent; }
			}

			int IRecord.FilterKey
			{
				get { return Parent; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Parent = mr.ReadTypeDef();
				records[i].PropertyList = mr.ReadProperty();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteTypeDef(records[i].Parent);
				mw.WriteProperty(records[i].PropertyList);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteTypeDef()
				.WriteProperty()
				.Value;
		}
	}

	public sealed class PropertyPtrTable : Table<int>
	{
		public const int Index = 0x16;

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i] = mr.ReadProperty();
			}
		}
	}

	public sealed class PropertyTable : Table<PropertyTable.Record>
	{
		public const int Index = 0x17;

		public struct Record
		{
			public short Flags;
			public int Name;
			public int Type;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Flags = mr.ReadInt16();
				records[i].Name = mr.ReadStringIndex();
				records[i].Type = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].Flags);
				mw.WriteStringIndex(records[i].Name);
				mw.WriteBlobIndex(records[i].Type);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(2)
				.WriteStringIndex()
				.WriteBlobIndex()
				.Value;
		}
	}

	public sealed class MethodSemanticsTable : SortedTable<MethodSemanticsTable.Record>
	{
		public const int Index = 0x18;

		// semantics
		public const short Setter = 0x0001;
		public const short Getter = 0x0002;
		public const short Other = 0x0004;
		public const short AddOn = 0x0008;
		public const short RemoveOn = 0x0010;
		public const short Fire = 0x0020;

		public struct Record : IRecord
		{
			public short Semantics;
			public int Method;
			public int Association;

			int IRecord.SortKey
			{
				get { return Association; }
			}

			int IRecord.FilterKey
			{
				get { return Association; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Semantics = mr.ReadInt16();
				records[i].Method = mr.ReadMethodDef();
				records[i].Association = mr.ReadHasSemantics();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].Semantics);
				mw.WriteMethodDef(records[i].Method);
				mw.WriteHasSemantics(records[i].Association);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(2)
				.WriteMethodDef()
				.WriteHasSemantics()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				moduleBuilder.FixupPseudoToken(ref records[i].Method);
				int token = records[i].Association;
				// do the HasSemantics encoding, so that we can sort the table
				switch (token >> 24)
				{
					case EventTable.Index:
						token = (token & 0xFFFFFF) << 1 | 0;
						break;
					case PropertyTable.Index:
						token = (token & 0xFFFFFF) << 1 | 1;
						break;
					default:
						throw new InvalidOperationException();
				}
				records[i].Association = token;
			}
			Sort();
		}

		public MethodInfo GetMethod(Module module, int token, bool nonPublic, short semantics)
		{
			foreach (int i in Filter(token))
			{
				if ((records[i].Semantics & semantics) != 0)
				{
					MethodBase method = module.ResolveMethod((MethodDefTable.Index << 24) + records[i].Method);
					if (nonPublic || method.IsPublic)
					{
						return (MethodInfo)method;
					}
				}
			}
			return null;
		}

		public MethodInfo[] GetMethods(Module module, int token, bool nonPublic, short semantics)
		{
			List<MethodInfo> methods = new List<MethodInfo>();
			foreach (int i in Filter(token))
			{
				if ((records[i].Semantics & semantics) != 0)
				{
					MethodInfo method = (MethodInfo)module.ResolveMethod((MethodDefTable.Index << 24) + records[i].Method);
					if (nonPublic || method.IsPublic)
					{
						methods.Add(method);
					}
				}
			}
			return methods.ToArray();
		}

		public void ComputeFlags(Module module, int token, out bool isPublic, out bool isNonPrivate, out bool isStatic)
		{
			isPublic = false;
			isNonPrivate = false;
			isStatic = false;
			foreach (int i in Filter(token))
			{
				MethodBase method = module.ResolveMethod((MethodDefTable.Index << 24) + records[i].Method);
				isPublic |= method.IsPublic;
				isNonPrivate |= (method.Attributes & MethodAttributes.MemberAccessMask) > MethodAttributes.Private;
				isStatic |= method.IsStatic;
			}
		}
	}

	public sealed class MethodImplTable : SortedTable<MethodImplTable.Record>
	{
		public const int Index = 0x19;

		public struct Record : IRecord
		{
			internal int Class;
			internal int MethodBody;
			internal int MethodDeclaration;

			int IRecord.SortKey => Class;

			int IRecord.FilterKey => Class;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Class = mr.ReadTypeDef();
				records[i].MethodBody = mr.ReadMethodDefOrRef();
				records[i].MethodDeclaration = mr.ReadMethodDefOrRef();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteTypeDef(records[i].Class);
				mw.WriteMethodDefOrRef(records[i].MethodBody);
				mw.WriteMethodDefOrRef(records[i].MethodDeclaration);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteTypeDef()
				.WriteMethodDefOrRef()
				.WriteMethodDefOrRef()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				moduleBuilder.FixupPseudoToken(ref records[i].MethodBody);
				moduleBuilder.FixupPseudoToken(ref records[i].MethodDeclaration);
			}
			Sort();
		}
	}

	public sealed class ModuleRefTable : Table<int>
	{
		public const int Index = 0x1A;

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i] = mr.ReadStringIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteStringIndex(records[i]);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteStringIndex()
				.Value;
		}

		public int FindOrAddRecord(int str)
		{
			for (int i = 0; i < rowCount; i++)
			{
				if (records[i] == str)
				{
					return i + 1;
				}
			}
			return AddRecord(str);
		}
	}

	public sealed class TypeSpecTable : Table<int>
	{
		public const int Index = 0x1B;

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i] = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteBlobIndex(records[i]);
			}
		}

		protected override int GetRowSize(Table.RowSizeCalc rsc)
		{
			return rsc.WriteBlobIndex().Value;
		}
	}

	sealed class ImplMapTable : SortedTable<ImplMapTable.Record>
	{
		internal const int Index = 0x1C;

		internal struct Record : IRecord
		{
			internal short MappingFlags;
			internal int MemberForwarded;
			internal int ImportName;
			internal int ImportScope;

			int IRecord.SortKey
			{
				get { return MemberForwarded; }
			}

			int IRecord.FilterKey
			{
				get { return MemberForwarded; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].MappingFlags = mr.ReadInt16();
				records[i].MemberForwarded = mr.ReadMemberForwarded();
				records[i].ImportName = mr.ReadStringIndex();
				records[i].ImportScope = mr.ReadModuleRef();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].MappingFlags);
				mw.WriteMemberForwarded(records[i].MemberForwarded);
				mw.WriteStringIndex(records[i].ImportName);
				mw.WriteModuleRef(records[i].ImportScope);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(2)
				.WriteMemberForwarded()
				.WriteStringIndex()
				.WriteModuleRef()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				moduleBuilder.FixupPseudoToken(ref records[i].MemberForwarded);
			}
			Sort();
		}
	}

	public sealed class FieldRVATable : SortedTable<FieldRVATable.Record>
	{
		public const int Index = 0x1D;

		public struct Record : IRecord
		{
			public int RVA;		// we set the high bit to signify that the RVA is in the CIL stream (instead of .sdata)
			public int Field;

			int IRecord.SortKey => Field;

			int IRecord.FilterKey => Field;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].RVA = mr.ReadInt32();
				records[i].Field = mr.ReadField();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].RVA);
				mw.WriteField(records[i].Field);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(4)
				.WriteField()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder, int sdataRVA, int cilRVA)
		{
			for (int i = 0; i < rowCount; i++)
			{
				if (records[i].RVA < 0)
				{
					records[i].RVA = (records[i].RVA & 0x7fffffff) + cilRVA;
				}
				else
				{
					records[i].RVA += sdataRVA;
				}
				moduleBuilder.FixupPseudoToken(ref records[i].Field);
			}
			Sort();
		}
	}

	public sealed class AssemblyTable : Table<AssemblyTable.Record>
	{
		public const int Index = 0x20;

		public struct Record
		{
			public int HashAlgId;
			public ushort MajorVersion;
			public ushort MinorVersion;
			public ushort BuildNumber;
			public ushort RevisionNumber;
			public int Flags;
			public int PublicKey;
			public int Name;
			public int Culture;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].HashAlgId = mr.ReadInt32();
				records[i].MajorVersion = mr.ReadUInt16();
				records[i].MinorVersion = mr.ReadUInt16();
				records[i].BuildNumber = mr.ReadUInt16();
				records[i].RevisionNumber = mr.ReadUInt16();
				records[i].Flags = mr.ReadInt32();
				records[i].PublicKey = mr.ReadBlobIndex();
				records[i].Name = mr.ReadStringIndex();
				records[i].Culture = mr.ReadStringIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].HashAlgId);
				mw.Write(records[i].MajorVersion);
				mw.Write(records[i].MinorVersion);
				mw.Write(records[i].BuildNumber);
				mw.Write(records[i].RevisionNumber);
				mw.Write(records[i].Flags);
				mw.WriteBlobIndex(records[i].PublicKey);
				mw.WriteStringIndex(records[i].Name);
				mw.WriteStringIndex(records[i].Culture);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(16)
				.WriteBlobIndex()
				.WriteStringIndex()
				.WriteStringIndex()
				.Value;
		}
	}

	public sealed class AssemblyRefTable : Table<AssemblyRefTable.Record>
	{
		public const int Index = 0x23;

		public struct Record
		{
			internal ushort MajorVersion;
			internal ushort MinorVersion;
			internal ushort BuildNumber;
			internal ushort RevisionNumber;
			internal int Flags;
			internal int PublicKeyOrToken;
			internal int Name;
			internal int Culture;
			internal int HashValue;
		}

		public int FindOrAddRecord(Record rec)
		{
			for (int i = 0; i < rowCount; i++)
			{
				// note that we ignore HashValue here!
				if (records[i].Name == rec.Name
					&& records[i].MajorVersion == rec.MajorVersion
					&& records[i].MinorVersion == rec.MinorVersion
					&& records[i].BuildNumber == rec.BuildNumber
					&& records[i].RevisionNumber == rec.RevisionNumber
					&& records[i].Flags == rec.Flags
					&& records[i].PublicKeyOrToken == rec.PublicKeyOrToken
					&& records[i].Culture == rec.Culture
					)
				{
					return i + 1;
				}
			}
			return AddRecord(rec);
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].MajorVersion = mr.ReadUInt16();
				records[i].MinorVersion = mr.ReadUInt16();
				records[i].BuildNumber = mr.ReadUInt16();
				records[i].RevisionNumber = mr.ReadUInt16();
				records[i].Flags = mr.ReadInt32();
				records[i].PublicKeyOrToken = mr.ReadBlobIndex();
				records[i].Name = mr.ReadStringIndex();
				records[i].Culture = mr.ReadStringIndex();
				records[i].HashValue = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].MajorVersion);
				mw.Write(records[i].MinorVersion);
				mw.Write(records[i].BuildNumber);
				mw.Write(records[i].RevisionNumber);
				mw.Write(records[i].Flags);
				mw.WriteBlobIndex(records[i].PublicKeyOrToken);
				mw.WriteStringIndex(records[i].Name);
				mw.WriteStringIndex(records[i].Culture);
				mw.WriteBlobIndex(records[i].HashValue);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(12)
				.WriteBlobIndex()
				.WriteStringIndex()
				.WriteStringIndex()
				.WriteBlobIndex()
				.Value;
		}
	}

	public sealed class FileTable : Table<FileTable.Record>
	{
		public const int Index = 0x26;

		public struct Record
		{
			public int Flags;
			public int Name;
			public int HashValue;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Flags = mr.ReadInt32();
				records[i].Name = mr.ReadStringIndex();
				records[i].HashValue = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].Flags);
				mw.WriteStringIndex(records[i].Name);
				mw.WriteBlobIndex(records[i].HashValue);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(4)
				.WriteStringIndex()
				.WriteBlobIndex()
				.Value;
		}
	}

	public sealed class ExportedTypeTable : Table<ExportedTypeTable.Record>
	{
		public const int Index = 0x27;

		public struct Record
		{
			public int Flags;
			public int TypeDefId;
			public int TypeName;
			public int TypeNamespace;
			public int Implementation;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Flags = mr.ReadInt32();
				records[i].TypeDefId = mr.ReadInt32();
				records[i].TypeName = mr.ReadStringIndex();
				records[i].TypeNamespace = mr.ReadStringIndex();
				records[i].Implementation = mr.ReadImplementation();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].Flags);
				mw.Write(records[i].TypeDefId);
				mw.WriteStringIndex(records[i].TypeName);
				mw.WriteStringIndex(records[i].TypeNamespace);
				mw.WriteImplementation(records[i].Implementation);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(8)
				.WriteStringIndex()
				.WriteStringIndex()
				.WriteImplementation()
				.Value;
		}

		public int FindOrAddRecord(Record rec)
		{
			for (int i = 0; i < rowCount; i++)
			{
				if (records[i].Implementation == rec.Implementation
					&& records[i].TypeName == rec.TypeName
					&& records[i].TypeNamespace == rec.TypeNamespace)
				{
					return i + 1;
				}
			}
			return AddRecord(rec);
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				moduleBuilder.FixupPseudoToken(ref records[i].Implementation);
			}
		}
	}

	public sealed class ManifestResourceTable : Table<ManifestResourceTable.Record>
	{
		public const int Index = 0x28;

		public struct Record
		{
			public int Offset;
			public int Flags;
			public int Name;
			public int Implementation;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Offset = mr.ReadInt32();
				records[i].Flags = mr.ReadInt32();
				records[i].Name = mr.ReadStringIndex();
				records[i].Implementation = mr.ReadImplementation();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].Offset);
				mw.Write(records[i].Flags);
				mw.WriteStringIndex(records[i].Name);
				mw.WriteImplementation(records[i].Implementation);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(8)
				.WriteStringIndex()
				.WriteImplementation()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				moduleBuilder.FixupPseudoToken(ref records[i].Implementation);
			}
		}
	}

	public sealed class NestedClassTable : SortedTable<NestedClassTable.Record>
	{
		public const int Index = 0x29;

		public struct Record : IRecord
		{
			public int NestedClass;
			public int EnclosingClass;

			int IRecord.SortKey
			{
				get { return NestedClass; }
			}

			int IRecord.FilterKey
			{
				get { return NestedClass; }
			}
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].NestedClass = mr.ReadTypeDef();
				records[i].EnclosingClass = mr.ReadTypeDef();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteTypeDef(records[i].NestedClass);
				mw.WriteTypeDef(records[i].EnclosingClass);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteTypeDef()
				.WriteTypeDef()
				.Value;
		}

		public List<int> GetNestedClasses(int enclosingClass)
		{
			List<int> nestedClasses = new List<int>();
			for (int i = 0; i < rowCount; i++)
			{
				if (records[i].EnclosingClass == enclosingClass)
				{
					nestedClasses.Add(records[i].NestedClass);
				}
			}
			return nestedClasses;
		}
	}

	public sealed class GenericParamTable : SortedTable<GenericParamTable.Record>, IComparer<GenericParamTable.Record>
	{
		public const int Index = 0x2A;

		public struct Record : IRecord
		{
			public short Number;
			public short Flags;
			public int Owner;
			public int Name;
			// not part of the table, we use it to be able to fixup the GenericParamConstraint table
			public int unsortedIndex;

			int IRecord.SortKey => Owner;

			int IRecord.FilterKey => Owner;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Number = mr.ReadInt16();
				records[i].Flags = mr.ReadInt16();
				records[i].Owner = mr.ReadTypeOrMethodDef();
				records[i].Name = mr.ReadStringIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.Write(records[i].Number);
				mw.Write(records[i].Flags);
				mw.WriteTypeOrMethodDef(records[i].Owner);
				mw.WriteStringIndex(records[i].Name);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.AddFixed(4)
				.WriteTypeOrMethodDef()
				.WriteStringIndex()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				int token = records[i].Owner;
				moduleBuilder.FixupPseudoToken(ref token);
				// do the TypeOrMethodDef encoding, so that we can sort the table
				switch (token >> 24)
				{
					case TypeDefTable.Index:
						records[i].Owner = (token & 0xFFFFFF) << 1 | 0;
						break;
					case MethodDefTable.Index:
						records[i].Owner = (token & 0xFFFFFF) << 1 | 1;
						break;
					default:
						throw new InvalidOperationException();
				}
				records[i].unsortedIndex = i;
			}
			// FXBUG the unnecessary (IComparer<Record>) cast is a workaround for a .NET 2.0 C# compiler bug
			Array.Sort(records, 0, rowCount, (IComparer<Record>)this);
		}

		int IComparer<Record>.Compare(Record x, Record y)
		{
			if (x.Owner == y.Owner)
			{
				return x.Number == y.Number ? 0 : (x.Number > y.Number ? 1 : -1);
			}
			return x.Owner > y.Owner ? 1 : -1;
		}

		public void PatchAttribute(int token, GenericParameterAttributes genericParameterAttributes)
		{
			records[(token & 0xFFFFFF) - 1].Flags = (short)genericParameterAttributes;
		}

		public int[] GetIndexFixup()
		{
			int[] array = new int[rowCount];
			for (int i = 0; i < rowCount; i++)
			{
				array[records[i].unsortedIndex] = i;
			}
			return array;
		}

		public int FindFirstByOwner(int token)
		{
			foreach (int i in Filter(token))
			{
				return i;
			}
			return -1;
		}
	}

	public sealed class MethodSpecTable : Table<MethodSpecTable.Record>
	{
		public const int Index = 0x2B;

		public struct Record
		{
			public int Method;
			public int Instantiation;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Method = mr.ReadMethodDefOrRef();
				records[i].Instantiation = mr.ReadBlobIndex();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteMethodDefOrRef(records[i].Method);
				mw.WriteBlobIndex(records[i].Instantiation);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteMethodDefOrRef()
				.WriteBlobIndex()
				.Value;
		}

		public int FindOrAddRecord(Record record)
		{
			for (int i = 0; i < rowCount; i++)
			{
				if (records[i].Method == record.Method
					&& records[i].Instantiation == record.Instantiation)
				{
					return i + 1;
				}
			}
			return AddRecord(record);
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			for (int i = 0; i < rowCount; i++)
			{
				moduleBuilder.FixupPseudoToken(ref records[i].Method);
			}
		}
	}

	public sealed class GenericParamConstraintTable : SortedTable<GenericParamConstraintTable.Record>
	{
		public const int Index = 0x2C;

		public struct Record : IRecord
		{
			public int Owner;
			public int Constraint;

			int IRecord.SortKey => Owner;

			int IRecord.FilterKey => Owner;
		}

		public override void Read(MetadataReader mr)
		{
			for (int i = 0; i < records.Length; i++)
			{
				records[i].Owner = mr.ReadGenericParam();
				records[i].Constraint = mr.ReadTypeDefOrRef();
			}
		}

		public override void Write(MetadataWriter mw)
		{
			for (int i = 0; i < rowCount; i++)
			{
				mw.WriteGenericParam(records[i].Owner);
				mw.WriteTypeDefOrRef(records[i].Constraint);
			}
		}

		protected override int GetRowSize(RowSizeCalc rsc)
		{
			return rsc
				.WriteGenericParam()
				.WriteTypeDefOrRef()
				.Value;
		}

		public void Fixup(ModuleBuilder moduleBuilder)
		{
			int[] fixups = moduleBuilder.GenericParam.GetIndexFixup();
			for (int i = 0; i < rowCount; i++)
			{
				records[i].Owner = fixups[records[i].Owner - 1] + 1;
			}
			Sort();
		}
	}
}
