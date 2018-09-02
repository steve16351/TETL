using System;
using TETL.Attributes;

namespace TETL.Converters
{
    public class DateTimeConverter<T> : BaseConverter<T, DateTime>, IConvertAndSet
    {
        public override void SetValue(object target, string value)
        {
            _setter((T)target, ParseDate(value, _mappingAttribute));
        }

        public override string GetValue(object target)
        {
            return GetDateAsString(_getter((T)target), _mappingAttribute);
        }

        public static string GetDateAsString(DateTime value, TextFileMappingAttribute mappingAttribute)
        {
            if (mappingAttribute.DateTimeFormat != null)
                return value.ToString(mappingAttribute.DateTimeFormat);

            return value.ToString();
        }

        public static DateTime ParseDate(string value, TextFileMappingAttribute mappingAttribute)
        {
            DateTime parsedDate = DateTime.MinValue;

            if (mappingAttribute.DateTimeFormat != null)
                parsedDate = DateTime.ParseExact(value, mappingAttribute.DateTimeFormat, null);
            else
                parsedDate = DateTime.Parse(value);

            return parsedDate;
        }
    }


    public class NullableDateTimeConverter<T> : BaseConverter<T, DateTime?>, IConvertAndSet
    {
        public override void SetValue(object target, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return;            
            _setter((T)target, DateTimeConverter<DateTime>.ParseDate(value, _mappingAttribute));
        }

        public override string GetValue(object target)
        {
            DateTime? value = _getter((T)target);
            if (value.HasValue == false) return String.Empty;
            return DateTimeConverter<DateTime>.GetDateAsString(value.Value, _mappingAttribute);
        }
    }


}
