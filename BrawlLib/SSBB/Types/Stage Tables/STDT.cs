﻿using System;
using System.Runtime.InteropServices;

namespace BrawlLib.SSBBTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct STDT//Stage Trap Data Table
   {
		public const uint Tag = 0x54445453;

		public uint _tag;
		public bint _version;
		public bint _unk1;
		public bint _unk2;
        public bint _entryOffset;

        public STDT(int version)
        {
            _tag = Tag;
            _version = version;
            _unk1 = 0;
            _unk2 = 0;
            _entryOffset = 0x14;
        }

        public VoidPtr Address { get { fixed (void* ptr = &this)return ptr; } }
		public bfloat* Entries { get { return (bfloat*)(Address + _entryOffset); } }
	}
}
