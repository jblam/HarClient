using HarSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace JBlam.HarClient
{
    class HarContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var baseProperty = base.CreateProperty(member, memberSerialization);
            if (member.Name == nameof(Response.RedirectUrl))
            {
                baseProperty.PropertyName = "redirectURL";
                baseProperty.DefaultValue = "";
                baseProperty.NullValueHandling = NullValueHandling.Include;
                baseProperty.ValueProvider = new FallbackValueProvider(baseProperty.ValueProvider, baseProperty.DefaultValue);
            }
            return baseProperty;
        }

        class FallbackValueProvider : IValueProvider
        {
            public FallbackValueProvider(IValueProvider valueProvider, object fallback)
            {
                this.valueProvider = valueProvider;
                this.fallback = fallback;
            }

            readonly IValueProvider valueProvider;
            readonly object fallback;

            public object GetValue(object target) => valueProvider.GetValue(target) ?? fallback;

            public void SetValue(object target, object value) => valueProvider.SetValue(target, value);
        }
    }
}
