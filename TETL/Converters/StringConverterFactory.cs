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

        /// <summary>
        /// Allow custom converter to be injected
        /// </summary>
        /// <param name="type">Source type the converter targets</param>
        /// <param name="converterType">Type of the converter</param>
        public static void AddOrReplaceConverter(Type type, Type converterType)
        {
            if (_converters.ContainsKey(type)) _converters.Remove(type);
            _converters.Add(type, converterType);
        }

        /// <summary>
        /// Retrieve a converter for the specified property
        /// </summary>
        /// <param name="info">Property info</param>
        /// <param name="attribute">Tax mapping attribute</param>
        /// <returns>Converter</returns>
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
