using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Refactor.Gameplay.Attributes
{
    /// <summary>
    /// 操作码.
    /// </summary>
    public enum OpCode : byte
    {
        /// <summary>压入基础值.</summary>
        LoadBase = 0,
        
        /// <summary>压入槽位值.</summary>
        LoadSlot = 1,
        
        /// <summary>压入常量.</summary>
        LoadConstant = 2,
        
        /// <summary>弹出两个值, 压入它们的和.</summary>
        Add = 3,
        
        /// <summary>弹出两个值, 压入它们的积.</summary>
        Multiply = 4,
    }

    public interface IFormula<T> where T : struct
    {
        int SlotCount { get; }
        T Calculate(T baseValue, ReadOnlySpan<T> slots);
    }

    /// <summary>
    /// 属性.
    /// 表示一个基础值, 槽位与公式的容器.
    /// <para><b>使用 ArrayPool, 需要 Dispose.</b></para>
    /// </summary>
    public sealed class Attribute<TFormula, TValue> : IDisposable
        where TFormula : struct, IFormula<TValue>
        where TValue : struct
    {
        private TValue[] _slots;
        private TValue   _base;
        private TFormula _formula;
        private TValue   _cached;
        private bool     _dirty;

        public Attribute(TValue @base, TFormula formula)
        {
            _base    = @base;
            _formula = formula;
            _slots   = ArrayPool<TValue>.Shared.Rent(_formula.SlotCount);
            Array.Clear(_slots, 0, _formula.SlotCount);
            _cached  = default;
            _dirty   = true;
        }

        public TValue Base => _base;
        public int SlotCount => _formula.SlotCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBase(TValue value)
        {
            _base = value;
            _dirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSlot(int slotIndex, TValue value)
        {
            _slots[slotIndex] = value;
            _dirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetValue()
        {
            if (_dirty)
            {
                _cached = _formula.Calculate(_base, _slots.AsSpan(0, _formula.SlotCount));
                _dirty = false;
            }
            return _cached;
        }

        public void Dispose()
        {
            if (_slots != null)
            {
                ArrayPool<TValue>.Shared.Return(_slots);
                _slots = null!;
            }
        }
    }
}