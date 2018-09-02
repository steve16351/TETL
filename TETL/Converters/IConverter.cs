using System.Reflection;
using TETL.Attributes;

namespace TETL.Converters
{
    public interface IConvertAndSet
    {
        void SetValue(object target, string value);
        string GetValue(object target);
        void Setup(PropertyInfo propertyInfo, TextFileMappingAttribute mapper);
    }
}
