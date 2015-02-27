﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.PowerPcAssembly;
using System.Text;

namespace BrawlLib.SSBB.ResourceNodes
{
    public unsafe class RelocationManager
    {
        public ModuleNode _module;
        public ModuleDataNode _data;

        public ModuleDataNode DataNode { get { return _reference == null ? _data : _reference; } }

        private Dictionary<int, List<RelocationTarget>> _linkedCommands;
        private Dictionary<int, List<RelocationTarget>> _linkedBranches;
        private Dictionary<int, RelocationTarget> _targetRelocations;
        private Dictionary<int, List<string>> _tags;
        internal Dictionary<int, RelCommand> _commands;
        private Dictionary<int, SolidBrush> _colors;

        public uint BaseOffset { get { return DataNode.Data - ((ResourceNode)_module).WorkingUncompressed.Address; } }
        public KeyValuePair<int, RelCommand>[] GetCommands() { return _commands.OrderBy(x => x.Key).ToArray(); }

        public int _constructorIndex, _destructorIndex, _unresolvedIndex;

        int _referenceIndex;
        ModuleDataNode _reference = null;
        
        public RelocationManager(ModuleDataNode data)
        {
            _data = data;
            if (DataNode._manager == null)
            {
                //Initialize
                _linkedCommands = new Dictionary<int, List<RelocationTarget>>();
                _linkedBranches = new Dictionary<int, List<RelocationTarget>>();
                _targetRelocations = new Dictionary<int, RelocationTarget>();
                _tags = new Dictionary<int, List<string>>();
                _commands = new Dictionary<int, RelCommand>();
                _colors = new Dictionary<int, SolidBrush>();
            }
            else
            {
                //Make a copy
                _linkedCommands = DataNode._manager._linkedCommands;
                _linkedBranches = DataNode._manager._linkedBranches;
                _targetRelocations = DataNode._manager._targetRelocations;
                _tags = DataNode._manager._tags;
                _commands = DataNode._manager._commands;
                _colors = DataNode._manager._colors;
            }
        }

        public void UseReference(ModuleDataNode reference, int offset)
        {
            _reference = reference;
            _referenceIndex = offset.RoundDown(4) / 4;
        }

        public uint GetUint(int index)
        {
            return (uint)*((buint*)DataNode._dataBuffer.Address + index);
        }
        public int GetInt(int index)
        {
            return (int)*((bint*)DataNode._dataBuffer.Address + index);
        }
        public float GetFloat(int index)
        {
            return (int)*((bint*)DataNode._dataBuffer.Address + index);
        }
        public Bin32 GetBin(int index)
        {
            return *((Bin32*)DataNode._dataBuffer.Address + index);
        }
        public PPCOpCode GetCode(int index)
        {
            return (uint)*((buint*)DataNode._dataBuffer.Address + index);
        }
        public string GetString(int index)
        {
            return new string((sbyte*)DataNode._dataBuffer.Address + index * 4);
        }
        public void SetUint(int index, uint value)
        {
            *((buint*)DataNode._dataBuffer.Address + index) = value;
        }
        public void SetInt(int index, int value)
        {
            *((bint*)DataNode._dataBuffer.Address + index) = value;
        }
        public void SetFloat(int index, float value)
        {
            *((bfloat*)DataNode._dataBuffer.Address + index) = value;
        }
        public void SetBin(int index, Bin32 value)
        {
            *((Bin32*)DataNode._dataBuffer.Address + index) = value;
        }
        public void SetCode(int index, PPCOpCode code)
        {
            *((buint*)DataNode._dataBuffer.Address + index) = (uint)code;
        }
        public void SetString(int index, string value)
        {
            value.Write((sbyte*)DataNode._dataBuffer.Address + index * 4);
        }

        #region TargetRelocation
        public RelocationTarget GetTargetRelocation(int index)
        {
            if (_reference != null)
                return _reference._manager.GetTargetRelocation(index + _referenceIndex);

            if (_targetRelocations.ContainsKey(index))
                return _targetRelocations[index];
            return null;
        }
        public void SetTargetRelocation(int index, RelocationTarget target)
        {
            if (_reference != null)
            {
                _reference._manager.SetTargetRelocation(index + _referenceIndex, target);
                return;
            }

            if (_targetRelocations.ContainsKey(index))
            {
                if (target != null)
                    _targetRelocations[index] = target;
                else
                    _targetRelocations.Remove(index);
            }
            else if (target != null)
                _targetRelocations.Add(index, target);
        }
        public void ClearTargetRelocation(int index) { SetTargetRelocation(index, null); }
        #endregion

        public RelCommand GetCommand(int index)
        {
            if (_reference != null)
                return _reference._manager.GetCommand(index + _referenceIndex);

            if (_commands.ContainsKey(index))
                return _commands[index];
            return null;
        }
        public void SetCommand(int index, RelCommand cmd)
        {
            if (_commands.ContainsKey(index))
            {
                if (cmd != null)
                {
                    LinkCommand(index, false);
                    _commands[index] = cmd;
                    LinkCommand(index, true);
                }
                else
                    _commands.Remove(index);
            }
            else if (cmd != null)
                _commands.Add(index, cmd);
        }

        private void LinkCommand(int index, bool isLinked)
        {
            if (_commands[index] == null)
                return;

            RelocationTarget cmdTarget = _commands[index].GetTargetRelocation();
            ModuleSectionNode targetSection;
            if (cmdTarget != null && (targetSection = cmdTarget.Section) != null)
            {
                RelocationTarget thisRelocation = new RelocationTarget(DataNode.ModuleID, DataNode.Index, index);

                if (isLinked)
                    targetSection._manager.AddLinked(cmdTarget._index, thisRelocation);
                else
                    targetSection._manager.RemoveLinked(cmdTarget._index, thisRelocation);
            }
        }

        public void ClearCommand(int index) { SetCommand(index, null); }
        public SolidBrush GetColor(int index)
        {
            if (_colors.ContainsKey(index))
                return _colors[index];
            return null;
        }
        public void SetColor(int index, Color color)
        {
            if (_colors.ContainsKey(index))
            {
                if (color != Color.Transparent)
                {
                    if (_colors[index] == null)
                        _colors[index] = new SolidBrush(color);
                    else
                        _colors[index].Color = color;
                }
                else
                    _colors.Remove(index);
            }
            else if (color != Color.Transparent)
                _colors.Add(index, new SolidBrush(color));
        }
        public void ClearColor(int index) { SetColor(index, Color.Transparent); }
        public List<RelocationTarget> GetLinked(int index)
        {
            if (_linkedCommands.ContainsKey(index))
                return _linkedCommands[index];
            return null;
        }
        public void RemoveLinked(int index, RelocationTarget target)
        {
            if (_linkedCommands.ContainsKey(index) &&
                _linkedCommands[index] != null &&
                _linkedCommands[index].Contains(target))
                _linkedCommands[index].Remove(target);
        }
        public void AddLinked(int index, RelocationTarget target)
        {
            if (_linkedCommands.ContainsKey(index))
            {
                if (_linkedCommands[index] == null)
                    _linkedCommands[index] = new List<RelocationTarget>() { target };
                else
                    _linkedCommands[index].Add(target);
            }
            else
                _linkedCommands.Add(index, new List<RelocationTarget>() { target });
        }
        public void ClearLinked(int index)
        {
            if (_linkedCommands.ContainsKey(index))
            {
                _linkedCommands[index].Clear();
                _linkedCommands.Remove(index);
            }
        }
        public List<RelocationTarget> GetBranched(int index)
        {
            if (_linkedBranches.ContainsKey(index))
                return _linkedBranches[index];
            return null;
        }
        public void RemoveBranched(int index, RelocationTarget target)
        {
            if (_linkedBranches.ContainsKey(index) &&
                _linkedBranches[index] != null &&
                _linkedBranches[index].Contains(target))
                _linkedBranches[index].Remove(target);
        }
        public void AddBranched(int index, RelocationTarget target)
        {
            if (_linkedBranches.ContainsKey(index))
            {
                if (_linkedBranches[index] == null)
                    _linkedBranches[index] = new List<RelocationTarget>() { target };
                else
                    _linkedBranches[index].Add(target);
            }
            else
                _linkedBranches.Add(index, new List<RelocationTarget>() { target });
        }
        public void ClearBranched(int index)
        {
            if (_linkedBranches.ContainsKey(index))
            {
                _linkedBranches[index].Clear();
                _linkedBranches.Remove(index);
            }
        }
        public List<string> GetTags(int index)
        {
            if (_tags.ContainsKey(index))
                return _tags[index];
            return null;
        }
        public void RemoveTag(int index, string tag)
        {
            if (_tags.ContainsKey(index) &&
                _tags[index] != null &&
                _tags[index].Contains(tag))
                _tags[index].Remove(tag);
        }
        public void AddTag(int index, string tag)
        {
            if (_tags.ContainsKey(index))
            {
                if (_tags[index] == null)
                    _tags[index] = new List<string>() { tag };
                else
                    _tags[index].Add(tag);
            }
            else
                _tags.Add(index, new List<string>() { tag });
        }
        public void ClearTags(int index)
        {
            if (_tags.ContainsKey(index))
            {
                _tags[index].Clear();
                _tags.Remove(index);
            }
        }

        public static Color clrNotRelocated = Color.FromArgb(255, 255, 255);
        public static Color clrRelocated = Color.FromArgb(200, 255, 200);
        public static Color clrBadRelocate = Color.FromArgb(255, 200, 200);
        public static Color clrBlr = Color.FromArgb(255, 255, 100);

        public Color GetStatusColorFromIndex(int index)
        {
            if (GetCode(index) is PPCblr)
                return clrBlr;
            if (!_commands.ContainsKey(index))
                return clrNotRelocated;
            return clrRelocated;
        }

        internal void ClearCommands()
        {
            _commands = new Dictionary<int, RelCommand>();
        }
    }

    public class RelocationTarget
    {
        public RelocationTarget(uint moduleID, int sectionID, int index)
        {
            _moduleID = moduleID;
            _sectionID = sectionID;
            _index = index;
        }

        public uint _moduleID;
        public int _sectionID;
        public int _index;

        public override string ToString()
        {
            return String.Format("{0}[{1}] 0x{2}", RELNode._idNames.ContainsKey(_moduleID) ? RELNode._idNames[_moduleID] : "m" + _moduleID.ToString(), _sectionID, (_index * 4).ToString("X"));
        }

        public override int GetHashCode()
        {
            return (int)_moduleID ^ _sectionID ^ _index;
        }

        public override bool Equals(object obj)
        {
            if (obj is RelocationTarget)
                return obj.GetHashCode() == GetHashCode();
            return false;
        }

        public ModuleSectionNode Section { get { return RELNode._files.ContainsKey(_moduleID) ? RELNode._files[_moduleID].Sections[_sectionID] : null; } }
    }
}
