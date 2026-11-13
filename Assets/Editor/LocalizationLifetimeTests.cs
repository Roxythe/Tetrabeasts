#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Object = UnityEngine.Object;

public class LocalizationLifetimeTests
{
    static ILocalesProvider CreateProvider() => (ILocalesProvider)Activator.CreateInstance(
        typeof(TetrabeastsLocalization).GetNestedType("RuntimeLocalesProvider", BindingFlags.NonPublic), true);

    static void DestroyLocales(ILocalesProvider provider)
    {
        foreach (var locale in new List<Locale>(provider.Locales))
            if (locale) Object.DestroyImmediate(locale);
    }

    [Test]
    public void DestroyedLocalesAreReplacedBeforeTheToolbarComparesThem()
    {
        var provider = CreateProvider();
        try
        {
            var english = provider.GetLocale(new LocaleIdentifier("en"));
            Object.DestroyImmediate(english);
            var rebuilt = provider.GetLocale(new LocaleIdentifier("en"));
            Assert.IsTrue(rebuilt);
            Assert.AreNotSame(english, rebuilt);
            Assert.DoesNotThrow(() => Assert.IsTrue(provider.Locales.Contains(rebuilt)));
            Assert.AreEqual(TetrabeastsLocalization.SupportedLanguages.Count, provider.Locales.Count);
        }
        finally { DestroyLocales(provider); }
    }

    [Test]
    public void ProviderRecoversAcrossRepeatedPlayModeCleanupAndReset()
    {
        var provider = CreateProvider();
        try
        {
            for (int transition = 0; transition < 3; transition++)
            {
                DestroyLocales(provider);
                ((IReset)provider).ResetState();
                foreach (var language in TetrabeastsLocalization.SupportedLanguages)
                {
                    var locale = provider.GetLocale(new LocaleIdentifier(language.Code));
                    Assert.IsTrue(locale);
                    Assert.DoesNotThrow(() => provider.Locales.Contains(locale));
                }
            }
        }
        finally { DestroyLocales(provider); }
    }

    [Test]
    public void RemovingADestroyedLocaleDoesNotInvokeLocaleEquals()
    {
        var provider = CreateProvider();
        try
        {
            var english = provider.GetLocale(new LocaleIdentifier("en"));
            Object.DestroyImmediate(english);
            Assert.IsTrue(provider.RemoveLocale(english));
            Assert.IsTrue(provider.GetLocale(new LocaleIdentifier("en")));
        }
        finally { DestroyLocales(provider); }
    }
}
#endif
