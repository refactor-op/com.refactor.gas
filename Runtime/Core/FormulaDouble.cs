#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Refactor.Gameplay.Attributes
{
    public readonly struct FormulaDouble : IFormula<double>
    {
        private readonly byte[] _bytecode;
        private readonly int _slotCount;
        private readonly int _maxStackDepth;

        internal FormulaDouble(byte[] bytecode, int slotCount, int maxStackDepth)
        {
            _bytecode = bytecode;
            _slotCount = slotCount;
            _maxStackDepth = maxStackDepth;
        }

        public int SlotCount => _slotCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Calculate(double baseValue, ReadOnlySpan<double> slots)
        {
            Span<double> stack = stackalloc double[_maxStackDepth];
            var stackPtr = 0;

            ReadOnlySpan<byte> code = _bytecode;
            var pc = 0;

            while (pc < code.Length)
            {
                var opCode = (OpCode)code[pc++];

                switch (opCode)
                {
                    case OpCode.LoadBase:
                        stack[stackPtr++] = baseValue;
                        break;

                    case OpCode.LoadSlot:
                        {
                            var slotIndex = ReadInt32(code, ref pc);
                            var value = slotIndex < slots.Length ? slots[slotIndex] : 0.0;
                            stack[stackPtr++] = value;
                        }
                        break;

                    case OpCode.LoadConstant:
                        {
                            var value = ReadDouble(code, ref pc);
                            stack[stackPtr++] = value;
                        }
                        break;

                    case OpCode.Add:
                        {
                            var b = stack[--stackPtr];
                            var a = stack[--stackPtr];
                            stack[stackPtr++] = a + b;
                        }
                        break;

                    case OpCode.Multiply:
                        {
                            var b = stack[--stackPtr];
                            var a = stack[--stackPtr];
                            stack[stackPtr++] = a * b;
                        }
                        break;
                }
            }

            return stack[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadInt32(ReadOnlySpan<byte> code, ref int pc)
        {
            var value = code[pc] | (code[pc + 1] << 8) | (code[pc + 2] << 16) | (code[pc + 3] << 24);
            pc += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ReadDouble(ReadOnlySpan<byte> code, ref int pc)
        {
            var bits = (long)ReadInt32(code, ref pc) | ((long)ReadInt32(code, ref pc) << 32);
            return BitConverter.Int64BitsToDouble(bits);
        }
    }
}