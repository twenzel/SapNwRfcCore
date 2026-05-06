using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using SapNwRfcCore.Internal.Fields;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal;

internal static class OutputMapper
{
    private static readonly ConcurrentDictionary<Type, Func<RfcInterop, IntPtr, object>> ExtractFuncsCache =
        new ConcurrentDictionary<Type, Func<RfcInterop, IntPtr, object>>();

    public static TOutput Extract<TOutput>(RfcInterop interop, IntPtr dataHandle)
    {
        var outputType = typeof(TOutput);
        var extractFunc = ExtractFuncsCache.GetOrAdd(outputType, BuildExtractFunc);
        return (TOutput)extractFunc(interop, dataHandle);
    }

    private static Func<RfcInterop, IntPtr, object> BuildExtractFunc(Type type)
    {
        var interop = Expression.Parameter(typeof(RfcInterop));
        var dataHandle = Expression.Parameter(typeof(IntPtr));
        var result = Expression.Variable(type);

        var extractExpressionsForProperties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(propertyInfo => BuildExtractExpressionForProperty(
                propertyInfo: propertyInfo,
                interop: interop,
                dataHandle: dataHandle,
                result: result))
            .Where(x => x != null);

        var body = Array.Empty<Expression>()
            .Concat([Expression.Assign(result, Expression.New(type))])
            .Concat(extractExpressionsForProperties)
            .Concat([result])
            .ToArray();

        var expression = Expression.Lambda<Func<RfcInterop, IntPtr, object>>(
            body: Expression.Block(
                variables: [result],
                expressions: body!),
            parameters: [interop, dataHandle]);

        return expression.Compile();
    }

    private static Expression? BuildExtractExpressionForProperty(
        PropertyInfo propertyInfo,
        Expression interop,
        Expression dataHandle,
        Expression result)
    {
        // skip property from mapping
        if (Attribute.IsDefined(propertyInfo, typeof(SapIgnoreAttribute)))
        {
            return null;
        }

        var nameAttribute = propertyInfo.GetCustomAttribute<SapNameAttribute>();
        var name = Expression.Constant(nameAttribute?.Name ?? propertyInfo.Name.ToUpper());

        Expression property = Expression.Property(result, propertyInfo);

        var arguments = new Collection<Expression> { interop, dataHandle, name };

        bool convertToNonNullable = false;
        MethodInfo? extractMethod = null;
        RfcInterop rfcInterop = RfcInterop.Instance;
        if (propertyInfo.PropertyType == typeof(string))
        {
            extractMethod = GetMethodInfo(() => StringField.Extract(rfcInterop, default, string.Empty));
        }
        else if (propertyInfo.PropertyType == typeof(int))
        {
            extractMethod = GetMethodInfo(() => IntField.Extract(rfcInterop, default, string.Empty));
        }
        else if (propertyInfo.PropertyType == typeof(long))
        {
            extractMethod = GetMethodInfo(() => LongField.Extract(rfcInterop, default, string.Empty));
        }
        else if (propertyInfo.PropertyType == typeof(double))
        {
            extractMethod = GetMethodInfo(() => DoubleField.Extract(rfcInterop, default, string.Empty));
        }
        else if (propertyInfo.PropertyType == typeof(decimal))
        {
            extractMethod = GetMethodInfo(() => DecimalField.Extract(rfcInterop, default, string.Empty));
        }
        else if (propertyInfo.PropertyType == typeof(byte[]))
        {
            extractMethod = GetMethodInfo(() => BytesField.Extract(rfcInterop, default, string.Empty, default));

            var bufferLengthAttribute = propertyInfo.GetCustomAttribute<SapBufferLengthAttribute>();
            arguments.Add(Expression.Constant(bufferLengthAttribute?.BufferLength, typeof(int?)));
        }
        else if (propertyInfo.PropertyType == typeof(char[]))
        {
            extractMethod = GetMethodInfo(() => CharsField.Extract(rfcInterop, default, string.Empty, default));

            var bufferLengthAttribute = propertyInfo.GetCustomAttribute<SapBufferLengthAttribute>();
            arguments.Add(Expression.Constant(bufferLengthAttribute?.BufferLength ?? 0));
        }
        else if (propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(DateTime?))
        {
            convertToNonNullable = propertyInfo.PropertyType == typeof(DateTime);
            extractMethod = GetMethodInfo(() => DateField.Extract(rfcInterop, default, string.Empty));
        }
        else if (propertyInfo.PropertyType == typeof(TimeSpan) || propertyInfo.PropertyType == typeof(TimeSpan?))
        {
            convertToNonNullable = propertyInfo.PropertyType == typeof(TimeSpan);
            extractMethod = GetMethodInfo(() => TimeField.Extract(rfcInterop, default, string.Empty));
        }
        else if (propertyInfo.PropertyType.IsArray)
        {
            var elementType = propertyInfo.PropertyType.GetElementType() ?? throw new InvalidOperationException("Can't get element type from array");

            extractMethod = GetMethodInfo(() => TableField<object>.Extract<object>(rfcInterop, default, string.Empty))
                .GetGenericMethodDefinition()
                .MakeGenericMethod(elementType);
        }
        else if (propertyInfo.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
        {
            var elementType = propertyInfo.PropertyType.GetGenericArguments()[0];

            extractMethod = GetMethodInfo(() => EnumerableField<object>.Extract<object>(rfcInterop, default, string.Empty))
                .GetGenericMethodDefinition()
                .MakeGenericMethod(elementType);
        }
        else if (!propertyInfo.PropertyType.IsPrimitive)
        {
            extractMethod = GetMethodInfo(() => StructureField<object>.Extract<object>(rfcInterop, default, string.Empty))
                .GetGenericMethodDefinition()
                .MakeGenericMethod(propertyInfo.PropertyType);
        }

        if (extractMethod == null)
            throw new InvalidOperationException($"No matching extract method found for type {propertyInfo.PropertyType.Name}");

        // ReSharper disable once PossibleNullReferenceException
        var fieldValueProperty = extractMethod.ReturnType.GetProperty(nameof(Field<>.Value)) ?? throw new InvalidOperationException("Can't get Value property from field");

        var fieldValue = Expression.Property(
            Expression.Call(
                method: extractMethod,
                arguments: arguments.ToArray()),
            fieldValueProperty);

        return convertToNonNullable
            ? Expression.Assign(property, Expression.Coalesce(
                left: fieldValue,
                right: Expression.Default(propertyInfo.PropertyType)))
            : Expression.Assign(property, fieldValue);
    }

    private static MethodInfo GetMethodInfo(Expression<Action> extractMethod)
        => ((MethodCallExpression)extractMethod.Body).Method;
}
