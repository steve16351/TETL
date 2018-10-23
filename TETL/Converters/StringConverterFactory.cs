using System;
using System.Collections.Generic;
using System.Reflection;
using TETL.Attributes;

namespace TETL.Converters
{
    public static class StringConverterFactory<T>
    {
        private static Dictionary<Type, Type> _converters = new Dictionary<Type, Type>();

        static StringConverterFactory()
        {
            _converters = new Dictionary<Type, Type>();
            _converters.Add(typeof(string), typeof(StringConverter<T>));
            _converters.Add(typeof(double), typeof(DoubleConverter<T>));
            _converters.Add(typeof(double?), typeof(DoubleConverter<T>));
            _converters.Add(typeof(decimal), typeof(DecimalConverter<T>));
            _converters.Add(typeof(decimal?), typeof(NullableDecimalConverter<T>));
            _converters.Add(typeof(DateTime), typeof(DateTimeConverter<T>));
            _converters.Add(typeof(DateTime?), typeof(NullableDateTimeConverter<T>));
            _converters.Add(typeof(Int32), typeof(Int32Converter<T>));
            _converters.Add(typeof(Int32?), typeof(NullableInt32Converter<T>));
            _converters.Add(typeof(Int64), typeof(Int64Converter<T>));
            _converters.Add(typeof(Int64?), typeof(NullableInt64Converter<T>));
            _converters.Add(typeof(bool), typeof(BoolConverter<T>));
            _converters.Add(typeof(bool?), typeof(NullableBoolConverter<T>));
            _converters.Add(typeof(char), typeof(CharConverter<T>));
            _converters.Add(typeof(char?), typeof(NullableCharConverter<T>));
        }

        public static IConvertAndSet Get(PropertyInfo info, TextFileMappingAttribute attribute)
        {
            Type converterType;
            if (_converters.TryGetValue(info.PropertyType, out converterType))
            {
                var converter = (IConvertAndSet)Activator.CreateInstance(converterType);
                converter.Setup(info, attribute);
                return converter;
            }

            throw new NotImplementedException($"No converter available for target type {info.PropertyType} which is required for target property \"{info.Name}\"");
        }
    }
}
