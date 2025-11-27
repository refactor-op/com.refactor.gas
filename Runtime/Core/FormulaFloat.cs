#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Refactor.Gameplay.Attributes
{
    /// <summary>
    /// Float 属性的公式.
    /// </summary>
    public readonly struct FormulaFloat : IFormula<float>
    {
        private readonly byte[] _bytecode;
        private readonly int _slotCount;
        private readonly int _maxStackDepth;

        internal FormulaFloat(byte[] bytecode, int slotCount, int maxStackDepth)
        {
            _bytecode = bytecode;
            _slotCount = slotCount;
            _maxStackDepth = maxStackDepth;
        }

        public int SlotCount => _slotCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Calculate(float baseValue, ReadOnlySpan<float> slots)
        {
            Span<float> stack = stackalloc float[_maxStackDepth];
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
                            var value = slotIndex < slots.Length ? slots[slotIndex] : 0f;
                            stack[stackPtr++] = value;
                        }
                        break;

                    case OpCode.LoadConstant:
                        {
                            var value = ReadSingle(code, ref pc);
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
        private static float ReadSingle(ReadOnlySpan<byte> code, ref int pc)
        {
            var bits = ReadInt32(code, ref pc);
            return BitConverter.Int32BitsToSingle(bits);
        }
    }
}