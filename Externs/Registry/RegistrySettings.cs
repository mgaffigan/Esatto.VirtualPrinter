﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics.Contracts;

namespace Esatto.Win32.Registry
{
#if ESATTO_WIN32
    public
#else
    internal
#endif
        abstract class RegistrySettings : IDisposable, INotifyPropertyChanged
    {
        private RegistryKey
            rkHkcu, rkHklm,
            rkHkcuSettings, rkHkcuPolicySettings, rkHklmSettings, rkHklmPolicySettings;
        private Options SetValueOptions;

        private string path = null;
        protected RegistryKey ConfigKey { get { return rkHkcuSettings; } }

        [Flags]
        protected enum ValueSource
        {
            Default = 0,
            Policy = 0x0001,
            Preferences = 0x0002,
            User = 0x0010,
            Computer = 0x0020,
            // calc
            UserPolicy = User | Policy,
            UserPreferences = User | Preferences,
            ComputerPolicy = Computer | Policy,
            ComputerPreferences = Computer | Preferences
        }

        [Flags]
        protected enum Options
        {
            None = 0,
            Writable = 0x0001,
            UseHkeyLocalMachine = 0x0002,
            WriteToHkeyLocalMachine = UseHkeyLocalMachine | Writable,
            PreferSetHkeyLocalMachine = 0x0004 | WriteToHkeyLocalMachine,
        }

        private const int TRUE = 1, FALSE = 0;

        #region ctor dtor

        protected RegistrySettings(string path)
            : this(path, Options.Writable)
        {
        }

        protected RegistrySettings(string path, bool isReadOnly)
            : this(path, isReadOnly ? Options.None : Options.Writable)
        {
        }

        protected RegistrySettings(string path, Options options)
        {
            this.path = path;
            this.SetValueOptions = options;

            rkHkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            rkHklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            string pathFormat = string.Format(@"SOFTWARE\{0}", path);
            string policyPathFormat = string.Format(@"SOFTWARE\Policies\{0}", path);

            if (options.HasFlag(Options.Writable))
            {
                rkHkcuSettings = rkHkcu.CreateSubKey(pathFormat, RegistryKeyPermissionCheck.ReadWriteSubTree);
            }
            else
            {
                rkHkcuSettings = rkHkcu.OpenSubKey(pathFormat, false);
            }
            rkHkcuPolicySettings = rkHkcu.OpenSubKey(policyPathFormat, false);

            if (options.HasFlag(Options.WriteToHkeyLocalMachine))
            {
                try
                {
                    rkHklmSettings = rkHklm.CreateSubKey(pathFormat, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                catch when (options.HasFlag(Options.PreferSetHkeyLocalMachine))
                {
                    // we did not get hklm open, write to HKCU instead
                    this.SetValueOptions = this.SetValueOptions & ~Options.UseHkeyLocalMachine;
                }
            }
            if (rkHklmSettings == null)
            {
                rkHklmSettings = rkHklm.OpenSubKey(pathFormat, false);
            }
            rkHklmPolicySettings = rkHklm.OpenSubKey(policyPathFormat, false);
        }

        public void Dispose()
        {
            disposeRegKey(rkHklmPolicySettings);
            disposeRegKey(rkHklmSettings);
            disposeRegKey(rkHkcuPolicySettings);
            disposeRegKey(rkHkcuSettings);
            disposeRegKey(rkHklm);
            disposeRegKey(rkHkcu);
        }

        private void disposeRegKey(RegistryKey key)
        {
            if (key == null)
                return;

            key.Dispose();
        }

        #endregion

        #region accessors

        #region bool

        protected bool GetBool(string name, bool defaultValue)
        {
            var value = GetValue(name, defaultValue ? TRUE : FALSE);
            if (!(value is int))
            {
                throw new ArgumentOutOfRangeException(string.Format(
                    "Setting '{0}' is invalid ({1}), expecting bool (REG_DWORD)",
                    name, value));
            }
            return ((int)value) != FALSE;
        }

        protected void SetBool(string name, bool value)
        {
            SetValue(name, value ? TRUE : FALSE, RegistryValueKind.DWord);
        }

        #endregion

        #region int

        protected int GetInt(string name, int defaultValue)
        {
            var value = GetValue(name, defaultValue);
            if (!(value is int))
            {
                throw new ArgumentOutOfRangeException(string.Format(
                    "Setting '{0}' is invalid ({1}), expecting int (REG_DWORD)",
                    name, value));
            }
            return (int)value;
        }

        protected void SetInt(string name, int value)
        {
            SetValue(name, value, RegistryValueKind.DWord);
        }

        #endregion

        #region string

        protected string GetString(string name, string defaultValue)
        {
            var value = GetValue(name, defaultValue);
            if (defaultValue != null && !(value is string))
            {
                throw new ArgumentOutOfRangeException(string.Format(
                    "Setting '{0}' is invalid ({1}), expecting string (REG_SZ)",
                    name, value));
            }
            return (string)value;
        }

        protected void SetString(string name, string value)
        {
            SetValue(name, value, RegistryValueKind.String);
        }

        protected string[] GetMultiString(string name, string[] defaultValues)
        {
            var value = GetValue(name, defaultValues);
            if (value != null && !(value is string[]))
            {
                throw new ArgumentOutOfRangeException(string.Format(
                    "Setting '{0}' is invalid ({1}), expecting strings (REG_MULTI_SZ)",
                    name, value));
            }
            return (string[])value;
        }

        protected void SetMultiString(string name, string[] values)
        {
            SetValue(name, values, RegistryValueKind.MultiString);
        }

        #endregion

        #region Certificate

        private TEnum GetEnumValue<TEnum>(string name, TEnum defaultValue)
            where TEnum : struct
        {
            Contract.Requires(typeof(TEnum).IsEnum);

            var value = GetString(name, defaultValue.ToString());
            if (value == null)
            {
                return defaultValue;
            }

            TEnum retVal = default(TEnum);
            if (!Enum.TryParse<TEnum>(value, out retVal))
            {
                throw new InvalidOperationException(string.Format("Could not "
                    + "deserialize enumeration value {0} from registry setting "
                    + "{1}. Current value: '{2}', allowed values:\r\n{3}", name,
                    this.path, value, string.Join(Environment.NewLine,
                    Enum.GetNames(typeof(TEnum)))));
            }

            return retVal;
        }

        #endregion

        #endregion

        #region values

        protected object GetValue(string name, object defaultValue)
        {
            object value;
            GetValue(name, defaultValue, out value);
            return value;
        }

        protected ValueSource GetValueSource(string name)
        {
            object _1;
            return GetValue(name, null, out _1);
        }

        protected ValueSource GetValue(string name, object defaultValue, out object value)
        {
            if (tryGetValue(rkHkcuPolicySettings, name, out value))
                return ValueSource.UserPolicy;
            if (tryGetValue(rkHklmPolicySettings, name, out value))
                return ValueSource.ComputerPolicy;
            if (tryGetValue(rkHkcuSettings, name, out value))
                return ValueSource.UserPreferences;
            if (tryGetValue(rkHklmSettings, name, out value))
                return ValueSource.ComputerPreferences;

            value = defaultValue;
            return ValueSource.Default;
        }

        protected void SetValue(string name, object defaultValue, RegistryValueKind kind)
        {
            if (SetValueOptions.HasFlag(Options.WriteToHkeyLocalMachine))
            {
                rkHklmSettings.SetValue(name, defaultValue, kind);
            }
            else
            {
                rkHkcuSettings.SetValue(name, defaultValue, kind);
            }

            Notify(name);
        }

        private bool tryGetValue(RegistryKey rkCurrent, string name, out object value)
        {
            value = null;

            if (rkCurrent != null)
            {
                value = rkCurrent.GetValue(name, value);
                if (value != null)
                    return true;
            }
            return false;
        }

        protected void DeleteUserKey()
        {
            rkHkcuSettings.Close();
            rkHkcu.DeleteSubKey(path);
        }

        #endregion

        /// <summary>
        /// INotifyPropertyChanged Event
        /// </summary>
        /// <param name="property"></param>
        protected void Notify(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// INotifyPropertyChanged Event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}