using System;

namespace TETL.Converters
{
    public class CharConverter<T> : BaseConverter<T, char>, IConvertAndSet
    {
        public override void SetValue(object target, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return;
            _setter((T)target, value[0]);
        }

        public override string GetValue(object target)
        {
            return _getter((T)target).ToString();
        }
    }

    public class NullableCharConverter<T> : BaseConverter<T, char?>, IConvertAndSet
    {
        public override void SetValue(object target, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return;
            _setter((T)target, value[0]);
        }

        public override string GetValue(object target)
        {
            return _getter((T)target)?.ToString() ?? String.Empty;
        }
    }
}
