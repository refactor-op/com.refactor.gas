using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Refactor.Gameplay.Attributes
{
    /// <summary>
    /// Double 属性的公式构建器.
    /// </summary>
    public ref struct FormulaBuilderDouble
    {
        private const int InitialBufferSize = 64;
        private const int MaxBufferSize     = 65536;

        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
        private                 byte[]          _buffer;
        private                 int             _position;
        private                 int             _maxSlot;

        private FormulaBuilderDouble(int initialCapacity)
        {
            _buffer   = _pool.Rent(initialCapacity);
            _position = 0;
            _maxSlot  = -1;
        }

        public static FormulaBuilderDouble Create() => new(InitialBufferSize);

        public FormulaBuilderDouble LoadBase()
        {
            var span = GetSpan(1);
            span[0] = (byte)OpCode.LoadBase;
            Advance(1);
            return this;
        }

        public FormulaBuilderDouble LoadSlot(int slotIndex)
        {
            if (slotIndex > _maxSlot)
                _maxSlot = slotIndex;

            var span = GetSpan(5);
            span[0] = (byte)OpCode.LoadSlot;
            WriteInt32(span[1..], slotIndex);
            Advance(5);
            return this;
        }

        public FormulaBuilderDouble LoadConstant(double value)
        {
            var span = GetSpan(9);
            span[0] = (byte)OpCode.LoadConstant;
            WriteDouble(span[1..], value);
            Advance(9);
            return this;
        }

        public FormulaBuilderDouble Add()
        {
            var span = GetSpan(1);
            span[0] = (byte)OpCode.Add;
            Advance(1);
            return this;
        }

        public FormulaBuilderDouble Multiply()
        {
            var span = GetSpan(1);
            span[0] = (byte)OpCode.Multiply;
            Advance(1);
            return this;
        }

        public FormulaDouble Build()
        {
            if (_position == 0)
                throw new InvalidOperationException("Formula cannot be empty!");

            // 1. 创建精确大小的字节码数组.
            var bytecode = new byte[_position];
            Array.Copy(_buffer, 0, bytecode, 0, _position);
            
            // 2. 归还缓冲区.
            _pool.Return(_buffer);
            _buffer = null; // 标记为 null 防止重用.

            // 3. 验证并计算元数据.
            Validate(bytecode);
            var maxStackDepth = CalculateMaxStackDepth(bytecode);

            // 4. 返回最终的不可变公式对象.
            return new FormulaDouble(bytecode, _maxSlot + 1, maxStackDepth);
        }

        #region Private

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<byte> GetSpan(int sizeHint)
        {
            EnsureCapacity(_position + sizeHint);
            return _buffer.AsSpan(_position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance(int count) => _position += count;

        private void EnsureCapacity(int capacity)
        {
            if (capacity > _buffer.Length)
                Grow(capacity);
        }

        private void Grow(int minCapacity)
        {
            var newSize = Math.Max(_buffer.Length * 2, minCapacity);
            newSize = Math.Min(newSize, MaxBufferSize);

            if (newSize <= _buffer.Length)
                throw new InvalidOperationException($"Formula exceeds maximum size!");

            var newBuffer = _pool.Rent(newSize);
            Array.Copy(_buffer, 0, newBuffer, 0, _position);
            _pool.Return(_buffer);
            _buffer = newBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteInt32(Span<byte> span, int value)
        {
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            span[2] = (byte)(value >> 16);
            span[3] = (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteInt64(Span<byte> span, long value)
        {
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            span[2] = (byte)(value >> 16);
            span[3] = (byte)(value >> 24);
            span[4] = (byte)(value >> 32);
            span[5] = (byte)(value >> 40);
            span[6] = (byte)(value >> 48);
            span[7] = (byte)(value >> 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteDouble(Span<byte> span, double value) =>
            WriteInt64(span, BitConverter.DoubleToInt64Bits(value));

        private static void Validate(byte[] bytecode)
        {
            var pc         = 0; // 程序计数器
            var stackDepth = 0;

            while (pc < bytecode.Length)
            {
                var opCode = (OpCode)bytecode[pc++];

                switch (opCode)
                {
                    case OpCode.LoadBase:
                        stackDepth++;
                        break;

                    case OpCode.LoadSlot:
                        if (pc + 4 > bytecode.Length) throw new InvalidOperationException("Incomplete LoadSlot!");
                        pc += 4; // 4-byte int
                        stackDepth++;
                        break;

                    case OpCode.LoadConstant:
                        if (pc + 8 > bytecode.Length) throw new InvalidOperationException("Incomplete LoadConstant!");
                        pc += 8; // 8-byte double
                        stackDepth++;
                        break;

                    case OpCode.Add:
                    case OpCode.Multiply:
                        if (stackDepth < 2)
                            throw new InvalidOperationException("Stack underflow!");
                        stackDepth -= 1;
                        break;

                    default:
                        throw new InvalidOperationException("Unknown OpCode!");
                }
            }

            if (stackDepth != 1)
                throw new InvalidOperationException("Unbalanced formula!");
        }

        private static int CalculateMaxStackDepth(byte[] bytecode)
        {
            var pc            = 0;
            var stackDepth    = 0;
            var maxStackDepth = 0;

            while (pc < bytecode.Length)
            {
                var opCode = (OpCode)bytecode[pc++];

                switch (opCode)
                {
                    case OpCode.LoadBase:
                        stackDepth++;
                        maxStackDepth = Math.Max(maxStackDepth, stackDepth);
                        break;
                        
                    case OpCode.LoadSlot:
                        pc += 4; // 4-byte int
                        stackDepth++;
                        maxStackDepth = Math.Max(maxStackDepth, stackDepth);
                        break;

                    case OpCode.LoadConstant:
                        pc += 8; // 8-byte double
                        stackDepth++;
                        maxStackDepth = Math.Max(maxStackDepth, stackDepth);
                        break;

                    case OpCode.Add:
                    case OpCode.Multiply:
                        stackDepth -= 1;
                        break;
                }
            }

            if (maxStackDepth > 255)
                throw new InvalidOperationException("Stack depth exceeds maximum size!");

            return maxStackDepth;
        }

        #endregion
    }
}
