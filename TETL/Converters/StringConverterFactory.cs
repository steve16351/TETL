// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using TETL.Attributes;

namespace TETL.Converters
{
    /// <summary>
    /// Provides a factory of converters to to/from property 
    /// types to/from strings
    /// </summary>
    /// <typeparam name="T">Target object type</typeparam>
    public static class StringConverterFactory<T>
    {
        /// <summary>
        /// Holds a mapping of types to the types of the conversion classes
        /// available to provide a bi-directional string conversion
        /// </summary>
        private static Dictionary<Type, Type> _converters = new Dictionary<Type, Type>();

        /// <summary>
        /// Initializes static members of the <see cref="StringConverterFactory{T}" /> class.
        /// Sets up build in converter types.
        /// </summary>
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
            _converters.Add(typeof(int), typeof(Int32Converter<T>));
            _converters.Add(typeof(int?), typeof(NullableInt32Converter<T>));
            _converters.Add(typeof(long), typeof(Int64Converter<T>));
            _converters.Add(typeof(long?), typeof(NullableInt64Converter<T>));
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
        /// <returns>An IConvertAndSet instance that will provide string conversion for the specified property</returns>
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
