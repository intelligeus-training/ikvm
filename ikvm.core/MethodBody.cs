/*
  Copyright (C) 2009 Jeroen Frijters

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
using IKVM.Reflection.Reader;
using System.IO;

namespace IKVM.Reflection
{
	public sealed class MethodBody
	{
		private readonly IList<ExceptionHandlingClause> exceptionClauses;
		private readonly IList<LocalVariableInfo> locals;
		private readonly bool initLocals;
		private readonly int maxStack;
		private readonly int localVarSigTok;
		private byte[] body;

		internal MethodBody(ModuleReader module, int rva, IGenericContext context)
		{
			const byte CorILMethod_TinyFormat = 0x02;
			const byte CorILMethod_FatFormat = 0x03;
			const byte CorILMethod_MoreSects = 0x08;
			const byte CorILMethod_InitLocals = 0x10;
			const byte CorILMethod_Sect_EHTable = 0x01;
			const byte CorILMethod_Sect_FatFormat = 0x40;
			const byte CorILMethod_Sect_MoreSects = 0x80;

			var exceptionClauses = new List<ExceptionHandlingClause>();
			var localVariableInfos = new List<LocalVariableInfo>();
			using var stream = module.GetStream();
			module.SeekRVA(rva);
			var binaryReader = new BinaryReader(stream);
			byte b = binaryReader.ReadByte();
			if ((b & 3) == CorILMethod_TinyFormat)
			{
				initLocals = true;
				body = binaryReader.ReadBytes(b >> 2);
				maxStack = 8;
			}
			else if ((b & 3) == CorILMethod_FatFormat)
			{
				initLocals = (b & CorILMethod_InitLocals) != 0;
				var flagsAndSize = (short)(b | (binaryReader.ReadByte() << 8));
				if ((flagsAndSize >> 12) != 3)
				{
					throw new BadImageFormatException("Fat format method header size should be 3");
				}
				maxStack = binaryReader.ReadUInt16();
				var codeLength = binaryReader.ReadInt32();
				localVarSigTok = binaryReader.ReadInt32();
				body = binaryReader.ReadBytes(codeLength);
				if ((b & CorILMethod_MoreSects) != 0)
				{
					stream.Position = (stream.Position + 3) & ~3;
					var hdr = binaryReader.ReadInt32();
					if ((hdr & CorILMethod_Sect_MoreSects) != 0 || (hdr & CorILMethod_Sect_EHTable) == 0)
					{
						throw new NotImplementedException();
					}
					if ((hdr & CorILMethod_Sect_FatFormat) != 0)
					{
						var count = ComputeExceptionCount((hdr >> 8) & 0xFFFFFF, 24);
						for (int i = 0; i < count; i++)
						{
							var flags = binaryReader.ReadInt32();
							var tryOffset = binaryReader.ReadInt32();
							var tryLength = binaryReader.ReadInt32();
							var handlerOffset = binaryReader.ReadInt32();
							var handlerLength = binaryReader.ReadInt32();
							var classTokenOrFilterOffset = binaryReader.ReadInt32();
							exceptionClauses.Add(new ExceptionHandlingClause(module, 
																					flags, 
																					tryOffset, 
																					tryLength, 
																					handlerOffset, 
																					handlerLength, 
																					classTokenOrFilterOffset, 
																					context));
						}
					}
					else
					{
						int count = ComputeExceptionCount((hdr >> 8) & 0xFF, 12);
						for (int i = 0; i < count; i++)
						{
							var flags = binaryReader.ReadUInt16();
							var tryOffset = binaryReader.ReadUInt16();
							var tryLength = binaryReader.ReadByte();
							var handlerOffset = binaryReader.ReadUInt16();
							int handlerLength = binaryReader.ReadByte();
							var classTokenOrFilterOffset = binaryReader.ReadInt32();
							exceptionClauses.Add(new ExceptionHandlingClause(module, 
																					flags, 
																					tryOffset, 
																					tryLength, 
																					handlerOffset, 
																					handlerLength, 
																					classTokenOrFilterOffset, 
																					context));
						}
					}
				}
				if (localVarSigTok != 0)
				{
					var byteReader = module.GetStandAloneSig((localVarSigTok & 0xFFFFFF) - 1);
					Signature.ReadLocalVarSig(module, byteReader, context, localVariableInfos);
				}
			}
			else
			{
				throw new BadImageFormatException();
			}
			this.exceptionClauses = exceptionClauses.AsReadOnly();
			locals = localVariableInfos.AsReadOnly();
		}

		private static int ComputeExceptionCount(int size, int itemLength)
		{
			// LAMESPEC according to the spec, the count should be calculated as "(size - 4) / itemLength",
			// FXBUG but to workaround a VB compiler bug that specifies the size incorrectly,
			// we do a truncating division instead.
			return size / itemLength;
		}

		public IList<ExceptionHandlingClause> ExceptionHandlingClauses => exceptionClauses;

		public bool InitLocals => initLocals;

		public IList<LocalVariableInfo> LocalVariables => locals;

		public byte[] GetILAsByteArray()
		{
			return body;
		}

		public int LocalSignatureMetadataToken => localVarSigTok;

		public int MaxStackSize => maxStack;
	}
}
