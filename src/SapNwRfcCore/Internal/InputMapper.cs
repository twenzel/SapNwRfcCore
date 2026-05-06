using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using SapNwRfcCore.Internal.Fields;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal;

internal static class InputMapper
{
    private static readonly Lazy<MethodInfo> FieldApplyMethod = new Lazy<MethodInfo>(GetFieldApplyMethod);

    private static readonly ConcurrentDictionary<Type, Action<RfcInterop, IntPtr, object>> ApplyActionsCache =
        new ConcurrentDictionary<Type, Action<RfcInterop, IntPtr, object>>();

    public static void Apply(RfcInterop interop, IntPtr dataHandle, object? input)
    {
        if (input == null)
            return;

        var inputType = input.GetType();
        var applyAction = ApplyActionsCache.GetOrAdd(inputType, BuildApplyAction);
        applyAction(interop, dataHandle, input);
    }

    private static MethodInfo GetFieldApplyMethod()
    {
        Expression<Action<IField>> expression = field => field.Apply(RfcInterop.Instance, default);
        return ((MethodCallExpression)expression.Body).Method;
    }

    private static Action<RfcInterop, IntPtr, object> BuildApplyAction(Type type)
    {
        var interopParameter = Expression.Parameter(typeof(RfcInterop));
        var dataHandleParameter = Expression.Parameter(typeof(IntPtr));
        var inputParameter = Expression.Parameter(typeof(object));
        var castedInputParameter = Expression.Convert(inputParameter, type);

        var applyExpressionsForProperties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(propertyInfo => BuildApplyExpressionForProperty(
                propertyInfo: propertyInfo,
                interopParameter: interopParameter,
                dataHandleParameter: dataHandleParameter,
                inputParameter: castedInputParameter))
            .Where(x => x != null)
            .ToArray();

        var expression = Expression.Lambda<Action<RfcInterop, IntPtr, object>>(
            Expression.Block(applyExpressionsForProperties!),
            interopParameter,
            dataHandleParameter,
            inputParameter);

        return expression.Compile();
    }

    private static Expression? BuildApplyExpressionForProperty(
        PropertyInfo propertyInfo,
        Expression interopParameter,
        Expression dataHandleParameter,
        Expression inputParameter)
    {
        // skip property from mapping
        if (Attribute.IsDefined(propertyInfo, typeof(SapIgnoreAttribute)))
        {
            return null;
        }

        var nameAttribute = propertyInfo.GetCustomAttribute<SapNameAttribute>();
        var name = Expression.Constant(nameAttribute?.Name ?? propertyInfo.Name.ToUpper());

        // var value = propertyInfo.GetValue(input);
        Expression property = Expression.Property(inputParameter, propertyInfo);

        ConstructorInfo? fieldConstructor = null;
        if (propertyInfo.PropertyType == typeof(string))
        {
            // new RfcStringField(name, (string)value);
            fieldConstructor = GetFieldConstructor(() => new StringField(string.Empty, string.Empty));
        }
        else if (propertyInfo.PropertyType == typeof(int))
        {
            // new RfcIntField(name, (int)value);
            fieldConstructor = GetFieldConstructor(() => new IntField(string.Empty, default));
        }
        else if (propertyInfo.PropertyType == typeof(long))
        {
            // new RfcLongField(name, (long)value);
            fieldConstructor = GetFieldConstructor(() => new LongField(string.Empty, default));
        }
        else if (propertyInfo.PropertyType == typeof(double))
        {
            // new RfcDoubleField(name, (double)value);
            fieldConstructor = GetFieldConstructor(() => new DoubleField(string.Empty, default));
        }
        else if (propertyInfo.PropertyType == typeof(decimal))
        {
            // new RfcDecimalField(name, (decimal)value);
            fieldConstructor = GetFieldConstructor(() => new DecimalField(string.Empty, default));
        }
        else if (propertyInfo.PropertyType == typeof(byte[]))
        {
            // new BytesField(name, value);
            fieldConstructor = GetFieldConstructor(() => new BytesField(string.Empty, new byte[0]));
        }
        else if (propertyInfo.PropertyType == typeof(char[]))
        {
            // new CharsField(name, value);
            fieldConstructor = GetFieldConstructor(() => new CharsField(string.Empty, new char[0]));
        }
        else if (propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(DateTime?))
        {
            // new RfcDateField(name, (DateTime?)value);
            fieldConstructor = GetFieldConstructor(() => new DateField(string.Empty, default));
            property = Expression.Convert(property, typeof(DateTime?));
        }
        else if (propertyInfo.PropertyType == typeof(TimeSpan) || propertyInfo.PropertyType == typeof(TimeSpan?))
        {
            // new RfcTimeField(name, (TimeSpan?)value);
            fieldConstructor = GetFieldConstructor(() => new TimeField(string.Empty, default));
            property = Expression.Convert(property, typeof(TimeSpan?));
        }
        else if (propertyInfo.PropertyType.IsArray)
        {
            // new RfcTableField<TElementType>(name, (TElementType[])value);
            var tableFieldType = typeof(TableField<>).MakeGenericType(propertyInfo.PropertyType.GetElementType()!);
            fieldConstructor = tableFieldType.GetConstructor([typeof(string), propertyInfo.PropertyType]);
        }
        else if (propertyInfo.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
        {
            var elementType = propertyInfo.PropertyType.GetGenericArguments()[0];

            var tableFieldType = typeof(EnumerableField<>).MakeGenericType(elementType);
            fieldConstructor = tableFieldType.GetConstructor([typeof(string), propertyInfo.PropertyType]);
        }
        else if (!propertyInfo.PropertyType.IsPrimitive)
        {
            // new RfcStructureField<T>(name, (T)value);
            var structureFieldType = typeof(StructureField<>).MakeGenericType(propertyInfo.PropertyType);
            fieldConstructor = structureFieldType.GetConstructor([typeof(string), propertyInfo.PropertyType]);
        }

        var fieldNewExpression = Expression.New(
            constructor: fieldConstructor ?? throw new InvalidOperationException("No matching field constructor found"),
            name,
            property);

        // instance.Apply(interopParameter, dataHandleParameter);
        return Expression.Call(
            instance: fieldNewExpression,
            method: FieldApplyMethod.Value,
            arguments: [interopParameter, dataHandleParameter]);
    }

    private static ConstructorInfo GetFieldConstructor(Expression<Func<IField>> constructor)
        => ((NewExpression)constructor.Body).Constructor!;
}
