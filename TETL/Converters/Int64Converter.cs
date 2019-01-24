using System;

namespace TETL.Converters
{
    public class Int64Converter<T> : BaseConverter<T, Int64>, IConvertAndSet
    {
        public override string GetValue(object target)
        {
            return _getter((T)target).ToString();
        }

        public override void SetValue(object target, string value)
        {
            _setter((T)target, Int64.Parse(value));
        }
    }

    public class NullableInt64Converter<T> : BaseConverter<T, Int64?>, IConvertAndSet
    {
        public override string GetValue(object target)
        {
            return _getter((T)target)?.ToString() ?? String.Empty;
        }

        public override void SetValue(object target, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return;
            _setter((T)target, Int64.Parse(value));
        }
    }
}
