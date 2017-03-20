﻿using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using LightBulb.Services;
using LightBulb.ViewModels;
using Microsoft.Practices.ServiceLocation;

namespace LightBulb
{
    public sealed class Locator
    {
        private static bool _isInit;

        public static T Resolve<T>() => ServiceLocator.Current.GetInstance<T>();
        public static T Resolve<T>(string id) => ServiceLocator.Current.GetInstance<T>(id);

        /// <summary>
        /// Initialize service locator
        /// </summary>
        public static void Init()
        {
            if (_isInit) return;
            if (ViewModelBase.IsInDesignModeStatic) return;

            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            // Services
            SimpleIoc.Default.Register<ISettingsService, FileSettingsService>();
            SimpleIoc.Default.Register<IGammaService, WindowsGammaService>();
            SimpleIoc.Default.Register<IWindowService, WindowsWindowService>();
            SimpleIoc.Default.Register<IHotkeyService, WindowsHotkeyService>();
            SimpleIoc.Default.Register<ITemperatureService, TemperatureService>();
            SimpleIoc.Default.Register<IGeoService, WebGeoService>();
            SimpleIoc.Default.Register<IVersionCheckService, WebVersionCheckService>();

            // View models
            SimpleIoc.Default.Register<IMainViewModel, MainViewModel>();
            SimpleIoc.Default.Register<IGeneralSettingsViewModel, GeneralSettingsViewModel>();
            SimpleIoc.Default.Register<IGeoSettingsViewModel, GeoSettingsViewModel>();
            SimpleIoc.Default.Register<IAdvancedSettingsViewModel, AdvancedSettingsViewModel>();

            // Load settings
            Resolve<ISettingsService>().Load();

            _isInit = true;
        }

        /// <summary>
        /// Cleanup resources used by service locator
        /// </summary>
        public static void Cleanup()
        {
            if (!_isInit) return;

            // Save settings
            Resolve<ISettingsService>().Save();

            // ReSharper disable SuspiciousTypeConversion.Global
            (Resolve<ISettingsService>() as IDisposable)?.Dispose();
            (Resolve<IGammaService>() as IDisposable)?.Dispose();
            (Resolve<IWindowService>() as IDisposable)?.Dispose();
            (Resolve<IHotkeyService>() as IDisposable)?.Dispose();
            (Resolve<ITemperatureService>() as IDisposable)?.Dispose();
            (Resolve<IGeoService>() as IDisposable)?.Dispose();
            (Resolve<IVersionCheckService>() as IDisposable)?.Dispose();

            (Resolve<IMainViewModel>() as IDisposable)?.Dispose();
            (Resolve<IGeneralSettingsViewModel>() as IDisposable)?.Dispose();
            (Resolve<IGeoSettingsViewModel>() as IDisposable)?.Dispose();
            (Resolve<IAdvancedSettingsViewModel>() as IDisposable)?.Dispose();
            // ReSharper restore SuspiciousTypeConversion.Global
        }

        public IMainViewModel MainViewModel => Resolve<IMainViewModel>();
        public IGeneralSettingsViewModel GeneralSettingsViewModel => Resolve<IGeneralSettingsViewModel>();
        public IGeoSettingsViewModel GeoSettingsViewModel => Resolve<IGeoSettingsViewModel>();
        public IAdvancedSettingsViewModel AdvancedSettingsViewModel => Resolve<IAdvancedSettingsViewModel>();
    }
}