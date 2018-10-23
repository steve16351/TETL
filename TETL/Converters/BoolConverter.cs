using System;

namespace TETL.Converters
{
    public class BoolConverter<T> : BaseConverter<T, bool>, IConvertAndSet
    {
        public override void SetValue(object target, string value)
        {
            _setter((T)target, ParseBool(value));
        }

        public static bool ParseBool(string value)
        {
            bool convertedValue;
            if (bool.TryParse(value, out convertedValue))
                return convertedValue;

            int integerValue;
            if (int.TryParse(value, out integerValue))
            {
                if (integerValue == 0) return false;
                if (integerValue == 1) return true;
            }

            throw new InvalidCastException();
        }

        public override string GetValue(object target)
        {
            return _getter((T)target).ToString();
        }
    }

    public class NullableBoolConverter<T> : BaseConverter<T, bool?>, IConvertAndSet
    {
        public override void SetValue(object target, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return;
            _setter((T)target, BoolConverter<bool>.ParseBool(value));
        }

        public override string GetValue(object target)
        {
            return _getter((T)target)?.ToString() ?? String.Empty;
        }
    }
}
