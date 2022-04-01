using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Registry
{
    public sealed class RegistrySettingsMetadata
    {
        public Type SettingsType { get; }
        public string ConstructorParameter { get; }
        public string SoftwarePath { get; }
        public string PolicyPath { get; }
        public string Category { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public RegistrySettingsMetadata(Type tRegistrySettings, string ctorParam = null)
        {
            if (tRegistrySettings == null)
            {
                throw new ArgumentNullException(nameof(tRegistrySettings));
            }

            var ctorType = GetParameterizationType(tRegistrySettings);
            if (ctorParam == null && !ctorType.HasFlag(ParameterizationType.NonParameterized))
            {
                throw new InvalidOperationException($"No constructor parameter specified for parameterized type {tRegistrySettings}");
            }
            if (ctorParam != null && !ctorType.HasFlag(ParameterizationType.StringParam))
            {
                throw new InvalidOperationException($"Constructor parameter specified for non-parameterized type {tRegistrySettings}");
            }

            this.SettingsType = tRegistrySettings;
            this.ConstructorParameter = ctorParam;
            this.DisplayName = tRegistrySettings.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                ?? tRegistrySettings.Name;
            this.Description = tRegistrySettings.GetCustomAttribute<DescriptionAttribute>()?.Description;

            RegistrySettings.IgnoreRegistry = true;
            try
            {
                RegistrySettings target;
                if (ctorParam == null)
                {
                    target = (RegistrySettings)Activator.CreateInstance(tRegistrySettings, true);
                }
                else
                {
                    target = (RegistrySettings)Activator.CreateInstance(tRegistrySettings,
                        BindingFlags.NonPublic | BindingFlags.Public, null, new object[] { ctorParam }, null);
                }

                if (target == null)
                {
                    throw new InvalidOperationException($"Failed to create type {tRegistrySettings} ({ctorType})");
                }

                this.Category =
                    tRegistrySettings.GetCustomAttribute<CategoryAttribute>()?.Category
                    ?? target.path;

                this.SoftwarePath = $@"SOFTWARE\{target.path}";
                this.PolicyPath = $@"SOFTWARE\Policies\{target.path}";

                var example = Activator.CreateInstance(tRegistrySettings);
                this.Settings = tRegistrySettings.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(IsSetting)
                    .Select(p => new RegistrySettingMetadata(p, example))
                    .ToList();
            }
            finally
            {
                RegistrySettings.IgnoreRegistry = false;
            }
        }

        public static IEnumerable<RegistrySettingsMetadata> GetAllParameterizations(Type tRegistrySettings, string ctorParam = "default")
        {
            var ctorType = GetParameterizationType(tRegistrySettings);
            if (ctorType.HasFlag(ParameterizationType.NonParameterized))
            {
                yield return new RegistrySettingsMetadata(tRegistrySettings);
            }
            if (ctorType.HasFlag(ParameterizationType.StringParam))
            {
                yield return new RegistrySettingsMetadata(tRegistrySettings, ctorParam);
            }
        }

        public static IEnumerable<RegistrySettingsMetadata> GetAllSettings(Assembly asm)
        {
            foreach (var type in asm.GetExportedTypes())
            {
                if (typeof(RegistrySettings).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    foreach (var p in GetAllParameterizations(type))
                    {
                        yield return p;
                    }
                }
            }
        }

        public static ParameterizationType GetParameterizationType(Type tRegistrySettings)
        {
            if (tRegistrySettings == null)
            {
                throw new ArgumentNullException(nameof(tRegistrySettings));
            }
            if (!typeof(RegistrySettings).IsAssignableFrom(tRegistrySettings))
            {
                throw new ArgumentOutOfRangeException(nameof(tRegistrySettings), "Type does not inherit from RegistrySettings");
            }
            if (tRegistrySettings.IsAbstract)
            {
                throw new ArgumentOutOfRangeException(nameof(tRegistrySettings), "Type is abstract");
            }

            var ctors = tRegistrySettings.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Select(t => t.GetParameters());
            var paramType = ParameterizationType.Unknown;
            if (ctors.Any(c => c.Length == 0))
            {
                paramType |= ParameterizationType.NonParameterized;
            }
            if (ctors.Any(c => c.Length == 1 && c[0].ParameterType == typeof(string)))
            {
                paramType |= ParameterizationType.StringParam;
            }

            return paramType;
        }

        [Flags]
        public enum ParameterizationType { Unknown = 0, NonParameterized = 1, StringParam = 2 };

        public IReadOnlyList<RegistrySettingMetadata> Settings { get; }

        private bool IsSetting(PropertyInfo arg)
        {
            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class ChildSettingOfAttribute : Attribute
    {
        public ChildSettingOfAttribute(string parentSettingName)
        {
            this.ParentName = parentSettingName ?? throw new ArgumentNullException(nameof(parentSettingName));
        }

        public string ParentName { get; }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class RegistrySettingMetadataAttribute : Attribute
    {
        public RegistrySettingMetadataAttribute(string name, Type type)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public string Name { get; }
        public Type Type { get; }
    }

    public sealed class RegistrySettingMetadata
    {
        public string Name { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string ParentSettingName { get; }
        public Type PropertyType { get; }
        public RegistryValueKind ValueKind { get; }
        public object DefaultValue { get; }

        public RegistrySettingMetadata(PropertyInfo p, object example)
        {
            var overrideMetadata = p.GetCustomAttribute<RegistrySettingMetadataAttribute>();
            this.Name = overrideMetadata?.Name ?? p.Name;
            this.PropertyType = overrideMetadata?.Type ?? p.PropertyType;
            this.ValueKind = RegistryKindForType(PropertyType);

            this.DefaultValue = p.GetValue(example, null);

            this.DisplayName = p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? Name;
            this.Description = p.GetCustomAttribute<DescriptionAttribute>()?.Description;
            this.ParentSettingName = p.GetCustomAttribute<ChildSettingOfAttribute>()?.ParentName;
        }

        private static RegistryValueKind RegistryKindForType(Type propertyType)
        {
            if (propertyType == typeof(string))
            {
                return RegistryValueKind.String;
            }
            else if (propertyType == typeof(int)
                || propertyType == typeof(bool))
            {
                return RegistryValueKind.DWord;
            }
            else if (propertyType == typeof(string[]))
            {
                return RegistryValueKind.MultiString;
            }
            // Most of the time, this is correct. Exporter works better with this.
            // throw new NotSupportedException($"Unknown type {propertyType}");
            else return RegistryValueKind.String;
        }
    }
}
