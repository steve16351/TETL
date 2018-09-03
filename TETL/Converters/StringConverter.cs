using System;

namespace TETL.Converters
{
    public class StringConverter<T> : BaseConverter<T, string>, IConvertAndSet
    {
        public override string GetValue(object target)
        {
            return _getter((T)target) ?? String.Empty;
        }
        
        public override void SetValue(object target, string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                value = null;

            _setter((T)target, value?.Trim());
        }
    }
}
