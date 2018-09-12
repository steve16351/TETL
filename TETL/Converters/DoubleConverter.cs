using System;

namespace TETL.Converters
{
    public class DoubleConverter<T> : BaseConverter<T, double>, IConvertAndSet
    {
        public override string GetValue(object target)
        {
            return _getter((T)target).ToString();
        }

        public override void SetValue(object target, string value)
        {
            _setter((T)target, double.Parse(value));
        }
    }

    public class NullableDoubleConverter<T> : BaseConverter<T, double?>, IConvertAndSet
    {
        public override string GetValue(object target)
        {
            return _getter((T)target)?.ToString() ?? String.Empty;
        }

        public override void SetValue(object target, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return;
            _setter((T)target, double.Parse(value));
        }
    }
}
