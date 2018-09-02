using System;

namespace TETL.Converters
{
    public class Int32Converter<T> : BaseConverter<T, Int32>, IConvertAndSet
    {
        public override string GetValue(object target)
        {
            return _getter((T)target).ToString();
        }

        public override void SetValue(object target, string value)
        {
            _setter((T)target, Int32.Parse(value));
        }
    }

    public class NullableInt32Converter<T> : BaseConverter<T, Int32?>, IConvertAndSet
    {
        public override string GetValue(object target)
        {
            return _getter((T)target)?.ToString() ?? String.Empty;
        }

        public override void SetValue(object target, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return;
            _setter((T)target, Int32.Parse(value));
        }
    }
}
