using System;
using System.Globalization;
using System.Reflection;

namespace ConfigurationManager.Utilities
{
    public sealed class FieldToPropertyInfoWrapper : PropertyInfo
    {
        private readonly FieldInfo _baseInfo;

        public FieldToPropertyInfoWrapper(FieldInfo baseInfo) => _baseInfo = baseInfo ?? throw new ArgumentNullException(nameof(baseInfo));

        public override object[] GetCustomAttributes(bool inherit) => _baseInfo.GetCustomAttributes(inherit);

        public override bool IsDefined(Type attributeType, bool inherit) => _baseInfo.IsDefined(attributeType, inherit);

        public override string Name => _baseInfo.Name;

        public override Type DeclaringType => _baseInfo.DeclaringType;

        public override Type ReflectedType => _baseInfo.ReflectedType;

        public override Type PropertyType => _baseInfo.FieldType;

        public override PropertyAttributes Attributes { get; } = PropertyAttributes.None;
        public override bool CanRead { get; } = true;
        public override bool CanWrite { get; } = true;

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _baseInfo.GetCustomAttributes(attributeType, inherit);

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) => _baseInfo.GetValue(obj);

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) => _baseInfo.SetValue(obj, value);

        public override MethodInfo[] GetAccessors(bool nonPublic) => throw new NotImplementedException();

        public override MethodInfo GetGetMethod(bool nonPublic) => throw new NotImplementedException();

        public override MethodInfo GetSetMethod(bool nonPublic) => throw new NotImplementedException();

        public override ParameterInfo[] GetIndexParameters() => throw new NotImplementedException();
    }
}