namespace Refactor.Gameplay.Attributes
{
    public static partial class Formulas
    {
        public static partial class Float
        {
            /// <summary> (base + slot0) * slot1. </summary>
            public static FormulaFloat Standard() => FormulaBuilderFloat.Create()
                .LoadBase()
                .LoadSlot(0)
                .Add()
                .LoadSlot(1)
                .Multiply()
                .Build();

            /// <summary> ((base + slot0) * slot1 + slot2) * slot3. </summary>
            public static FormulaFloat Wow() => FormulaBuilderFloat.Create()
                .LoadBase()
                .LoadSlot(0)
                .Add()
                .LoadSlot(1)
                .Multiply()
                .LoadSlot(2)
                .Add()
                .LoadSlot(3)
                .Multiply()
                .Build();
        }

        public static partial class Double
        {
            /// <summary> (base + slot0) * slot1. </summary>
            public static FormulaDouble Standard() => FormulaBuilderDouble.Create()
                .LoadBase()
                .LoadSlot(0)
                .Add()
                .LoadSlot(1)
                .Multiply()
                .Build();

            /// <summary> ((base + slot0) * slot1 + slot2) * slot3. </summary>
            public static FormulaDouble Wow() => FormulaBuilderDouble.Create()
                .LoadBase()
                .LoadSlot(0)
                .Add()
                .LoadSlot(1)
                .Multiply()
                .LoadSlot(2)
                .Add()
                .LoadSlot(3)
                .Multiply()
                .Build();
        }
    }
}