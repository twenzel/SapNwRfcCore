using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal.Fields;

internal abstract class Field<TValue> : IField
{
    protected Field(string name, TValue? value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public TValue? Value { get; }

    public abstract void Apply(RfcInterop interop, IntPtr dataHandle);

    public override bool Equals(object? obj)
    {
        return obj is Field<TValue> @base &&
               Name == @base.Name &&
               EqualityComparer<TValue>.Default.Equals(Value, @base.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Value);
    }
}

internal interface IField
{
    void Apply(RfcInterop interop, IntPtr dataHandle);
}
